using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Session;
using System.Linq.Expressions;
using System.Reflection;
using BSAG.IOCTalk.Communication.Common;
using System.IO;
using BSAG.IOCTalk.Common.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using BSAG.IOCTalk.Communication.PersistentQueue.Transaction;
using System.Transactions;

namespace BSAG.IOCTalk.Communication.PersistentQueue
{
    public class PersistentClientCommunicationHost : IGenericCommunicationService
    {
        private IGenericCommunicationService underlyingCom;
        private Dictionary<Type, Dictionary<string, PersistentMethod>> persistentMethods = new Dictionary<Type, Dictionary<string, PersistentMethod>>();
        private string persistMsgFilePath;
        private FileStream persistMsgFile;
        internal static object syncLock = new object();
        private EventHandler<SessionEventArgs> fictionalSessionCreatedEvent;
        private EventHandler<SessionEventArgs> sessionCreatedBeforeResendEvent;
        private EventHandler<SessionEventArgs> fictionalSessionTerminatedEvent;
        private Session fictionalSession;
        private IContract fictionalSessionContract;
        private DateTime? lastActiveInvokeUtc;

        private ISession realUnderlyingSession;

        private int processResendLock = 0;
        private int resendTryCount = 1;
        private DateTime? lastResendTryUtc = null;

        internal const byte NotSendByte = 1;
        internal const byte AlreadySentByte = 2;

        private static readonly byte[] AlreadySentByteArr = new byte[] { AlreadySentByte };


        public PersistentClientCommunicationHost(IGenericCommunicationService underlyingCommunication)
        {
            this.underlyingCom = underlyingCommunication;
            this.underlyingCom.SessionCreated += UnderlyingCom_SessionCreated;
            this.underlyingCom.SessionTerminated += UnderlyingCom_SessionTerminated;
        }

        public bool RedirectSessionEvents { get; set; }

        public IGenericContainerHost ContainerHost => underlyingCom.ContainerHost;

        public string SerializerTypeName { get => underlyingCom.SerializerTypeName; set => underlyingCom.SerializerTypeName = value; }

        public IGenericMessageSerializer Serializer => underlyingCom.Serializer;

        public string LoggerTypeName { get => underlyingCom.LoggerTypeName; set => underlyingCom.LoggerTypeName = value; }

        public ILogger Logger => underlyingCom.Logger;

        public bool LogDataStream { get => underlyingCom.LogDataStream; set => underlyingCom.LogDataStream = value; }

        public string DataStreamLoggerTypeName { get => underlyingCom.DataStreamLoggerTypeName; set => underlyingCom.DataStreamLoggerTypeName = value; }

        public IDataStreamLogger DataStreamLogger { get => underlyingCom.DataStreamLogger; set => underlyingCom.DataStreamLogger = value; }

        public ISession[] ClientSessions => underlyingCom.ClientSessions;

        public InvokeThreadModel InvokeThreadModel { get => underlyingCom.InvokeThreadModel; set => underlyingCom.InvokeThreadModel = value; }

        public string DirectoryPath { get; set; } = @"." + Path.DirectorySeparatorChar + "IOCTalk-PendingMessageStore";

        public event EventHandler<SessionEventArgs> SessionCreated
        {
            add
            {
                fictionalSessionCreatedEvent += value;
            }
            remove
            {
                fictionalSessionCreatedEvent -= value;
            }
        }

        public event EventHandler<SessionEventArgs> SessionCreatedBeforeResend
        {
            add
            {
                sessionCreatedBeforeResendEvent += value;
            }
            remove
            {
                sessionCreatedBeforeResendEvent -= value;
            }
        }


        public event EventHandler<SessionEventArgs> SessionTerminated
        {
            add
            {
                fictionalSessionTerminatedEvent += value;
            }
            remove
            {
                fictionalSessionTerminatedEvent -= value;
            }
        }

        public TimeSpan ResendDelay { get; set; } = TimeSpan.FromSeconds(7);

