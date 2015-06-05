using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using BSAG.IOCTalk.Common.Session;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Exceptions;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Interface.Logging;
using System.Runtime.Serialization;

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

        private const string DismissInvalidSessionMessageLogTag = "[Dismiss invalid session message]";

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
        protected Dictionary<string, IInvokeMethodInfo> methodInfoCache = new Dictionary<string, IInvokeMethodInfo>();

        /// <summary>
        /// Custom response wait handler
        /// </summary>
        protected ICustomResponseWaitHandler customResponseWaitHandler = null;

        /// <summary>
        /// Request timeout
        /// Default: 5 min
        /// </summary>
        protected TimeSpan requestTimeout = new TimeSpan(0, 5, 0);

        /// <summary>
        /// Technical logger
        /// </summary>
        protected ILogger logger;
        private string loggerTypeName = "BSAG.IOCTalk.Logging.TraceLogger, BSAG.IOCTalk.Logging";

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
        private CreateSessionHandler customCreateSessionHandler = null;
        private Dictionary<int, ISession> sessionDictionary = new Dictionary<int, ISession>();
        private object sessionLockSyncObj = new object();
        private SpinWait spinWait = new SpinWait();
        private long pendingSessionCreationCount = 0;
        private string serializerTypeName = "BSAG.IOCTalk.Serialization.Json.JsonMessageSerializer, BSAG.IOCTalk.Serialization.Json";

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
        /// Gets or sets the name of the data stream logger type.
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
        public IDataStreamLogger DataStreamLogger
        {
            get { return dataStreamLogger; }
        }

        /// <summary>
        /// Gets the sessions.
        /// </summary>
        public ISession[] ClientSessions
        {
            get { return sessions; }
        }

        /// <summary>
        /// Gets or sets the custom create session handler.
        /// </summary>
        /// <value>
        /// The custom create session handler.
        /// </value>
        public CreateSessionHandler CustomCreateSessionHandler
        {
            get
            {
                return customCreateSessionHandler;
            }
            set
            {
                customCreateSessionHandler = value;
            }
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
        }
        
        /// <summary>
        /// Communication service shutdown
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// Creates the session.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="description">The description.</param>
        public void CreateSession(int sessionId, string description)
        {
            Interlocked.Increment(ref pendingSessionCreationCount);
            ISession newSession = null;
            object sessionContractObject = null;
            try
            {

                if (customCreateSessionHandler != null)
                {
                    // create custom session object
                    newSession = customCreateSessionHandler(this, sessionId, description);
                }
                else
                {
                    // create default session
                    newSession = new Session(this, sessionId, description);
                }

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
                        sessionContractObject = containerHost.CreateSessionContractInstance(newSession);
                    }

                    if (dataStreamLogger != null)
                    {
                        dataStreamLogger.OnSessionCreated(newSession);
                    }

                    logger.Info(string.Format("Session created - {0} - {1}", newSession.SessionId, newSession.Description));

                    // call session created event
                    if (SessionCreated != null)
                    {
                        // start event in a new thread to avoid EndHandle blocking
                        ThreadPool.QueueUserWorkItem(OnSessionCreatedInternal, new SessionEventArgs(newSession, sessionContractObject));

                        // provoke thread context switch
                        Thread.Sleep(0);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref pendingSessionCreationCount);
            }
        }

        private void OnSessionCreatedInternal(object newSessionObj)
        {
            SessionEventArgs newSessionArgs = (SessionEventArgs)newSessionObj;
            if (SessionCreated != null)
            {
                SessionCreated(this, newSessionArgs);
            }
        }


        /// <summary>
        /// Processes the session terminated.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <returns>Returns <c>true</c> if the session was found and terminated; otherwise <c>false</c>.</returns>
        public bool ProcessSessionTerminated(int sessionId)
        {
            lock (sessionLockSyncObj)
            {
                ISession session = null;
                if (sessionDictionary.TryGetValue(sessionId, out session))
                {
                    sessionDictionary.Remove(session.SessionId);
                    sessions = sessionDictionary.Values.ToArray<ISession>();

                    session.Close();

                    logger.Info(string.Format("Session terminated - {0} - {1}", session.SessionId, session.Description));

                    if (dataStreamLogger != null)
                    {
                        dataStreamLogger.OnSessionTerminated(session);
                    }

                    if (SessionTerminated != null)
                    {
                        SessionTerminated(this, new SessionEventArgs(session));
                    }
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
        public void RegisterContainerHost(IGenericContainerHost containerHost)
        {
            if (this.containerHost != null)
            {
                throw new Exception("A container host is already registered.");
            }

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

                this.serializer = (IGenericMessageSerializer)Activator.CreateInstance(serializerType);
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

                this.logger = (ILogger)Activator.CreateInstance(loggerType);
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

                this.dataStreamLogger = (IDataStreamLogger)Activator.CreateInstance(dataStreamLoggerType);
            }

            this.serializer.RegisterContainerHost(containerHost);
        }


        /// <summary>
        /// Invokes a remote interface method by a given lambda method expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public object InvokeMethod<T>(object source, Expression<Action<T>> expression)
        {
            MethodCallExpression methodCall = expression.Body as MethodCallExpression;
            if (methodCall == null)
            {
                throw new Exception("Lambda expression must be a method call!");
            }
            object[] parameters = null;
            List<ParameterInfo> outParameters = null;
            List<FieldInfo> outParameterFieldInfos = null;
            List<object> outParameterTargetValues = null;
            if (methodCall.Arguments.Count > 0)
            {
                parameters = new object[methodCall.Arguments.Count];
                ParameterInfo[] parameterInfos = methodCall.Method.GetParameters();
                for (int i = 0; i < methodCall.Arguments.Count; i++)
                {
                    object argument = methodCall.Arguments[i];

                    // get value from lambda expression
                    MemberExpression memberExpression = argument as MemberExpression;
                    FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
                    ConstantExpression constantExpression = (memberExpression.Expression as ConstantExpression);
                    parameters[i] = fieldInfo.GetValue(constantExpression.Value);

                    // check out parameter
                    var parameterInfo = parameterInfos[i];
                    if (parameterInfo.IsOut)
                    {
                        if (outParameterFieldInfos == null)
                        {
                            outParameters = new List<ParameterInfo>();
                            outParameterFieldInfos = new List<FieldInfo>();
                            outParameterTargetValues = new List<object>();
                        }

                        outParameters.Add(parameterInfo);
                        outParameterFieldInfos.Add(fieldInfo);
                        outParameterTargetValues.Add(constantExpression.Value);
                    }
                }
            }

            try
            {
                // todo: directly call InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters)
                return InvokeMethod(source, methodCall.Method, parameters);
            }
            finally
            {
                if (outParameters != null)
                {
                    for (int i = 0; i < outParameters.Count; i++)
                    {
                        object outValue = parameters[outParameters[i].Position];
                        outParameterFieldInfos[i].SetValue(outParameterTargetValues[i], outValue);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes a remote interface method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public virtual object InvokeMethod(object source, MethodInfo method, object[] parameters)
        {
            return InvokeMethod(source, new InvokeMethodInfo(method), parameters);
        }



        /// <summary>
        /// Invokes a remote interface method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="invokeInfo">The invoke info (cached reflection infos).</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public virtual object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters)
        {
            long requestId = Interlocked.Increment(ref currentRequestId);


            ISession session = containerHost.GetSessionByServiceInstance(source);

            if (session == null || !session.IsActive)
            {
                throw new RemoteConnectionLostException(null);
            }

            bool responseExpected = true;
            if (invokeInfo.IsAsyncRemoteInvoke)
            {
                if (baseCommunicationServiceSupport.IsAsyncSendCurrentlyPossible(session))
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

                invokeState.WaitHandle = new ManualResetEvent(false);
                invokeState.Method = invokeInfo.InterfaceMethod;
                invokeState.MethodSource = invokeInfo;

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
                        throw new RemoteConnectionLostException(null);
                    }
                    else
                    {
                        throw;
                    }
                }
            }


            baseCommunicationServiceSupport.SendMessage(requestMsg, session.SessionId, invokeInfo);

            if (responseExpected)
            {
                // block thread and wait for return object response
                if (customResponseWaitHandler != null)
                {
                    customResponseWaitHandler.WaitForResponse(invokeState);
                }
                else
                {
                    if (!invokeState.WaitHandle.WaitOne(requestTimeout))
                    {
                        throw new TimeoutException(string.Format("Request timeout occured! Request: {0}; timeout time: {1}", invokeState.Method.Name, requestTimeout));
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

        /// <summary>
        /// Processes the received message string.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="messageString">The message string.</param>
        public void ProcessReceivedMessageString(int sessionId, string messageString)
        {
            WaitForPendingSessions();

            ISession session;
            if (!sessionDictionary.TryGetValue(sessionId, out session))
            {
                // session terminated -> ignore packets
                if (logDataStream)
                {
                    dataStreamLogger.LogStreamMessage(sessionId, true, DismissInvalidSessionMessageLogTag + messageString);
                }
                return;
            }

            if (logDataStream)
            {
                dataStreamLogger.LogStreamMessage(sessionId, true, messageString);
            }

            IGenericMessage message = serializer.DeserializeFromString(messageString, session);

            ProcessReceivedMessage(session, message);
        }

        public void ProcessReceivedMessageBytes(int sessionId, byte[] messageBytes)
        {
            WaitForPendingSessions();

            ISession session;
            if (!sessionDictionary.TryGetValue(sessionId, out session))
            {
                // session terminated -> ignore packets
                if (logDataStream)
                {
                    dataStreamLogger.LogStreamMessage(sessionId, true, DismissInvalidSessionMessageLogTag + Encoding.UTF8.GetString(messageBytes));
                }
                return;
            }

            if (logDataStream)
            {
                dataStreamLogger.LogStreamMessage(sessionId, true, messageBytes);
            }

            IGenericMessage message = serializer.DeserializeFromBytes(messageBytes, session);

            ProcessReceivedMessage(session, message);
        }

        private void WaitForPendingSessions()
        {
            if (Interlocked.Read(ref pendingSessionCreationCount) > 0)
            {
                // wait until session context is fully initialized (container composition...)
                while (Interlocked.Read(ref pendingSessionCreationCount) > 0)
                {
                    spinWait.SpinOnce();
                }
                spinWait.Reset();
            }
        }

        /// <summary>
        /// Processes the received message.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="message">The message.</param>
        public void ProcessReceivedMessage(ISession session, IGenericMessage message)
        {
            switch (message.Type)
            {
                case MessageType.MethodInvokeRequest:
                case MessageType.AsyncMethodInvokeRequest:

                    try
                    {
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
                        string invokeInfoCacheKey = InvokeMethodInfo.CreateKey(message.Target, message.Name, serviceType);
                        if (!methodInfoCache.TryGetValue(invokeInfoCacheKey, out methodInfo))
                        {
                            Type interfaceType = serviceType.GetInterface(message.Target);
                            methodInfo = new InvokeMethodInfo(interfaceType, message.Name, null, serviceType);
                            methodInfoCache.Add(invokeInfoCacheKey, methodInfo);
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
                        object returnObject = methodInfo.InterfaceMethod.Invoke(implementationInstance, parameters);

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

                            // Send response message
                            GenericMessage responseMessage = new GenericMessage(message.RequestId, responseObject);
                            baseCommunicationServiceSupport.SendMessage(responseMessage, session.SessionId, methodInfo);
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

                        // Send exception message
                        GenericMessage responseMessage = new GenericMessage(message.RequestId, ex);
                        baseCommunicationServiceSupport.SendMessage(responseMessage, session.SessionId, null);
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

        /// <summary>
        /// Preserves the exception stack trace on rethrow.
        /// </summary>
        /// <param name="ex">The ex.</param>
        private void PreserveStackTrace(Exception ex)
        {
            try
            {
                var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
                var mgr = new ObjectManager(null, ctx);
                var si = new SerializationInfo(ex.GetType(), new FormatterConverter());

                ex.GetObjectData(si, ctx);
                mgr.RegisterObject(ex, 1, si); // prepare for SetObjectData
                mgr.DoFixups(); // ObjectManager calls SetObjectData
            }
            catch (Exception preserveEx)
            {
                logger.Error("Preserve exception stack trace error: " + preserveEx.ToString());
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

    }
}
