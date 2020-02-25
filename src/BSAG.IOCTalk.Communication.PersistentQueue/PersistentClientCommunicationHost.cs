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

namespace BSAG.IOCTalk.Communication.PersistentQueue
{
    public class PersistentClientCommunicationHost : IGenericCommunicationService
    {
        private IGenericCommunicationService underlyingCom;
        private Dictionary<Type, HashSet<string>> persistentMethods = new Dictionary<Type, HashSet<string>>();
        private FileStream persistMsgFile;
        private object syncLock = new object();
        private EventHandler<SessionEventArgs> fictionalSessionCreatedEvent;
        private EventHandler<SessionEventArgs> sessionCreatedBeforeResendEvent;
        private EventHandler<SessionEventArgs> fictionalSessionTerminatedEvent;
        private Session fictionalSession;
        private IContract fictionalSessionContract;

        private ISession realUnderlyingSession;

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


        public void RegisterPersistentMethod<InterfaceT>(string methodName)
        {
            RegisterPersistentMethod(typeof(InterfaceT), methodName);
        }

        public void RegisterPersistentMethod(Type interfaceType, string methodName)
        {
            HashSet<string> methods;
            if (!persistentMethods.TryGetValue(interfaceType, out methods))
            {
                methods = new HashSet<string>();
                persistentMethods.Add(interfaceType, methods);
            }

            methods.Add(methodName);
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

        private void UnderlyingCom_SessionCreated(object sender, SessionEventArgs e)
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

                ResendPendingMethodInvokes(e.Session);

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
                PersistPendingMethodInvoke(invokeInfo, parameters);

                return null;    // only void methods
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
                PersistPendingMethodInvoke(invokeInfo, parameters);
                return null;    // only void methods
            }
            else
            {
                throw exception;
            }
        }

        private void PersistPendingMethodInvoke(IInvokeMethodInfo invokeInfo, object[] parameters)
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
        }

        private async void ResendPendingMethodInvokes(ISession newSession)
        {
            try
            {
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

                            try
                            {
                                Logger.Info($"Open pend file: {pendFilePath}");

                                FileStream stream = new FileStream(pendFilePath, FileMode.Open, FileAccess.ReadWrite);

                                do
                                {
                                    long indicatorBytePos = stream.Position;
                                    byte indicator = (byte)stream.ReadByte();
                                    bool alredySent = indicator == AlreadySentByte;

                                    byte[] lengthBytes = new byte[4];
                                    stream.Read(lengthBytes, 0, 4);
                                    int msgLength = BitConverter.ToInt32(lengthBytes, 0);
                                    byte[] msgBytes = new byte[msgLength];
                                    stream.Read(msgBytes, 0, msgLength);

                                    if (!alredySent
                                        && msgLength > 0)
                                    {
                                        Logger.Debug($"Resend local store message: {Encoding.Default.GetString(msgBytes)}");

                                        IGenericMessage msg = Serializer.DeserializeFromBytes(msgBytes, newSession);
                                        Type targetType;
                                        TypeService.TryGetTypeByName(msg.Target, out targetType);
                                        InvokeMethodInfo invokeInfo = new InvokeMethodInfo(targetType, msg.Name);
                                        underlyingCom.InvokeMethod(this, invokeInfo, newSession, (object[])msg.Payload);

                                        // Flag message as sent
                                        long endNextPos = stream.Position;
                                        stream.Position = indicatorBytePos;
                                        stream.Write(AlreadySentByteArr, 0, 1);
                                        stream.Flush();

                                        stream.Position = endNextPos;
                                    }
                                }
                                while (stream.Position < stream.Length);

                                // all messages in pending file sent
                                stream.Close();
                                stream.Dispose();

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
        }

        private bool IsPersistentMethod(Type interfaceType, string methodName)
        {
            HashSet<string> methods;
            if (persistentMethods.TryGetValue(interfaceType, out methods))
            {
                return methods.Contains(methodName);
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