        /// <summary>
        /// Grace period for resend delay if recent realtime call.
        /// </summary>
        public TimeSpan ResendSuspensionGracePeriod { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Delay to allow current realtime calls
        /// </summary>
        public TimeSpan ResendSuspensionDelay { get; set; } = TimeSpan.FromMilliseconds(300);

        public bool DebugLogResendMessages { get; set; } = false;

        /// <summary>
        /// Ensures that first all pending calls are processed before the connection created event is readirected for realtime processing
        /// Default: true (queue behaviour)
        /// </summary>
        public bool ResendInExecOrderBeforeOtherInvokes { get; set; } = true;

        /// <summary>
        /// Ignores unexpected deserialize exceptions
        /// </summary>
        public bool IgnoreDeserializeExceptions { get; set; }

        public TimeSpan RequestTimeout { get => underlyingCom.RequestTimeout; set => underlyingCom.RequestTimeout = value; }

        public ISession RealUnderlyingSession => realUnderlyingSession;

        public PersistentMethod RegisterPersistentMethod<InterfaceT>(string methodName)
        {
            return RegisterPersistentMethod(typeof(InterfaceT), methodName);
        }

        public PersistentMethod RegisterPersistentMethod(Type interfaceType, string methodName)
        {
            Dictionary<string, PersistentMethod> methods;
            if (!persistentMethods.TryGetValue(interfaceType, out methods))
            {
                methods = new Dictionary<string, PersistentMethod>();
                persistentMethods.Add(interfaceType, methods);
            }

            var result = new PersistentMethod(interfaceType, methodName);
            methods.Add(result.MethodName, result);

            return result;
        }


        public void Init()
        {
            underlyingCom.Init();
        }

        public void RaiseFictionalSession()
        {
            if (RedirectSessionEvents)
                throw new NotSupportedException("RaiseFictionalSession not supported in RedirectSessionEvents mode!");

            if (fictionalSessionCreatedEvent != null)
                fictionalSessionCreatedEvent(this, new SessionEventArgs(fictionalSession, fictionalSessionContract));
        }

        private async void UnderlyingCom_SessionCreated(object sender, SessionEventArgs e)
        {
            try
            {
                if (sessionCreatedBeforeResendEvent != null)
                    sessionCreatedBeforeResendEvent(sender, e);

                lock (syncLock)
                {
                    // release opened file
                    if (persistMsgFile != null)
                    {
                        persistMsgFile.Close();
                        persistMsgFile = null;
                        persistMsgFilePath = null;

                        Logger.Debug("Current pending file closed");
                    }

                    this.realUnderlyingSession = e.Session;
                }

                var resendTask = Task.Run(() => ResendPendingMethodInvokes(e.Session));

                if (ResendInExecOrderBeforeOtherInvokes)
                {
                    Logger.Debug("Wait for pending file resend");

                    await resendTask.ConfigureAwait(false);
                }

                if (RedirectSessionEvents)
                {
                    if (fictionalSessionCreatedEvent != null)
                        fictionalSessionCreatedEvent(sender, e);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }

        private void UnderlyingCom_SessionTerminated(object sender, SessionEventArgs e)
        {
            lock (syncLock)
            {
                realUnderlyingSession = null;
            }

            if (RedirectSessionEvents)
            {
                if (fictionalSessionTerminatedEvent != null)
                    fictionalSessionTerminatedEvent(sender, e);
            }
        }

        public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            var realSession = realUnderlyingSession;

            bool methodAlreadyPersisted = false;
            if (realSession != null && realSession.IsActive)
            {
                PersistentMethod pm = null;
                try
                {
                    lastActiveInvokeUtc = DateTime.UtcNow;

                    if (TryGetPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name, out pm))
                    {
                        if (pm.Transaction != null)
                        {
                            PersistPendingMethodInvoke(invokeInfo, parameters, pm, true, false);
                            methodAlreadyPersisted = true;
                        }
                    }

                    var returnValue = underlyingCom.InvokeMethod(source, invokeInfo, realSession, parameters);

                    if (pm != null
                        && pm.Transaction != null
                        && pm.Transaction.CurrentTransaction != null
                        && pm.Transaction.CommitTransactionMethod == pm)
                    {
                        // all methods are sent > commit online transaction
                        pm.Transaction.CommitOnlineTransaction();

                        Logger.Debug($"Online write transaction committed - method: {pm.MethodName}");
                    }

                    return returnValue;
                }
                catch (TimeoutException timeoutEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, timeoutEx, methodAlreadyPersisted);
                }
                catch (IOException ioExc)
                {
                    return PersistOrThrow(invokeInfo, parameters, ioExc, methodAlreadyPersisted);
                }
                catch (OperationCanceledException operationCancelledEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, operationCancelledEx, methodAlreadyPersisted);
                }
                catch (AggregateException aggExc)
                {
                    // check if AggregateException contains a connection related exception
                    Exception connectionException = GetConnectionException(aggExc);

                    if (connectionException != null)
                    {
                        return PersistOrThrow(invokeInfo, parameters, connectionException, methodAlreadyPersisted);
                    }
                    else
                    {
                        AbortTransactionBecauseFunctionalOnlineException(pm);

                        throw;
                    }
                }
                catch (Exception)
                {
                    AbortTransactionBecauseFunctionalOnlineException(pm);

                    throw;
                }
            }
            else if (TryGetPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name, out PersistentMethod persMeth))
            {
                // persist async request in file
                // continue processing
                return PersistPendingMethodInvoke(invokeInfo, parameters, persMeth, false, methodAlreadyPersisted);
            }
            else
            {
                throw new OperationCanceledException("Remote connection lost!");
            }
        }

        /// <summary>
        /// todo: implement async file write
        /// </summary>
        /// <param name="source"></param>
        /// <param name="invokeInfo"></param>
        /// <param name="session"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<object> InvokeMethodAsync(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            var realSession = realUnderlyingSession;

            bool methodAlreadyPersisted = false;
            if (realSession != null && realSession.IsActive)
            {
                PersistentMethod pm = null;
                try
                {
                    lastActiveInvokeUtc = DateTime.UtcNow;

                    if (TryGetPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name, out pm))
                    {
                        if (pm.Transaction != null)
                        {
                            PersistPendingMethodInvoke(invokeInfo, parameters, pm, true, false);
                            methodAlreadyPersisted = true;
                        }
                    }

                    var returnValue = await underlyingCom.InvokeMethodAsync(source, invokeInfo, realSession, parameters);

                    if (pm != null
                        && pm.Transaction != null
                        && pm.Transaction.CurrentTransaction != null
                        && pm.Transaction.CommitTransactionMethod == pm)
                    {
                        // all methods are sent > commit online transaction
                        pm.Transaction.CommitOnlineTransaction();

                        Logger.Debug($"Online write transaction committed - method: {pm.MethodName}");
                    }

                    return returnValue;
                }
                catch (TimeoutException timeoutEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, timeoutEx, methodAlreadyPersisted);
                }
                catch (IOException ioExc)
                {
                    return PersistOrThrow(invokeInfo, parameters, ioExc, methodAlreadyPersisted);
                }
                catch (OperationCanceledException operationCancelledEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, operationCancelledEx, methodAlreadyPersisted);
                }
                catch (AggregateException aggExc)
                {
                    // check if AggregateException contains a connection related exception
                    Exception connectionException = GetConnectionException(aggExc);

                    if (connectionException != null)
                    {
                        return PersistOrThrow(invokeInfo, parameters, connectionException, methodAlreadyPersisted);
                    }
                    else
                    {
                        AbortTransactionBecauseFunctionalOnlineException(pm);

                        throw;
                    }
                }
                catch (Exception)
                {
                    AbortTransactionBecauseFunctionalOnlineException(pm);

                    throw;
                }
            }
            else if (TryGetPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name, out PersistentMethod persMeth))
            {
                // persist async request in file
                // continue processing
                return PersistPendingMethodInvoke(invokeInfo, parameters, persMeth, false, methodAlreadyPersisted);
            }
            else
            {
                throw new OperationCanceledException("Remote connection lost!");
            }
        }



        private static Exception GetConnectionException(AggregateException aggExc)
        {
            Exception connectionException = null;
            foreach (var innerEx in aggExc.InnerExceptions)
            {
                if (innerEx is TimeoutException || innerEx is OperationCanceledException || innerEx is IOException)
                {
                    connectionException = innerEx;
                    break;
                }
            }

            return connectionException;
        }

        private object PersistOrThrow(IInvokeMethodInfo invokeInfo, object[] parameters, Exception exception, bool isPersistet)
        {
            if (TryGetPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name, out PersistentMethod pm))
            {
                // persist
                return PersistPendingMethodInvoke(invokeInfo, parameters, pm, false, isPersistet);
            }
            else
            {
                AbortTransactionBecauseFunctionalOnlineException(pm);

                throw exception;
            }
        }


        private object PersistPendingMethodInvoke(IInvokeMethodInfo invokeInfo, object[] parameters, PersistentMethod persistentMeth, bool isOnlineTry, bool isPersistet)
        {
            bool isTransaction = persistentMeth.Transaction != null;

            if (!isPersistet)
            {
                if (isTransaction)
                {
                    // always save persistent transaction methods to replay all methods no matter where it failed
                    if (persistentMeth.Transaction.BeginTransactionMethod == persistentMeth)
                    {
                        // online transaction start
                        Logger.Debug($"Begin online write transaction - method: {persistentMeth.MethodName}");

                        persistentMeth.Transaction.BeginTransaction();
                    }
                }

                GenericMessage pMsg = new GenericMessage(0, invokeInfo, parameters, false);

                byte[] msgBytes = underlyingCom.Serializer.SerializeToBytes(pMsg, this);

                if (msgBytes.Length > 0)
                {
                    lock (syncLock)
                    {
                        DateTime t = DateTime.Now;

                        string dir = Path.GetFullPath(DirectoryPath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        if (isTransaction)
                        {
                            var trx = persistentMeth.Transaction.CurrentTransaction;
                            if (persistentMeth.Transaction.CurrentTransaction.CurrentWriteStream == null)
                            {
                                trx.CurrentWritePath = Path.Combine(dir, $"MessageStore-Trx_{persistentMeth.Transaction.Name}-{t.ToString("yyyyMMdd_HHmmss_ffff")}.pend");

                                Logger.Debug("Create persistent transaction file: " + trx.CurrentWritePath);

                                trx.CurrentWriteStream = new FileStream(trx.CurrentWritePath, FileMode.Append, FileAccess.Write);
                            }

                            WritePersistentMessage(trx.CurrentWriteStream, persistentMeth, isOnlineTry, isTransaction, msgBytes);
                        }
                        else
                        {
                            if (persistMsgFile == null)
                            {
                                this.persistMsgFilePath = Path.Combine(dir, $"MessageStore-{t.ToString("yyyyMMdd_HHmmss_ffff")}.pend");

                                Logger.Debug("Create persistent file: " + persistMsgFilePath);

                                persistMsgFile = new FileStream(persistMsgFilePath, FileMode.Append, FileAccess.Write);
                            }

                            WritePersistentMessage(persistMsgFile, persistentMeth, isOnlineTry, isTransaction, msgBytes);
                        }
                    }
                }
            }

            if (isOnlineTry == false && isTransaction)
            {
                // Oflline transaction
                if (persistentMeth.Transaction.CurrentTransaction != null
                     && persistentMeth.Transaction.CommitTransactionMethod == persistentMeth)
                {
                    // Offline call for commit transaction method

                    // clear previous succesfully performed online calls
                    persistentMeth.Transaction.CurrentTransaction.ClearSendIndicatorPositions();

                    persistentMeth.Transaction.CommitTransaction();

                    Logger.Debug($"Offline write transaction committed - method: {persistentMeth.MethodName}");
                }
            }

            if (isOnlineTry)
            {
                // return value never used with active connection
                return null;
            }
            else
            {
                var returnType = invokeInfo.InterfaceMethod.ReturnType;
                if (!returnType.Equals(typeof(void)) && returnType.IsValueType)
                {
                    object defaultValue = Activator.CreateInstance(returnType);
                    return defaultValue;
                }
                else
                {
                    return null;
                }
            }
        }

        private void WritePersistentMessage(FileStream fs, PersistentMethod persistentMeth, bool isOnlineTry, bool isTransaction, byte[] msgBytes)
        {
            long indicatorPos = fs.Position;
            if (isOnlineTry && isTransaction && persistentMeth.Transaction.CurrentTransaction != null)
            {
                // after succesfully send set "sent indicator" position to flag all if transaction is comitted without connection fails
                persistentMeth.Transaction.CurrentTransaction.AddSendIndicatorPosition(fs, indicatorPos);
            }
            fs.WriteByte(NotSendByte);    // sent = false
            byte[] messageLengthBytes = BitConverter.GetBytes(msgBytes.Length);
            fs.Write(messageLengthBytes, 0, messageLengthBytes.Length);  // message length
            fs.Write(msgBytes, 0, msgBytes.Length);
            fs.Flush();

            //Logger.Debug($"Write {persistentMeth.MethodName}; Ind. pos: {indicatorPos}; dataLength: {msgBytes.Length}");
        }

        private async Task ResendPendingMethodInvokes(ISession newSession)
        {
            if (Interlocked.Exchange(ref processResendLock, 1) == 0)    // do not execute resend parallel
            {
                try
                {
                    if (ResendInExecOrderBeforeOtherInvokes)
                    {
                        if (resendTryCount > 5
                            && lastResendTryUtc.HasValue
                            && (DateTime.UtcNow - lastResendTryUtc.Value).TotalSeconds < 30)
                        {
                            Logger.Warn("Wait 40 sec. to prevent loop resends because of recent new connection fail!");
                            await Task.Delay(TimeSpan.FromSeconds(40));
                        }
                    }
                    else
                    {
                        // pause to allow realtime calls going first
                        await Task.Delay(ResendDelay);
                    }

                    lastResendTryUtc = DateTime.UtcNow;
                    resendTryCount++;


                    string dir = Path.GetFullPath(DirectoryPath);

                    Logger.Debug($"Check pending files in directory \"{dir}\"");

                    if (Directory.Exists(dir))
                    {
                        List<string> files = new List<string>(Directory.GetFiles(dir, "*.pend"));

                        Logger.Debug($"{files.Count} pending file(s) found");

                        if (files.Count > 0)
                        {
                            files.Sort();

                            for (int i = 0; i < files.Count; i++)
                            {
                                string pendFilePath = files[i];

                                if (pendFilePath == this.persistMsgFilePath)
                                {
                                    // always skip active write file resend (should never happen)
                                    Logger.Warn($"Skip resend of active write file: {pendFilePath}");
                                    continue;
                                }

                                if (await ResendFile(pendFilePath, newSession) == false)
                                {
                                    if (newSession.IsActive)
                                    {
                                        Logger.Warn($"Resend failed but session still active! Retry once after 10 seconds...");

                                        await Task.Delay(TimeSpan.FromSeconds(10));

                                        if (await ResendFile(pendFilePath, newSession) == false)
                                        {
                                            Logger.Info($"Retry resend unseccussfull. Skip until next session. Session: {newSession}; IsActive: {newSession?.IsActive}");

                                            return;     // skip on conneciton lost  
                                        }
                                        else
                                        {
                                            Logger.Info($"Retry resend succesfull. Session: {newSession}");
                                        }
                                    }
                                    else
                                    {
                                        return;     // skip on conneciton lost
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Debug("Pending file directory does not exist");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                }
                finally
                {
                    Interlocked.Exchange(ref processResendLock, 0);
                }
            }
            else
            {
                Logger.Info("Skip resend because other resend in progress");
            }
        }

        /// <summary>
        /// Resends the given file
        /// </summary>
        /// <param name="pendFilePath"></param>
        /// <param name="newSession"></param>
        /// <returns>Returns <c>true</c> if no connection lost during resend; otherwise: false</returns>
        private async Task<bool> ResendFile(string pendFilePath, ISession newSession)
        {
            FileStream stream = null;
            List<TransactionDefinition> openReadTransactions = new List<TransactionDefinition>();
            try
            {
                Logger.Info($"Open pend file: {pendFilePath}");

                stream = new FileStream(pendFilePath, FileMode.Open, FileAccess.ReadWrite);

                //byte[] msgBytesPrevious = null;
                int persistentMethodReadCount = 0;
                do
                {
                    long indicatorBytePos = stream.Position;
                    byte indicator = (byte)stream.ReadByte();
                    bool alredySent = indicator == AlreadySentByte;

                    byte[] lengthBytes = new byte[4];
                    await stream.ReadAsync(lengthBytes, 0, 4).ConfigureAwait(false);
                    int msgLength = BitConverter.ToInt32(lengthBytes, 0);
                    Logger.Debug($"Read message; Indicator position: {indicatorBytePos}; dataLength: {msgLength}");

                    byte[] msgBytes = new byte[msgLength];
                    await stream.ReadAsync(msgBytes, 0, msgLength).ConfigureAwait(false);

                    //msgBytesPrevious = msgBytes;

                    if (!alredySent
                        && msgLength > 0)
                    {
                        // check if resend must be delayed because of recent realtime call
                        if (lastActiveInvokeUtc.HasValue && (DateTime.UtcNow - lastActiveInvokeUtc.Value) < ResendSuspensionGracePeriod)
                        {
                            await Task.Delay(ResendSuspensionDelay);
                        }

                        if (DebugLogResendMessages)
                            Logger.Debug($"Resend local store message: {Encoding.Default.GetString(msgBytes)}");

                        // deserialize method call
                        IGenericMessage msg = null;
                        try
                        {
                            msg = Serializer.DeserializeFromBytes(msgBytes, newSession);
                        }
                        catch (Exception ex)
                        {
                            if (IgnoreDeserializeExceptions)
                            {
                                Logger.Warn($"Ignore deserialize exception! File: {pendFilePath}; Read Number: {persistentMethodReadCount}; Details: {ex}");
                            }
                            else
                            {
                                throw;
                            }
                        }

                        // call method
                        if (msg != null)
                        {
                            Type targetType;
                            TypeService.TryGetTypeByName(msg.Target, out targetType);
                            InvokeMethodInfo invokeInfo = new InvokeMethodInfo(targetType, msg.Name);

                            object[] paramValues = (object[])msg.Payload;

                            // check transaction
                            PersistentMethod persistMethod = null;
                            bool commitTransactionAfterInvoke = false;
                            if (persistentMethods.TryGetValue(targetType, out Dictionary<string, PersistentMethod> pMethods)
                                && pMethods.TryGetValue(invokeInfo.InterfaceMethod.Name, out persistMethod)
                                && persistMethod.Transaction != null)
                            {
                                if (persistMethod.Transaction.BeginTransactionMethod.Equals(persistMethod))
                                {
                                    // begin new transaction
                                    Logger.Debug($"Begin read transaction. Method: {persistMethod.MethodName}");
                                    persistMethod.Transaction.BeginTransaction();
                                    openReadTransactions.Add(persistMethod.Transaction);
                                }
                                else if (persistMethod.Transaction.CommitTransactionMethod.Equals(persistMethod))
                                {
                                    commitTransactionAfterInvoke = true;
                                }

                                // check if method parameters must be overriden with current transaction context values
                                if (persistMethod.Transaction.CurrentTransaction != null
                                    && persistMethod.Transaction.CurrentTransaction.ContextValues != null)
                                {
                                    for (int paramIndex = 0; paramIndex < invokeInfo.ParameterInfos.Length; paramIndex++)
                                    {
                                        var pi = invokeInfo.ParameterInfos[paramIndex];

                                        var ctxValue = persistMethod.Transaction.CurrentTransaction.ContextValues.Where(cv => cv.Name == pi.Name && cv.Type == pi.ParameterType).FirstOrDefault();
                                        if (ctxValue != null)
                                        {
                                            // replace with transaction context value
                                            paramValues[paramIndex] = ctxValue.Value;
                                        }
                                    }
                                }
                            }

                            // call underlying communication layer
                            object returnValue = underlyingCom.InvokeMethod(this, invokeInfo, newSession, paramValues);

                            // post call transaction processing
                            if (commitTransactionAfterInvoke)
                            {
                                if (persistMethod.Transaction.CurrentTransaction == null)
                                {
                                    Logger.Warn($"Cannot commit transaction because start transaction method is missing in resend file! Method: {persistMethod.MethodName}");
                                }
                                else
                                {
                                    persistMethod.Transaction.CurrentTransaction.AddSendIndicatorPosition(stream, indicatorBytePos);
                                    persistMethod.Transaction.CommitTransaction();

                                    openReadTransactions.Remove(persistMethod.Transaction);

                                    Logger.Debug($"Read transaction committed. Method: {persistMethod.MethodName}");
                                }
                            }
                            else if (persistMethod != null
                                && persistMethod.TransactionResendAction != null)
                            {
                                persistMethod.Transaction.CurrentTransaction.SetTransactionValue(invokeInfo.InterfaceMethod.ReturnType, persistMethod.TransactionResendAction.ApplyToParameterName, returnValue);
                                persistMethod.Transaction.CurrentTransaction.AddSendIndicatorPosition(stream, indicatorBytePos);
                            }
                            else if (persistMethod != null
                                && persistMethod.Transaction != null
                                && persistMethod.Transaction.CurrentTransaction != null)
                            {
                                // record current transaction sent positions
                                persistMethod.Transaction.CurrentTransaction.AddSendIndicatorPosition(stream, indicatorBytePos);
                            }
                            else
                            {
                                // Flag message as sent
                                long endNextPos = stream.Position;
                                stream.Position = indicatorBytePos;
                                await stream.WriteAsync(AlreadySentByteArr, 0, 1).ConfigureAwait(false);
                                await stream.FlushAsync().ConfigureAwait(false);

                                stream.Position = endNextPos;
                            }
                        }
                    }
                    else if (alredySent)
                    {
                        Logger.Debug("Skip already sent");
                    }
                    else
                    {
                        Logger.Warn("Skip zero lenth message");
                    }

                    persistentMethodReadCount++;
                }
                while (stream.Position < stream.Length);

                DismissOpenTransactions(pendFilePath, openReadTransactions);

                // all messages in pending file sent
                stream.Close();
                stream.Dispose();
                stream = null;

                // delete file
                File.Delete(pendFilePath);

                Logger.Debug("Pending message file deleted");

                return true;
            }
            catch (OperationCanceledException operationCancel)
            {
                Logger.Warn("Connection lost during local resend");

                await Task.Delay(300);

                if (newSession.IsActive)
                {
                    Logger.Error($"Connection still active after cancel persistent resend! ExDetails: {operationCancel}");
                }

                return false;
            }
            catch (TimeoutException)
            {
                Logger.Warn("Connection lost during local resend (Timeout)");
                return false;
            }
            catch (IOException)
            {
                Logger.Warn("Connection lost during local resend (IOException)");
                return false;
            }
            catch (AggregateException aggExc)
            {
                // check if AggregateException contains a connection related exception
                Exception connectionException = GetConnectionException(aggExc);

                if (connectionException != null)
                {
                    Logger.Warn($"Connection lost during local resend ({connectionException.GetType().FullName})");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log unexpected error
                Logger.Error($"Error resending file {pendFilePath}  \nException: {ex}");

                return true; // no connection lost > continue sending next file
            }
            finally
            {
                if (stream != null)
                {
                    try
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                    catch { /*ignore*/ }
                }
            }
        }

        private void DismissOpenTransactions(string pendFilePath, List<TransactionDefinition> openReadTransactions)
        {
            if (openReadTransactions.Count > 0)
            {
                Logger.Error($"Incomplete read transaction file {pendFilePath}! Dismiss {openReadTransactions.Count} open transactions.");
                foreach (var item in openReadTransactions)
                {
                    if (item.CurrentTransaction != null)
                    {
                        item.DismissTransaction();
                    }
                }
            }
        }

        private bool TryGetPersistentMethod(Type interfaceType, string methodName, out PersistentMethod pm)
        {
            Dictionary<string, PersistentMethod> methods;
            if (persistentMethods.TryGetValue(interfaceType, out methods))
            {
                return methods.TryGetValue(methodName, out pm);
            }
            else
            {
                pm = null;
                return false;
            }
        }

        private bool IsPersistentMethod(Type interfaceType, string methodName)
        {
            Dictionary<string, PersistentMethod> methods;
            if (persistentMethods.TryGetValue(interfaceType, out methods))
            {
                return methods.ContainsKey(methodName);
            }
            else
            {
                return false;
            }
        }

        public void RegisterContainerHost(IGenericContainerHost containerHost, ILogger logger)
        {
            underlyingCom.RegisterContainerHost(containerHost, logger);

            if (!RedirectSessionEvents)
            {
                if (fictionalSession == null)
                {
                    fictionalSession = new Session(this, 0, "Fictional Persistent Session");

                    fictionalSessionContract = containerHost.CreateSessionContractInstance(fictionalSession);
                }
                else
                {
                    throw new InvalidOperationException("Container host already registered!");
                }
            }
        }

        public void Shutdown()
        {
            if (fictionalSessionTerminatedEvent != null && !RedirectSessionEvents)
                fictionalSessionTerminatedEvent(this, new SessionEventArgs(fictionalSession, fictionalSessionContract));

            underlyingCom.Shutdown();
        }

        /// <summary>
        /// Functional exception during online remote invoke.
        /// Delete pending transaction file (before re-throw in caller stack).
        /// </summary>
        /// <param name="pm"></param>
        private void AbortTransactionBecauseFunctionalOnlineException(PersistentMethod pm)
        {
            if (pm != null && pm.Transaction != null && pm.Transaction.CurrentTransaction != null)
            {
                pm.Transaction.CurrentTransaction.AbortTransaction();

                Logger?.Info("Pending transaction aborted");
            }
        }


    }
}
