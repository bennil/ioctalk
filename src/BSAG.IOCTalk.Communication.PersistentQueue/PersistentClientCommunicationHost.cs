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

namespace BSAG.IOCTalk.Communication.PersistentQueue
{
    public class PersistentClientCommunicationHost : IGenericCommunicationService
    {
        private IGenericCommunicationService underlyingCom;
        private Dictionary<Type, Dictionary<string, PersistentMethod>> persistentMethods = new Dictionary<Type, Dictionary<string, PersistentMethod>>();
        private FileStream persistMsgFile;
        private object syncLock = new object();
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

        private const byte NotSendByte = 1;
        private const byte AlreadySentByte = 2;

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

        public IDataStreamLogger DataStreamLogger => underlyingCom.DataStreamLogger;

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

            if (realSession != null && realSession.IsActive)
            {
                try
                {
                    lastActiveInvokeUtc = DateTime.UtcNow;

                    return underlyingCom.InvokeMethod(source, invokeInfo, realSession, parameters);
                }
                catch (TimeoutException timeoutEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, timeoutEx);
                }
                catch (IOException ioExc)
                {
                    return PersistOrThrow(invokeInfo, parameters, ioExc);
                }
                catch (OperationCanceledException operationCancelledEx)
                {
                    return PersistOrThrow(invokeInfo, parameters, operationCancelledEx);
                }
                catch (AggregateException aggExc)
                {
                    // check if AggregateException contains a connection related exception
                    Exception connectionException = GetConnectionException(aggExc);

                    if (connectionException != null)
                    {
                        return PersistOrThrow(invokeInfo, parameters, connectionException);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (IsPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name))
            {
                // persist async request in file
                // continue processing
                return PersistPendingMethodInvoke(invokeInfo, parameters);
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

        private object PersistOrThrow(IInvokeMethodInfo invokeInfo, object[] parameters, Exception exception)
        {
            if (IsPersistentMethod(invokeInfo.InterfaceMethod.DeclaringType, invokeInfo.InterfaceMethod.Name))
            {
                // persist
                return PersistPendingMethodInvoke(invokeInfo, parameters);
            }
            else
            {
                throw exception;
            }
        }

        private object PersistPendingMethodInvoke(IInvokeMethodInfo invokeInfo, object[] parameters)
        {
            GenericMessage pMsg = new GenericMessage(0, invokeInfo, parameters, false);

            byte[] msgBytes = underlyingCom.Serializer.SerializeToBytes(pMsg, this);

            if (msgBytes.Length > 0)
            {
                lock (syncLock)
                {
                    if (persistMsgFile == null)
                    {
                        DateTime t = DateTime.Now;

                        string dir = Path.GetFullPath(DirectoryPath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        string path = Path.Combine(dir, $"MessageStore-{t.ToString("yyyyMMdd_HHmmss_ffff")}.pend");

                        persistMsgFile = new FileStream(path, FileMode.Append, FileAccess.Write);
                    }

                    persistMsgFile.WriteByte(NotSendByte);    // sent = false
                    byte[] messageLengthBytes = BitConverter.GetBytes(msgBytes.Length);
                    persistMsgFile.Write(messageLengthBytes, 0, messageLengthBytes.Length);  // message length
                    persistMsgFile.Write(msgBytes, 0, msgBytes.Length);
                    persistMsgFile.Flush();
                }
            }

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

                                FileStream stream = null;
                                try
                                {
                                    Logger.Info($"Open pend file: {pendFilePath}");

                                    stream = new FileStream(pendFilePath, FileMode.Open, FileAccess.ReadWrite);

                                    do
                                    {
                                        long indicatorBytePos = stream.Position;
                                        byte indicator = (byte)stream.ReadByte();
                                        bool alredySent = indicator == AlreadySentByte;

                                        byte[] lengthBytes = new byte[4];
                                        await stream.ReadAsync(lengthBytes, 0, 4).ConfigureAwait(false);
                                        int msgLength = BitConverter.ToInt32(lengthBytes, 0);
                                        byte[] msgBytes = new byte[msgLength];
                                        await stream.ReadAsync(msgBytes, 0, msgLength).ConfigureAwait(false);

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
                                                    Logger.Warn($"Ignore deserialize exception: {ex}");
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
                                                        persistMethod.Transaction.BeginTransaction();
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
                                                    persistMethod.Transaction.CommitTransaction();
                                                }
                                                else if (persistMethod != null
                                                    && persistMethod.TransactionResendAction != null)
                                                {
                                                    persistMethod.Transaction.CurrentTransaction.SetTransactionValue(invokeInfo.InterfaceMethod.ReturnType, persistMethod.TransactionResendAction.ApplyToParameterName, returnValue);
                                                }
                                            }

                                            // Flag message as sent
                                            long endNextPos = stream.Position;
                                            stream.Position = indicatorBytePos;
                                            await stream.WriteAsync(AlreadySentByteArr, 0, 1).ConfigureAwait(false);
                                            await stream.FlushAsync().ConfigureAwait(false);

                                            stream.Position = endNextPos;
                                        }
                                    }
                                    while (stream.Position < stream.Length);

                                    // all messages in pending file sent
                                    stream.Close();
                                    stream.Dispose();
                                    stream = null;

                                    // delete file
                                    File.Delete(pendFilePath);

                                    Logger.Debug("Pending message file deleted");
                                }
                                catch (OperationCanceledException)
                                {
                                    Logger.Warn("Connection lost during local resend");
                                    return;
                                }
                                catch (TimeoutException timeoutEx)
                                {
                                    Logger.Warn("Connection lost during local resend (Timeout)");
                                    return;
                                }
                                catch (IOException ioExc)
                                {
                                    Logger.Warn("Connection lost during local resend (IOException)");
                                    return;
                                }
                                catch (AggregateException aggExc)
                                {
                                    // check if AggregateException contains a connection related exception
                                    Exception connectionException = GetConnectionException(aggExc);

                                    if (connectionException != null)
                                    {
                                        Logger.Warn($"Connection lost during local resend ({connectionException.GetType().FullName})");
                                        return;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log unexpected error
                                    Logger.Error($"Error resending file {pendFilePath}  \nException: {ex}");
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
    }
}
