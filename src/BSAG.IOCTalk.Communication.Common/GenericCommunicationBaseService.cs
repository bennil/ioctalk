using BSAG.IOCTalk.Common.Exceptions;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Communication.Common.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BSAG.IOCTalk.Communication.Common
{
    /// <summary>
    /// IGenericCommunicationService base implementation
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-08-21
    /// </remarks>
    public class GenericCommunicationBaseService : IGenericCommunicationService
    {
        #region CommunicationHelper fields
        // ----------------------------------------------------------------------------------------
        // CommunicationHelper fields
        // ----------------------------------------------------------------------------------------

        protected const string DismissInvalidSessionMessageLogTag = "[Dismiss invalid session message]";

        /// <summary>
        /// Common communication service (can be implemented in sub class)
        /// </summary>
        protected ICommunicationBaseServiceSupport baseCommunicationServiceSupport;

        /// <summary>
        /// IOC container host
        /// </summary>
        protected IGenericContainerHost containerHost;

        /// <summary>
        /// Message serializer
        /// </summary>
        protected IGenericMessageSerializer serializer;

        /// <summary>
        /// Method info reflection cache
        /// </summary>
        protected IDictionary<int, IInvokeMethodInfo> methodInfoCache;

        /// <summary>
        /// Custom response wait handler
        /// </summary>
        protected ICustomResponseWaitHandler customResponseWaitHandler = null;

        /// <summary>
        /// Request timeout
        /// Default: 5 min
        /// </summary>
        protected TimeSpan requestTimeout = new TimeSpan(0, 3, 0);

        /// <summary>
        /// Defines the maximum time a session created client event is allowed to block the invoke processing.
        /// </summary>
        protected TimeSpan sessionCreatedEventTimeout = new TimeSpan(0, 0, 15);

        /// <summary>
        /// Technical logger
        /// </summary>
        protected ILogger logger;
        private string loggerTypeName = "BSAG.IOCTalk.Logging.BasicLogger, BSAG.IOCTalk.Logging";

        /// <summary>
        /// Data stream logger
        /// </summary>
        protected IDataStreamLogger dataStreamLogger;

        /// <summary>
        /// Log data stream switch
        /// </summary>
        protected bool logDataStream = false;
        private string dataStreamLoggerTypeName = "BSAG.IOCTalk.Logging.DataStream.DataStreamLogger, BSAG.IOCTalk.Logging";


        private long currentRequestId = 0;

        private ISession[] sessions = new ISession[0];
        protected Dictionary<int, ISession> sessionDictionary = new Dictionary<int, ISession>();
        private object sessionLockSyncObj = new object();
        private long pendingSessionCreationCount = 0;
        private string serializerTypeName = "BSAG.IOCTalk.Serialization.Json.JsonMessageSerializer, BSAG.IOCTalk.Serialization.Json";
        private InvokeThreadModel invokeThreadModel = InvokeThreadModel.CallerThread;
        private Channel<Tuple<ISession, IGenericMessage>> callerQueue;
        protected Channel<Tuple<int, byte[]>> receiverQueue;
        protected bool supportsReceiverQueue = true;
        protected bool isActive = true;

        private long receivedMessageCounter = 0;
        private long sentMessageCounter = 0;
        private long lastReceivedMessageCounter = 0;
        private long lastSentMessageCounter = 0;

        private static int lastConnectionSessionId = 0;

        // ----------------------------------------------------------------------------------------
        #endregion


        #region CommunicationHelper properties
        // ----------------------------------------------------------------------------------------
        // CommunicationHelper properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the base communication service support.
        /// </summary>
        /// <value>
        /// The base communication service support.
        /// </value>
        public ICommunicationBaseServiceSupport BaseCommunicationServiceSupport
        {
            get { return baseCommunicationServiceSupport; }
            set { baseCommunicationServiceSupport = value; }
        }

        /// <summary>
        /// Gets or sets the custom response wait handler.
        /// </summary>
        /// <value>
        /// The custom response wait handler.
        /// </value>
        public ICustomResponseWaitHandler CustomResponseWaitHandler
        {
            get { return customResponseWaitHandler; }
            set { customResponseWaitHandler = value; }
        }



        /// <summary>
        /// Gets the container host.
        /// </summary>
        /// <value>
        /// The container host.
        /// </value>
        public IGenericContainerHost ContainerHost
        {
            get { return containerHost; }
        }


        /// <summary>
        /// Gets or sets once the message serializer.
        /// </summary>
        public IGenericMessageSerializer Serializer
        {
            get { return serializer; }
            set
            {
                if (serializer != null)
                {
                    throw new Exception("Serializer can be only set once before container host initializing!");
                }

                serializer = value;
            }
        }


        /// <summary>
        /// Gets or sets the default name of the serializer type.
        /// </summary>
        /// <value>
        /// The default name of the serializer type.
        /// </value>
        public string SerializerTypeName
        {
            get { return serializerTypeName; }
            set { serializerTypeName = value; }
        }


        /// <summary>
        /// Gets or sets the name of the logger type.
        /// </summary>
        /// <value>
        /// The name of the logger type.
        /// </value>
        public string LoggerTypeName
        {
            get
            {
                return loggerTypeName;
            }
            set
            {
                this.loggerTypeName = value;
            }
        }


        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILogger Logger
        {
            get { return logger; }
        }


        /// <summary>
        /// Gets or sets the name of the default data stream logger type.
        /// </summary>
        /// <value>
        /// The name of the data stream logger type.
        /// </value>
        public string DataStreamLoggerTypeName
        {
            get
            {
                return dataStreamLoggerTypeName;
            }
            set
            {
                dataStreamLoggerTypeName = value;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether [log data stream].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [log data stream]; otherwise, <c>false</c>.
        /// </value>
        public bool LogDataStream
        {
            get { return logDataStream; }
            set { logDataStream = value; }
        }


        /// <summary>
        /// Gets the data stream logger.
        /// </summary>
        public IDataStreamLogger DataStreamLogger { get => dataStreamLogger; set => dataStreamLogger = value; }


        /// <summary>
        /// Gets the sessions.
        /// </summary>
        public ISession[] ClientSessions
        {
            get { return sessions; }
        }


        /// <summary>
        /// Gets or sets the request timeout seconds.
        /// Default: 300
        /// </summary>
        /// <value>
        /// The request timeout seconds.
        /// </value>
        public int RequestTimeoutSeconds
        {
            get { return (int)requestTimeout.TotalSeconds; }
            set { requestTimeout = new TimeSpan(0, 0, value); }
        }

        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        /// <value>
        /// The request timeout.
        /// </value>
        [XmlIgnore]
        public TimeSpan RequestTimeout
        {
            get { return requestTimeout; }
            set { requestTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the heartbeat interval time in seconds
        /// </summary>
        public int HeartbeatIntervalTimeSeconds
        {
            get { return (int)HeartbeatIntervalTime.TotalSeconds; }
            set { HeartbeatIntervalTime = new TimeSpan(0, 0, value); }
        }

        /// <summary>
        /// Gets or sets the heartbeat interval time
        /// </summary>
        [XmlIgnore]
        public TimeSpan HeartbeatIntervalTime { get; set; } = TimeSpan.Zero;


        /// <summary>
        /// Gets or sets the maximum time a session created client event is allowed to block the invoke processing in seconds.
        /// </summary>
        public int SessionCreatedEventTimeoutSeconds
        {
            get { return (int)sessionCreatedEventTimeout.TotalSeconds; }
            set { sessionCreatedEventTimeout = TimeSpan.FromSeconds(value); }
        }

        /// <summary>
        /// Gets or sets the maximum time a session created client event is allowed to block the invoke processing.
        /// Default: 15 seconds
        /// </summary>
        [XmlIgnore]
        public TimeSpan SessionCreatedEventTimeout
        {
            get { return sessionCreatedEventTimeout; }
            set { sessionCreatedEventTimeout = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use simple method description].
        /// false (default) = method names are full qualified including the invoke parameters signiture
        /// true            = only the plain method name will be transferred (no method overrides in interface classes possible)
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use simple method description]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSimpleMethodDescription { get; set; }

        /// <summary>
        /// Occurs when a session is created.
        /// </summary>
        public event EventHandler<SessionEventArgs> SessionCreated;


        /// <summary>
        /// Occurs when a session is terminated.
        /// </summary>
        public event EventHandler<SessionEventArgs> SessionTerminated;

        /// <summary>
        /// Gets or sets the invoke thread model.
        /// </summary>
        /// <value>
        /// The invoke thread model.
        /// </value>
        public InvokeThreadModel InvokeThreadModel
        {
            get
            {
                return invokeThreadModel;
            }
            set
            {
                if (invokeThreadModel != value)
                {
                    if (this.containerHost != null)
                    {
                        throw new InvalidOperationException("The InvokeThreadModel can't be changed after container registration!");
                    }

                    invokeThreadModel = value;
                }
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region CommunicationHelper methods
        // ----------------------------------------------------------------------------------------
        // CommunicationHelper methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Inits the communication service.
        /// </summary>
        public virtual void Init()
        {
            isActive = true;
        }

        /// <summary>
        /// Communication service shutdown
        /// </summary>
        public virtual void Shutdown()
        {
            if (isActive)
            {
                isActive = false;

                if (receiverQueue != null)
                    receiverQueue.Writer.Complete();

                if (callerQueue != null)
                    callerQueue.Writer.Complete();   // release caller queue thread

                if (DataStreamLogger != null)
                    DataStreamLogger.Dispose();
            }
        }

        /// <summary>
        /// Creates the session.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="description">The description.</param>
        public void CreateSession(int sessionId, string description)
        {
            Interlocked.Increment(ref pendingSessionCreationCount);
            Session newSession = null;
            IContract sessionContract = null;
            try
            {
                // create session
                description += $" ({containerHost.Name})";
                newSession = new Session(this, sessionId, description);

                if (newSession != null)
                {
                    lock (sessionLockSyncObj)
                    {
                        if (!sessionDictionary.ContainsKey(newSession.SessionId))
                        {
                            sessionDictionary.Add(newSession.SessionId, newSession);
                            sessions = sessionDictionary.Values.ToArray<ISession>();
                        }
                        else
                        {
                            logger.Error(string.Format("Session ID {0} already exists!", newSession.SessionId));
                        }

                        // create contract session
                        sessionContract = containerHost.CreateSessionContractInstance(newSession);
                    }

                    if (dataStreamLogger != null)
                    {
                        dataStreamLogger.OnSessionCreated(newSession);
                    }

                    newSession.OnSessionInitalized(sessionContract);

                    logger.Info(string.Format("Session created - {0} - {1}", newSession.SessionId, newSession.Description));


                    // call session created event
                    if (SessionCreated != null)
                    {
                        // event must run async because otherwise if any method call is made by the client within the SessionCreated event
                        // the code will end in a deadleack because the tcp receiving thread is only started after this method is finished
                        Task.Run(() =>
                        {
                            OnSessionCreatedInternal(new SessionEventArgs(newSession, sessionContract));
                        });
                    }

                    if (HeartbeatIntervalTime.TotalSeconds > 0)
                    {
                        ActivateSendHeartbeats(newSession);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref pendingSessionCreationCount);
            }
        }


        private void OnSessionCreatedInternal(SessionEventArgs newSessionArgs)
        {
            try
            {
                if (SessionCreated != null)
                {
                    SessionCreated(this, newSessionArgs);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }


        /// <summary>
        /// Processes the session terminated.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>Returns <c>true</c> if the session was found and terminated; otherwise <c>false</c>.</returns>
        public bool ProcessSessionTerminated(int sessionId, string source)
        {
            lock (sessionLockSyncObj)
            {
                ISession session = null;
                if (sessionDictionary.TryGetValue(sessionId, out session))
                {
                    session.Close();

                    sessionDictionary.Remove(session.SessionId);
                    sessions = sessionDictionary.Values.ToArray<ISession>();

                    logger.Info($"Session terminated - {session.SessionId} - {session.Description} - {source}");

                    if (dataStreamLogger != null)
                    {
                        dataStreamLogger.OnSessionTerminated(session);
                    }

                    if (SessionTerminated != null)
                    {
                        SessionTerminated(this, new SessionEventArgs(session, session.Contract));
                    }

                    serializer?.DisposeSession(sessionId);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        public virtual void RegisterContainerHost(IGenericContainerHost containerHost, ILogger logger)
        {
            if (this.containerHost != null)
            {
                throw new Exception("A container host is already registered.");
            }
            this.logger = logger;

            if (baseCommunicationServiceSupport == null)
            {
                if (this is ICommunicationBaseServiceSupport)
                {
                    // Interface is implemented in the inheritance
                    baseCommunicationServiceSupport = (ICommunicationBaseServiceSupport)this;
                }
                else
                {
                    throw new NullReferenceException("BaseCommunicationServiceSupport property must be set or the ICommunicationBaseServiceSupport interface must be implemented in the inheritance!");
                }
            }

            this.containerHost = containerHost;

            if (this.serializer == null)
            {
                // get serializer instance
                if (string.IsNullOrEmpty(SerializerTypeName))
                {
                    throw new ArgumentNullException("The SerializerTypeName is not defined!");
                }

                Type serializerType;
                if (!TypeService.TryGetTypeByName(SerializerTypeName, out serializerType))
                {
                    throw new TypeLoadException("Cant find the serializer type \"" + SerializerTypeName + "\"!");
                }

                this.serializer = (IGenericMessageSerializer)TypeService.CreateInstance(serializerType);
            }

            if (this.logger == null)
            {
                // get logger instance
                Type loggerType;
                if (string.IsNullOrEmpty(LoggerTypeName))
                {
                    throw new ArgumentNullException("The LoggerTypeName is not defined!");
                }

                if (!TypeService.TryGetTypeByName(LoggerTypeName, out loggerType))
                {
                    throw new TypeLoadException("Cant find the logger type \"" + LoggerTypeName + "\"!");
                }

                this.logger = (ILogger)TypeService.CreateInstance(loggerType);
            }

            if (logDataStream
                && this.dataStreamLogger == null)
            {
                // get data stream logger instance if defined
                if (string.IsNullOrEmpty(DataStreamLoggerTypeName))
                {
                    throw new ArgumentNullException("The DataStreamLoggerTypeName is not defined!");
                }

                Type dataStreamLoggerType;
                if (!TypeService.TryGetTypeByName(DataStreamLoggerTypeName, out dataStreamLoggerType))
                {
                    throw new TypeLoadException("Cant find the data stream logger type \"" + DataStreamLoggerTypeName + "\"!");
                }

                this.dataStreamLogger = (IDataStreamLogger)TypeService.CreateInstance(dataStreamLoggerType);
            }

            if (InvokeThreadModel == IOCTalk.Common.Interface.Communication.InvokeThreadModel.CallerThread)
            {
                BoundedChannelOptions channelOptions = new BoundedChannelOptions(2048); // todo: verify max count
                channelOptions.FullMode = BoundedChannelFullMode.Wait;
                channelOptions.SingleReader = true;
                channelOptions.SingleWriter = true;

                callerQueue = Channel.CreateBounded<Tuple<ISession, IGenericMessage>>(channelOptions);
                Task.Run(CallerThreadProcess);
            }

            if (InvokeThreadModel == IOCTalk.Common.Interface.Communication.InvokeThreadModel.IndividualTask)
            {
                methodInfoCache = new ConcurrentDictionary<int, IInvokeMethodInfo>();
            }
            else
            {
                methodInfoCache = new Dictionary<int, IInvokeMethodInfo>();
            }

            this.serializer.RegisterContainerHost(containerHost);

            if (supportsReceiverQueue)
            {
                BoundedChannelOptions receiverChannelOptions = new BoundedChannelOptions(2048); // todo: verify max count
                receiverChannelOptions.FullMode = BoundedChannelFullMode.Wait;
                receiverChannelOptions.SingleReader = true;
                receiverChannelOptions.SingleWriter = true;

                receiverQueue = Channel.CreateBounded<Tuple<int, byte[]>>(receiverChannelOptions);
                Task.Run(ReceiverProcessTask);
            }
        }


        /// <summary>
        /// Invokes a remote interface method by a given lambda method expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        //public object InvokeMethod<T>(object source, Expression<Action<T>> expression)
        //{
        //    MethodCallExpression methodCall = expression.Body as MethodCallExpression;
        //    if (methodCall == null)
        //    {
        //        throw new Exception("Lambda expression must be a method call!");
        //    }
        //    object[] parameters = null;
        //    List<ParameterInfo> outParameters = null;
        //    List<FieldInfo> outParameterFieldInfos = null;
        //    List<object> outParameterTargetValues = null;
        //    if (methodCall.Arguments.Count > 0)
        //    {
        //        parameters = new object[methodCall.Arguments.Count];
        //        ParameterInfo[] parameterInfos = methodCall.Method.GetParameters();
        //        for (int i = 0; i < methodCall.Arguments.Count; i++)
        //        {
        //            object argument = methodCall.Arguments[i];

        //            // get value from lambda expression
        //            MemberExpression memberExpression = argument as MemberExpression;
        //            FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
        //            ConstantExpression constantExpression = (memberExpression.Expression as ConstantExpression);
        //            parameters[i] = fieldInfo.GetValue(constantExpression.Value);

        //            // check out parameter
        //            var parameterInfo = parameterInfos[i];
        //            if (parameterInfo.IsOut)
        //            {
        //                if (outParameterFieldInfos == null)
        //                {
        //                    outParameters = new List<ParameterInfo>();
        //                    outParameterFieldInfos = new List<FieldInfo>();
        //                    outParameterTargetValues = new List<object>();
        //                }

        //                outParameters.Add(parameterInfo);
        //                outParameterFieldInfos.Add(fieldInfo);
        //                outParameterTargetValues.Add(constantExpression.Value);
        //            }
        //        }
        //    }

        //    try
        //    {
        //        // todo: directly call InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters)
        //        return InvokeMethod(source, methodCall.Method, parameters);
        //    }
        //    finally
        //    {
        //        if (outParameters != null)
        //        {
        //            for (int i = 0; i < outParameters.Count; i++)
        //            {
        //                object outValue = parameters[outParameters[i].Position];
        //                outParameterFieldInfos[i].SetValue(outParameterTargetValues[i], outValue);
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Invokes a remote interface method.
        ///// </summary>
        ///// <param name="source">The source.</param>
        ///// <param name="method">The method.</param>
        ///// <param name="parameters">The parameters.</param>
        ///// <returns></returns>
        //public virtual object InvokeMethod(object source, MethodInfo method, object[] parameters)
        //{
        //    return InvokeMethod(source, new InvokeMethodInfo(method), parameters);
        //}



        ///// <summary>
        ///// Invokes a remote interface method.
        ///// </summary>
        ///// <param name="source">The source.</param>
        ///// <param name="invokeInfo">The invoke info (cached reflection infos).</param>
        ///// <param name="parameters">The parameters.</param>
        ///// <returns></returns>
        //public virtual object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters)
        //{
        //    ISession session = containerHost.GetSessionByServiceInstance(source);
        //    return InvokeMethod(source, invokeInfo, session, parameters);
        //}

        /// <summary>
        /// Invokes a remote interface method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="invokeInfo">The invoke info.</param>
        /// <param name="session">The target session</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            if (session == null || !session.IsActive)
            {
                if (session == null)
                    throw new OperationCanceledException("Remote connction lost");
                else
                    throw new OperationCanceledException($"Remote connction lost - Session: {session.SessionId} {session.Description}");

            }

            long requestId = Interlocked.Increment(ref currentRequestId);

            bool responseExpected = true;

            if (invokeInfo.IsVoidReturnMethod && invokeInfo.IsAsyncVoidRemoteInvoke(containerHost))
            {
                if (baseCommunicationServiceSupport.IsAsyncVoidSendCurrentlyPossible(session))
                {
                    responseExpected = false;
                }
            }

            GenericMessage requestMsg;
            if (UseSimpleMethodDescription)
            {
                // transfer only plain method name without invoke type parameter signature
                requestMsg = new GenericMessage(requestId, invokeInfo.InterfaceMethod, parameters, responseExpected);
            }
            else
            {
                requestMsg = new GenericMessage(requestId, invokeInfo, parameters, responseExpected);
            }

            InvokeState invokeState = null;
            if (responseExpected)
            {
                // create state object for request/response mapping
                invokeState = new InvokeState();
                invokeState.RequestMessage = requestMsg;

                invokeState.WaitHandle = new ManualResetEventSlim(false);
                invokeState.Method = invokeInfo.InterfaceMethod;
                invokeState.MethodSource = invokeInfo;
                invokeState.Session = session;

                if (invokeInfo.OutParameters != null)
                    invokeState.OutParameterValues = new object[invokeInfo.OutParameters.Length];

                try
                {
                    session.PendingRequests.Add(requestId, invokeState);
                }
                catch (NullReferenceException)
                {
                    // session lost check
                    if (!session.IsActive)
                    {
                        throw new OperationCanceledException("Remote connction lost - Session ID: " + session.SessionId);
                    }
                    else
                    {
                        throw;
                    }
                }
            }


            baseCommunicationServiceSupport.SendMessage(requestMsg, session.SessionId, invokeInfo);
            sentMessageCounter++;

            if (responseExpected)
            {
                // block thread and wait for return object response
                TimeSpan timeout = invokeInfo.CustomTimeout.HasValue ? invokeInfo.CustomTimeout.Value : requestTimeout;

                if (invokeThreadModel == IOCTalk.Common.Interface.Communication.InvokeThreadModel.CallerThread)
                {
                    // Use caller thread wait function (custom response handler is in CallerThread mode not supported)
                    WaitForResponseAndProcessOtherCalls(invokeState, timeout);
                }
                else
                {
                    if (customResponseWaitHandler != null)
                    {
                        customResponseWaitHandler.WaitForResponse(invokeState);
                    }
                    else
                    {
                        if (!invokeState.WaitHandle.Wait(timeout))
                        {
                            throw new TimeoutException(string.Format("Request timeout occured! Request: {0}; timeout time: {1}; session ID: {2}; request ID: {3}", invokeState.Method.Name, requestTimeout, session.SessionId, requestId));
                        }
                    }
                }

                invokeState.WaitHandle.Dispose();

                if (invokeState.Exception != null)
                {
                    // rethrow remote exception
                    PreserveStackTrace(invokeState.Exception);
                    throw invokeState.Exception;
                }
                else
                {
                    if (invokeInfo.OutParameters != null)
                    {
                        // set out parameter
                        for (int outParamIndex = 0; outParamIndex < invokeInfo.OutParameters.Length; outParamIndex++)
                        {
                            ParameterInfo paramInfo = invokeInfo.OutParameters[outParamIndex];
                            parameters[paramInfo.Position] = invokeState.OutParameterValues[outParamIndex];
                        }
                    }

                    return invokeState.ReturnObject;
                }
            }
            else
            {
                return null;
            }
        }


        public async Task<object> InvokeMethodAsync(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            if (session == null || !session.IsActive)
            {
                if (session == null)
                    throw new OperationCanceledException("Remote connction lost");
                else
                    throw new OperationCanceledException($"Remote connction lost - Session: {session.SessionId} {session.Description}");

            }

            long requestId = Interlocked.Increment(ref currentRequestId);

            bool responseExpected = true;

            if (invokeInfo.IsVoidReturnMethod && invokeInfo.IsAsyncVoidRemoteInvoke(containerHost))
            {
                if (baseCommunicationServiceSupport.IsAsyncVoidSendCurrentlyPossible(session))
                {
                    responseExpected = false;
                }
            }

            GenericMessage requestMsg = new GenericMessage(requestId, invokeInfo, parameters, responseExpected);


            InvokeState invokeState = null;
            if (responseExpected)
            {
                // create state object for request/response mapping
                invokeState = new InvokeState();
                invokeState.RequestMessage = requestMsg;

                invokeState.WaitHandle = new ManualResetEventSlim(false);
                invokeState.Method = invokeInfo.InterfaceMethod;
                invokeState.MethodSource = invokeInfo;
                invokeState.Session = session;

                if (invokeInfo.OutParameters != null)
                    invokeState.OutParameterValues = new object[invokeInfo.OutParameters.Length];

                try
                {
                    session.PendingRequests.Add(requestId, invokeState);
                }
                catch (NullReferenceException)
                {
                    // session lost check
                    if (!session.IsActive)
                    {
                        throw new OperationCanceledException("Remote connction lost - Session ID: " + session.SessionId);
                    }
                    else
                    {
                        throw;
                    }
                }
            }


            await baseCommunicationServiceSupport.SendMessageAsync(requestMsg, session.SessionId, invokeInfo);
            sentMessageCounter++;

            if (responseExpected)
            {
                // block thread and wait for return object response
                TimeSpan timeout = invokeInfo.CustomTimeout.HasValue ? invokeInfo.CustomTimeout.Value : requestTimeout;

                await WaitHandleTaskAsyncResponseHelper(invokeState.WaitHandle.WaitHandle, timeout, invokeState, session.SessionId, requestId);

                invokeState.WaitHandle.Dispose();

                if (invokeState.Exception != null)
                {
                    // rethrow remote exception
                    PreserveStackTrace(invokeState.Exception);
                    throw invokeState.Exception;
                }
                else
                {
                    if (invokeInfo.OutParameters != null)
                    {
                        // set out parameter
                        for (int outParamIndex = 0; outParamIndex < invokeInfo.OutParameters.Length; outParamIndex++)
                        {
                            ParameterInfo paramInfo = invokeInfo.OutParameters[outParamIndex];
                            parameters[paramInfo.Position] = invokeState.OutParameterValues[outParamIndex];
                        }
                    }

                    return invokeState.ReturnObject;
                }
            }
            else
            {
                return null;
            }
        }




        /// <summary>
        /// Processes the received message string.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="messageString">The message string.</param>
        public async ValueTask ProcessReceivedMessageString(int sessionId, string messageString)
        {
            try
            {
                ISession session;
                if (!sessionDictionary.TryGetValue(sessionId, out session))
                {
                    WaitForPendingSessionById(sessionId, out session);

                    if (session == null)
                    {
                        // session terminated -> ignore packets
                        if (logDataStream)
                        {
                            dataStreamLogger.LogStreamMessage(sessionId, true, DismissInvalidSessionMessageLogTag + messageString);
                        }
                        return;
                    }
                }

                if (logDataStream)
                {
                    dataStreamLogger.LogStreamMessage(sessionId, true, messageString);
                }

                IGenericMessage message = serializer.DeserializeFromString(messageString, session);

                await ProcessReceivedMessage(session, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Unexpected error during message processing! Message: \"{0}\" \n Exception: {1}", messageString, ex.ToString()));
            }
        }



        /// <summary>
        /// Processes the received message bytes.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="messageBytes">The message bytes.</param>
        public async ValueTask ProcessReceivedMessageBytes(int sessionId, byte[] messageBytes)
        {
            try
            {
                ISession session;
                if (!sessionDictionary.TryGetValue(sessionId, out session))
                {
                    WaitForPendingSessionById(sessionId, out session);

                    if (session == null)
                    {
                        // session terminated -> ignore packets
                        if (logDataStream)
                        {
                            dataStreamLogger.LogStreamMessage(sessionId, true, DismissInvalidSessionMessageLogTag + Encoding.UTF8.GetString(messageBytes));
                        }
                        return;
                    }
                }

                if (logDataStream)
                {
                    dataStreamLogger.LogStreamMessage(sessionId, true, messageBytes, serializer.MessageFormat != IOCTalk.Common.Interface.Communication.Raw.RawMessageFormat.JSON);
                }

                IGenericMessage message = serializer.DeserializeFromBytes(messageBytes, messageBytes.Length, session, sessionId);

                await ProcessReceivedMessage(session, message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Unexpected error during message processing! Message: \"{0}\" \n Exception: {1}", Encoding.UTF8.GetString(messageBytes), ex.ToString()));
            }
        }

        private void WaitForPendingSession(ISession session)
        {
            if (!session.IsInitialized)
            {
                if (logger != null)
                    logger.Debug($"Wait for pending session {session.SessionId}");

                DateTime startWaitTime = DateTime.UtcNow;
                bool warnLogged = false;
                while (!session.IsInitialized)
                {
                    var timeDiff = (DateTime.UtcNow - startWaitTime);
                    if (timeDiff.TotalSeconds > 20)
                    {
                        if (session.PendingRequests.Count > 0)
                        {
                            throw new TimeoutException("Session creation wait timeout occured! Probably deadlock because of crossed constructor remote calls. Implement ISupportInitialize interface to call after session created.");
                        }
                        else
                        {
                            throw new TimeoutException("Session creation wait timeout occured!");
                        }
                    }
                    else if (timeDiff.TotalSeconds > 3
                        && !warnLogged)
                    {
                        logger.Warn($"Long waiting for session initialization! Session Id {session.SessionId}");
                        warnLogged = true;
                    }

                    Thread.Sleep(50);
                }

                if (warnLogged)
                {
                    logger.Info($"Pending session creation (SID: {session.SessionId}) initialization completed. Continue processing");
                }
            }
        }

        protected void WaitForPendingSessionById(int sessionId, out ISession session)
        {
            if (Interlocked.Read(ref pendingSessionCreationCount) > 0)
            {
                DateTime startUtc = DateTime.UtcNow;
                bool warnLogged = false;

                // wait for pending session creation
                while (Interlocked.Read(ref pendingSessionCreationCount) > 0)
                {
                    Thread.Sleep(50);

                    if ((DateTime.UtcNow - startUtc).TotalSeconds > 3
                        && !warnLogged)
                    {
                        logger.Warn($"Long pending session creation! Waiting for session Id {sessionId}");
                        warnLogged = true;
                    }
                }

                if (warnLogged)
                {
                    logger.Info($"Pending session creation completed. Continue processing");
                }

                // recheck session existance
                sessionDictionary.TryGetValue(sessionId, out session);
            }
            else
            {
                session = null;
            }
        }

        /// <summary>
        /// Processes the received message.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="message">The message.</param>
        public async ValueTask ProcessReceivedMessage(ISession session, IGenericMessage message)
        {
            receivedMessageCounter++;

            switch (message.Type)
            {
                case MessageType.MethodInvokeRequest:
                case MessageType.AsyncMethodInvokeRequest:

                    switch (invokeThreadModel)
                    {
                        case InvokeThreadModel.CallerThread:
                            // enqueue caller thread invoke
                            var containerItem = new Tuple<ISession, IGenericMessage>(session, message);
                            if (!callerQueue.Writer.TryWrite(containerItem))
                            {
                                await callerQueue.Writer.WriteAsync(containerItem).ConfigureAwait(false);
                            }
                            break;

                        case InvokeThreadModel.ReceiverThread:
                            // call method directly in this receiver thread
                            await CallReceivedMethodAsync(session, message);
                            break;

                        case InvokeThreadModel.IndividualTask:
                            // invoke in new task
                            _ = Task.Run(() =>
                            {
                                CallReceivedMethod(session, message);
                            });
                            break;
                    }
                    break;


                case MessageType.MethodInvokeResponse:

                    IInvokeState invokeParam;
                    if (session.PendingRequests.TryGetValue(message.RequestId, out invokeParam))
                    {
                        if (message.Payload != null)
                        {
                            object returnObject;
                            if (invokeParam.OutParameterValues != null)
                            {
                                // process multiple return values
                                // Index 0: method return object
                                // Index 1 - x: out parameter values
                                object[] returnValues = (object[])message.Payload;

                                returnObject = returnValues[0];

                                for (int i = 0; i < invokeParam.OutParameterValues.Length; i++)
                                {
                                    invokeParam.OutParameterValues[i] = returnValues[i + 1];
                                }
                            }
                            else
                            {
                                returnObject = message.Payload;
                            }

                            invokeParam.ReturnObject = returnObject;
                        }

                        invokeParam.WaitHandle.Set();

                        session.PendingRequests.Remove(message.RequestId);
                    }
                    break;


                case MessageType.Exception:

                    IInvokeState invokeExceptionParam;
                    if (session.PendingRequests.TryGetValue(message.RequestId, out invokeExceptionParam))
                    {
                        // throw exception in the caller stack
                        Exception exception = null;
                        IExceptionWrapper exWrapper = null;
                        if (message.Payload is IExceptionWrapper)
                        {
                            exWrapper = (IExceptionWrapper)message.Payload;

                            if (exWrapper.TryDeserializeException(out exception))
                            {
                                // original exception could be deserialized
                                // add remote invoke identifier
                                ExceptionWrapper.AddRemoteInvokeIdentification(exception);
                            }
                        }

                        if (exception == null)
                        {
                            // create generic remote invoke exception with payload text (ex.ToString)
                            if (exWrapper != null)
                            {
                                exception = new NonSerializableRemoteException(invokeExceptionParam, message.Payload.ToString(), exWrapper.Name, exWrapper.TypeName, exWrapper.Message);
                            }
                            else
                            {
                                exception = new NonSerializableRemoteException(invokeExceptionParam, message.Payload.ToString());
                            }
                        }

                        invokeExceptionParam.Exception = exception;
                        invokeExceptionParam.WaitHandle.Set();
                        session.PendingRequests.Remove(message.RequestId);
                    }
                    else
                    {
                        if (session.IsActive)
                        {
                            logger.Error("Remote Exception without a waiting caller: " + message.Payload.ToString());
                        }
                        else
                        {
                            logger.Debug("[Closed Session] Remote Exception without a waiting caller: " + message.Payload.ToString());
                        }
                    }

                    break;
            }
        }

        private void CallReceivedMethod(ISession session, IGenericMessage message)
        {

            try
            {
                if (!session.IsInitialized)
                {
                    WaitForPendingSession(session);
                }

                // call remote invoke request
                object implementationInstance = containerHost.GetInterfaceImplementationInstance(session, message.Target);

                if (implementationInstance == null)
                {
                    throw new Exception("No implementation for the interface \"" + message.Target + "\" found!");
                }
                else if (implementationInstance.GetType().Namespace == TypeService.AutoGeneratedProxiesNamespace)
                {
                    throw new Exception("No implementation for the interface \"" + message.Target + "\" found! The auto generated proxy \"" + implementationInstance.GetType().FullName + "\" is not valid for remote invoke requests.");
                }

                Type serviceType = implementationInstance.GetType();

                // Get method
                IInvokeMethodInfo methodInfo;
                int invokeInfoCacheKey = InvokeMethodInfo.CreateKey(message.Target, message.Name, serviceType);
                if (!methodInfoCache.TryGetValue(invokeInfoCacheKey, out methodInfo))
                {
                    Type interfaceType = serviceType.GetInterface(message.Target);
                    methodInfo = new InvokeMethodInfo(interfaceType, message.Name, null, serviceType);

                    methodInfoCache[invokeInfoCacheKey] = methodInfo;
                }

                // Get method params
                object[] parameters = null;
                if (message.Payload != null)
                {
                    parameters = (object[])message.Payload;

                    // check parameter types and convert is necesarry
                    var parameterInfos = methodInfo.ParameterInfos;
                    if (parameters.Length != parameters.Length)
                    {
                        throw new MethodAccessException("Parameter count mismatch! Method name: " + message.Name + "; Interface: " + message.Target);
                    }
                }

                // Invoke method
                object returnObject = methodInfo.Invoke(implementationInstance, parameters);

                bool responseExpected = message.Type != MessageType.AsyncMethodInvokeRequest;
                if (responseExpected)
                {
                    object responseObject;
                    if (methodInfo.OutParameters != null)
                    {
                        // method contains out parameters
                        // create a list of return values
                        // Index 0: method return object
                        // Index 1 - x: out parameter values
                        object[] responseArray = new object[methodInfo.OutParameters.Length + 1];
                        responseArray[0] = returnObject;

                        for (int i = 0; i < methodInfo.OutParameters.Length; i++)
                        {
                            responseArray[i + 1] = parameters[methodInfo.OutParameters[i].Position];
                        }

                        responseObject = responseArray;
                    }
                    else
                    {
                        responseObject = returnObject;
                    }

                    try
                    {
                        // Send response message
                        GenericMessage responseMessage = new GenericMessage(message.RequestId, responseObject);
                        baseCommunicationServiceSupport.SendMessage(responseMessage, session.SessionId, methodInfo);
                        sentMessageCounter++;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Warn($"Could not answer to request id {message.RequestId} because of session id {session.SessionId} connection loss");
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Debug(string.Format("Error during remote call processing from session ID {0}. The exception will be send to the caller. Details: {1}", session.SessionId, ex.ToString()));

                if (ex is TargetInvocationException)
                {
                    TargetInvocationException targetInvokeEx = (TargetInvocationException)ex;

                    // unwrap reflection (invoke) exception container
                    ex = targetInvokeEx.InnerException;
                }

                try
                {
                    // Send exception message
                    GenericMessage responseMessage = new GenericMessage(message.RequestId, ex);
                    baseCommunicationServiceSupport.SendMessage(responseMessage, session.SessionId, null);
                    sentMessageCounter++;
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"Could not error respond to request id {message.RequestId} because of session id {session.SessionId} connection loss");
                }
                catch (Exception exSendEx)
                {
                    logger.Warn($"Exception during send response exception back to client! Details: {exSendEx}");
                }
            }
        }


        private async ValueTask CallReceivedMethodAsync(ISession session, IGenericMessage message)
        {

            try
            {
                if (!session.IsInitialized)
                {
                    WaitForPendingSession(session);
                }

                // call remote invoke request
                object implementationInstance = containerHost.GetInterfaceImplementationInstance(session, message.Target);

                if (implementationInstance == null)
                {
                    throw new Exception("No implementation for the interface \"" + message.Target + "\" found!");
                }
                else if (implementationInstance.GetType().Namespace == TypeService.AutoGeneratedProxiesNamespace)
                {
                    throw new Exception("No implementation for the interface \"" + message.Target + "\" found! The auto generated proxy \"" + implementationInstance.GetType().FullName + "\" is not valid for remote invoke requests.");
                }

                Type serviceType = implementationInstance.GetType();

                // Get method
                IInvokeMethodInfo methodInfo;
                int invokeInfoCacheKey = InvokeMethodInfo.CreateKey(message.Target, message.Name, serviceType);
                if (!methodInfoCache.TryGetValue(invokeInfoCacheKey, out methodInfo))
                {
                    Type interfaceType = serviceType.GetInterface(message.Target);
                    methodInfo = new InvokeMethodInfo(interfaceType, message.Name, null, serviceType);

                    methodInfoCache[invokeInfoCacheKey] = methodInfo;
                }

                // Get method params
                object[] parameters = null;
                if (message.Payload != null)
                {
                    parameters = (object[])message.Payload;

                    // check parameter types and convert is necesarry
                    var parameterInfos = methodInfo.ParameterInfos;
                    if (parameters.Length != parameters.Length)
                    {
                        throw new MethodAccessException("Parameter count mismatch! Method name: " + message.Name + "; Interface: " + message.Target);
                    }
                }

                // Invoke method
                object returnObject = methodInfo.Invoke(implementationInstance, parameters);

                bool responseExpected = message.Type != MessageType.AsyncMethodInvokeRequest;
                if (responseExpected)
                {
                    if (methodInfo.IsAsyncAwaitRemoteMethod)
                    {

                        if (returnObject is Task task)
                        {
                            await task;

                            returnObject = TypeService.GetAsyncAwaitResultValue(task);
                        }
                        //else if (returnObject is ValueTask valueTask)
                        //{
                        //    await valueTask;
                        //}
                        else
                        {
                            throw new MethodAccessException($"Unexpected async/await method return type! Unsupported type: {returnObject?.GetType().FullName}");
                        }

                        if (methodInfo.IsVoidReturnMethod)
                        {
                            // do not serialize Task
                            returnObject = null;
                        }
                    }

                    object responseObject;
                    if (methodInfo.OutParameters != null)
                    {
                        // method contains out parameters
                        // create a list of return values
                        // Index 0: method return object
                        // Index 1 - x: out parameter values
                        object[] responseArray = new object[methodInfo.OutParameters.Length + 1];
                        responseArray[0] = returnObject;

                        for (int i = 0; i < methodInfo.OutParameters.Length; i++)
                        {
                            responseArray[i + 1] = parameters[methodInfo.OutParameters[i].Position];
                        }

                        responseObject = responseArray;
                    }
                    else
                    {
                        responseObject = returnObject;
                    }

                    try
                    {
                        // Send response message
                        GenericMessage responseMessage = new GenericMessage(message.RequestId, responseObject);
                        await baseCommunicationServiceSupport.SendMessageAsync(responseMessage, session.SessionId, methodInfo);
                        sentMessageCounter++;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Warn($"Could not answer to request id {message.RequestId} because of session id {session.SessionId} connection loss");
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Debug(string.Format("Error during remote call processing from session ID {0}. The exception will be send to the caller. Details: {1}", session.SessionId, ex.ToString()));

                if (ex is TargetInvocationException)
                {
                    TargetInvocationException targetInvokeEx = (TargetInvocationException)ex;

                    // unwrap reflection (invoke) exception container
                    ex = targetInvokeEx.InnerException;
                }

                try
                {
                    // Send exception message
                    GenericMessage responseMessage = new GenericMessage(message.RequestId, ex);
                    baseCommunicationServiceSupport.SendMessage(responseMessage, session.SessionId, null);
                    sentMessageCounter++;
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"Could not error respond to request id {message.RequestId} because of session id {session.SessionId} connection loss");
                }
                catch (Exception exSendEx)
                {
                    logger.Warn($"Exception during send response exception back to client! Details: {exSendEx}");
                }
            }
        }

        /// <summary>
        /// Preserves the exception stack trace on rethrow.
        /// </summary>
        /// <param name="ex">The ex.</param>
        private void PreserveStackTrace(Exception ex)
        {
            try
            {
                if (ex is OperationCanceledException)
                {
                    // do not preseve stack on remote connection lost exception
                    return;
                }

                var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
                var mgr = new ObjectManager(null, ctx);
                var si = new SerializationInfo(ex.GetType(), new FormatterConverter());

                ex.GetObjectData(si, ctx);
                mgr.RegisterObject(ex, 1, si); // prepare for SetObjectData
                mgr.DoFixups(); // ObjectManager calls SetObjectData
            }
            catch (Exception preserveEx)
            {
                logger.Warn("Can't preserve exception stack trace: " + preserveEx.ToString() + " \n\n Original Exception: " + ex.ToString());
            }
        }

        private async ValueTask ReceiverProcessTask()
        {
            try
            {
                //logger.Info("Remote receiver processing started");

                var reader = receiverQueue.Reader;

                // todo: change to awat foreach and ReadAllAsync in future .net versions
                while (await reader.WaitToReadAsync())
                {
                    if (reader.TryRead(out var invokeItem))
                    {
                        await ProcessReceivedMessageBytes(invokeItem.Item1, invokeItem.Item2);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Unexpected error! Receiver processing will exit! Details: " + ex.ToString());
            }
            finally
            {
                logger.Info("Receiver processing stopped");
            }
        }

        private async ValueTask CallerThreadProcess()
        {
            try
            {
                if (containerHost is ITalkContainer container)
                    logger.Info($"Remote caller processing for \"{container.Name}\" started");
                else
                    logger.Info("Remote caller processing started");

                var reader = callerQueue.Reader;

                // todo: change to awat foreach and ReadAllAsync in future .net versions
                while (await reader.WaitToReadAsync())
                {
                    if (reader.TryRead(out var invokeItem))
                    {
                        await CallReceivedMethodAsync(invokeItem.Item1, invokeItem.Item2);
                    }
                }
            }
            catch (Exception ex)
            {
                if (containerHost is ITalkContainer container)
                    logger.Error($"Unexpected error! Caller processing \"{container.Name}\" will exit! Details: {ex}");
                else
                    logger.Error("Unexpected error! Caller processing will exit! Details: " + ex.ToString());
            }
            finally
            {
                if (containerHost is ITalkContainer container)
                    logger.Info($"Caller processing for \"{container.Name}\" stopped");
                else
                    logger.Info("Caller processing stopped");
            }
        }


        /// <summary>
        /// Waits for response and process other calls.
        /// This method can only by invoked by the caller thread (InvokeThreadModel = CallerThread).
        /// </summary>
        /// <param name="invokeState">State of the invoke.</param>
        private void WaitForResponseAndProcessOtherCalls(InvokeState invokeState, TimeSpan timeout)
        {
            // process other invoke requests in meantime
            bool waitForResponse = true;
            bool otherCallsProcessed = false;
            DateTime waitStartUtcTime = DateTime.UtcNow;
            do
            {
                if (otherCallsProcessed)
                {
                    // only wait one milisecond max if other invokes are pending
                    if (invokeState.WaitHandle.Wait(1))
                    {
                        waitForResponse = false;
                        break;
                    }
                }
                else
                {
                    if (invokeState.WaitHandle.Wait(10))
                    {
                        waitForResponse = false;
                        break;
                    }
                }

                if (waitForResponse
                    && DateTime.UtcNow.Subtract(waitStartUtcTime) > timeout)
                {
                    throw new TimeoutException(string.Format("Request timeout occured! Request: {0}; timeout time: {1}; session ID: {2}", invokeState.Method.Name, RequestTimeout, invokeState.Session.SessionId, invokeState.RequestMessage.RequestId));
                }

                if (!isActive)
                {
                    // process shutdown pending
                    break;
                }

                // check if other invoke requests are received during waiting phase
                otherCallsProcessed = ProcessPendingCallerThreadInvokesOnce();

            } while (waitForResponse);
        }

        /// <summary>
        /// Processes the pending caller thread invokes once.
        /// This method can only by invoked by the caller thread waiting for responses!
        /// </summary>
        private bool ProcessPendingCallerThreadInvokesOnce()
        {
            Tuple<ISession, IGenericMessage> invokeItem;
            if (callerQueue.Reader.TryRead(out invokeItem))
            {
                CallReceivedMethod(invokeItem.Item1, invokeItem.Item2);
                return true;
            }

            return false;
        }



        private async void ActivateSendHeartbeats(ISession session)
        {
            try
            {
                logger.Debug($"Activate send hearbeats - session ID: {session.SessionId} - Interv.: {HeartbeatIntervalTimeSeconds} sec. ");

                GenericMessage heartbeatMsg = new GenericMessage();
                heartbeatMsg.Type = MessageType.Heartbeat;

                // initial wait
                await Task.Delay(HeartbeatIntervalTime);

                lastReceivedMessageCounter = receivedMessageCounter;
                lastSentMessageCounter = sentMessageCounter;

                while (session.IsActive)
                {
                    if (receivedMessageCounter == lastReceivedMessageCounter
                        && sentMessageCounter == lastSentMessageCounter)
                    {
                        heartbeatMsg.RequestId = Interlocked.Increment(ref currentRequestId);

                        baseCommunicationServiceSupport.SendMessage(heartbeatMsg, session.SessionId, null);
                    }
                    else
                    {
                        lastReceivedMessageCounter = receivedMessageCounter;
                        lastSentMessageCounter = sentMessageCounter;
                    }

                    await Task.Delay(HeartbeatIntervalTime);
                }
            }
            catch (OperationCanceledException)
            {
                ProcessSessionTerminated(session.SessionId, "Heartbeat fail");
            }
            catch (Exception ex)
            {
                logger.Warn($"Unexpected exception during heartbeat send! Details: {ex}");
            }
            finally
            {
                if (session.IsActive)
                    logger.Error($"Send heartbeats unexpected stopped though session (ID: {session.SessionId}) is still marked as active");

                logger.Debug($"Send heartbeats for {session.SessionId} stopped");
            }
        }

        private static Task WaitHandleTaskAsyncResponseHelper(WaitHandle handle, TimeSpan timeout, IInvokeState invokeState, int sessionId, long requestId)
        {
            var tcs = new TaskCompletionSource<object>();
            var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
            {
                var localTcs = (TaskCompletionSource<object>)state;
                if (timedOut)
                {
                    localTcs.SetException(new TimeoutException($"Request timeout occured! Request: {invokeState.Method.Name}; timeout time: {timeout}; session ID: {sessionId}; request ID: {requestId}"));
                }
                else
                {
                    localTcs.SetResult(null);
                }
            }, tcs, timeout, true);

            // clean up the RegisterWaitHandle
            tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            return tcs.Task;
        }


        /// <summary>
        /// Gets the new connection session id.
        /// </summary>
        /// <returns></returns>
        public static int GetNewConnectionSessionId()
        {
            return Interlocked.Increment(ref lastConnectionSessionId);
        }

        // ----------------------------------------------------------------------------------------
        #endregion




    }
}
