using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Xml.Linq;
using BSAG.IOCTalk.Common.Reflection;
using System.Globalization;
using BSAG.IOCTalk.Communication.Tcp.Utils;
using System.Threading;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Config;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Communication.Tcp.Security;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Communication.Tcp.Config;

namespace BSAG.IOCTalk.Communication.Tcp
{
    /// <summary>
    /// Provides client/service tcp communication
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-06-30
    /// </remarks>
    public class TcpCommunicationController : GenericCommunicationBaseService, ICommunicationBaseServiceSupport, IXmlConfig
    {
        #region TcpCommunicationController fields
        // ----------------------------------------------------------------------------------------
        // TcpCommunicationController fields
        // ----------------------------------------------------------------------------------------

        private AbstractTcpCom communication;
        private int clientAutoReconnectLock = 0;
        private ConnectionType connectionType;
        private int clientConnectCount = 0;

        public const string ConfigParamConnectionType = "ConnectionType";
        public const string ConfigParamHost = "Host";
        public const string ConfigParamPort = "Port";


        public const string ConfigElementSecurity = "Security";
        public const string ConfigParamSecProtocol = "Protocol";
        public const string ConfigParamServerName = "ServerName";
        public const string ConfigParamCertificateName = "CertificateName";
        public const string ConfigParamClientCertificateRequired = "ClientCertificateRequired";
        public const string ConfigParamProvideClientCertificate = "ProvideClientCertificate";
        public const string ConfigParamClientCertificateName = "ClientCertificateName";
        public const string ConfigParamLogDataStream = "LogDataStream";

        public const string ConfigAttributeEnabled = "enabled";

        private TcpConfiguration configObject;
        private TcpTarget originalTarget;


        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpCommunicationController constructors
        // ----------------------------------------------------------------------------------------
        // TcpCommunicationController constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>TcpCommunicationController</c> class.
        /// </summary>
        public TcpCommunicationController()
        {
        }


        /// <summary>
        /// Creates a new instance of the <c>TcpCommunicationController</c> class.
        /// </summary>
        public TcpCommunicationController(ILogger logger)
        {
            this.logger = logger;
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpCommunicationController properties
        // ----------------------------------------------------------------------------------------
        // TcpCommunicationController properties
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Gets or sets the config as XML.
        /// </summary>
        /// <value>
        /// The config.
        /// </value>
        public XDocument Config { get; set; }

        /// <summary>
        /// Gets or sets the configuration as object.
        /// </summary>
        public TcpConfiguration ConfigObject
        {
            get { return configObject; }
            set
            {
                configObject = value;

                // direct updates before init
                LogDataStream = configObject.LogDataStream;
            }
        }


        /// <summary>
        /// Gets the underlying communication provider.
        /// </summary>
        public AbstractTcpCom Communication
        {
            get
            {
                return communication;
            }
        }

        /// <summary>
        /// Gets or sets the client reconnect interval
        /// </summary>
        public TimeSpan ClientReconnectInterval { get; set; } = TimeSpan.FromSeconds(1);

        public event EventHandler ClientReconnectFailed;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpCommunicationController methods
        // ----------------------------------------------------------------------------------------
        // TcpCommunicationController methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Inits this instance.
        /// </summary>
        public override void Init()
        {
            if (ConfigObject != null)
            {
                //todo: implement TLS

                this.LogDataStream = ConfigObject.LogDataStream;

                if (ConfigObject.Port <= 0)
                    throw new InvalidOperationException($"Invalid tcp port number: {ConfigObject.Port} in ConfigObject!");

                switch (ConfigObject.Type)
                {
                    case Tcp.Config.ConnectionType.Client:
                        if (string.IsNullOrWhiteSpace(ConfigObject.Host))
                            throw new InvalidOperationException($"Invalid tcp hostname: \"{ConfigObject.Host}\" in ConfigObject!");

                        InitClient(ConfigObject.Host, ConfigObject.Port);
                        break;

                    case Tcp.Config.ConnectionType.Service:
                        InitService(ConfigObject.Port);
                        break;

                    default:
                        throw new NotSupportedException($"Configuration TCP type {ConfigObject.Type} not suppprted!");
                }
            }
            else
            {
                if (Config == null)
                {
                    throw new NullReferenceException("TCP Config XML is not defined!");
                }

                if (communication != null)
                {
                    throw new InvalidOperationException(GetType().Name + " already initialized!");
                }

                connectionType = Config.Root.GetConfigParameterValue<ConnectionType>(ConfigParamConnectionType);
                var securityXml = Config.Root.Element(ConfigElementSecurity);
                bool isSecurityEnabled = false;
                if (securityXml != null)
                {
                    var enabledAttr = securityXml.Attribute(ConfigAttributeEnabled);
                    if (enabledAttr != null)
                    {
                        isSecurityEnabled = bool.Parse(enabledAttr.Value);
                    }
                }

                bool? logDataStream = Config.Root.GetConfigParameterValueOrDefault<bool?>(false, ConfigParamLogDataStream);
                if (logDataStream.HasValue && logDataStream.Value)
                    this.LogDataStream = logDataStream.Value;

                if (connectionType == ConnectionType.Client)
                {
                    string host = Config.Root.GetConfigParameterValue<string>(ConfigParamHost);
                    int port = Config.Root.GetConfigParameterValue<int>(ConfigParamPort);

                    InitClient(securityXml, isSecurityEnabled, host, port);
                }
                else if (connectionType == ConnectionType.Service)
                {
                    int servicePort = Config.Root.GetConfigParameterValue<int>(ConfigParamPort);

                    InitService(securityXml, isSecurityEnabled, servicePort);
                }
                else
                {
                    throw new InvalidOperationException("TCP ConnectionType is undefined!");
                }
            }
        }

        public void InitClient(string host, int port)
        {

            InitClient(null, false, host, port);
        }

        public void InitClient(string host, int port, SecureTcpClient secureTcpClient)
        {
            InitClientInternal(host, port, secureTcpClient);
        }

        public void InitService(int port)
        {
            InitService(null, false, port);
        }

        public void InitService(int port, SecureTcpServer secureTcpServer)
        {
            InitServiceInternal(port, secureTcpServer);
        }

        private void InitClient(XElement securityXml, bool isSecurityEnabled, string host, int port)
        {
            TcpClientCom client;
            if (isSecurityEnabled)
            {
                var secureClient = new SecureTcpClient();
                secureClient.ServerName = securityXml.GetConfigParameterValue<string>(ConfigParamServerName);
                secureClient.ProvideClientCertificate = securityXml.GetConfigParameterValueOrDefault<bool>(false, ConfigParamProvideClientCertificate);
                if (secureClient.ProvideClientCertificate)
                {
                    secureClient.ClientCertificateName = securityXml.GetConfigParameterValue<string>(ConfigParamClientCertificateName);
                }

                client = secureClient;
            }
            else
            {
                client = new TcpClientCom();
            }

            InitClientInternal(host, port, client);
        }

        private void InitClientInternal(string host, int port, TcpClientCom client)
        {
            this.connectionType = ConnectionType.Client;
            client.Logger = this.logger;
            SubscribeCommunicationEvents(client);

            originalTarget = new TcpTarget() { Host = host, Port = port };

            client.Init(host, port);

            this.communication = client;

            if (LogDataStream)
            {
                dataStreamLogger.Init(this, host.Replace('.', '_') + "-" + port, Config != null ? Config.Root : null);
            }

            // async client connect
            StartAutoReconnectAsync();
        }

        private void InitService(XElement securityXml, bool isSecurityEnabled, int servicePort)
        {

            TcpServiceCom service;
            if (isSecurityEnabled)
            {
                var secureService = new SecureTcpServer();
                secureService.CertificateName = securityXml.GetConfigParameterValue<string>(ConfigParamCertificateName);
                secureService.ClientCertificateRequired = securityXml.GetConfigParameterValueOrDefault<bool>(false, ConfigParamClientCertificateRequired);
                service = secureService;
            }
            else
            {
                service = new TcpServiceCom();
            }
            InitServiceInternal(servicePort, service);
        }

        private void InitServiceInternal(int servicePort, TcpServiceCom service)
        {
            this.connectionType = ConnectionType.Service;
            service.Logger = this.logger;


            SubscribeCommunicationEvents(service);

            service.Init(servicePort);
            this.communication = service;

            if (LogDataStream)
            {
                dataStreamLogger.Init(this, "Service-" + servicePort, Config != null ? Config.Root : null);
            }

            string errMsg;
            if (this.communication.Connect(out errMsg))
            {
                logger.Info(string.Format("Tcp service established on port {0}", servicePort));
            }
            else
            {
                throw new InvalidOperationException(errMsg);
            }
        }

        /// <summary>
        /// Communication service shutdown
        /// </summary>
        public override void Shutdown()
        {
            this.communication?.Close();

            base.Shutdown();
        }


        private void StartAutoReconnectAsync()
        {
            if (Interlocked.Exchange(ref clientAutoReconnectLock, 1) == 0)    // only start auto reconnect task once
            {
                Task taskClientReconnect = new Task(new Action(async () =>
                {
                    try
                    {
                        await Task.Delay(100);

                        if (!isActive)
                        {
                            if (Logger != null)
                                Logger.Info($"No reconnect to {communication?.EndPoint} because of shutdown");

                            return;
                        }


                        if (Logger != null)
                            Logger.Info($"Connect to {communication?.EndPointInfo}...");

                        clientConnectCount++;

                        string errMsg;
                        while (!this.communication.Connect(out errMsg))
                        {
                            if (Logger != null)
                                Logger.Warn($"Connection refused {communication?.EndPoint}! {errMsg}");

                            await Task.Delay(ClientReconnectInterval);

                            clientConnectCount++;

                            RotateFallbackClientTargets();
                        }

                        clientConnectCount = 0;
                    }
                    catch (Exception ex)
                    {
                        if (Logger != null)
                            Logger.Error(ex.ToString());

                        await Task.Delay(1000);    // pause between reconnect
                    }
                    finally
                    {
                        Interlocked.Exchange(ref clientAutoReconnectLock, 0);

                        if (communication is TcpClientCom)
                        {
                            TcpClientCom client = (TcpClientCom)communication;

                            if (!client.IsConnected && isActive)
                            {
                                if (ClientReconnectFailed != null)
                                    ClientReconnectFailed(this, EventArgs.Empty);

                                if (logger != null)
                                    logger.Debug("Restart reconnect processing");

                                StartAutoReconnectAsync();
                            }
                        }
                        else
                        {
                            if (logger != null)
                                logger.Debug("Reconnect processing exit");
                        }
                    }
                }));
                taskClientReconnect.Start();
            }
        }

        private void RotateFallbackClientTargets()
        {
            if (clientConnectCount > 0
                    && this.configObject != null
                    && this.configObject.ClientFallbackTargets != null
                    && this.configObject.ClientFallbackTargets.Count > 0
                    && communication is TcpClientCom client)
            {
                int targetFallbackIndex = clientConnectCount % configObject.ClientFallbackTargets.Count + 1;

                TcpTarget nextTcpTarget = null;
                if (targetFallbackIndex == 0 && originalTarget != null)
                {
                    nextTcpTarget = originalTarget;
                }
                else
                {
                    int targetListIndex = targetFallbackIndex - 1;
                    nextTcpTarget = configObject.ClientFallbackTargets[targetListIndex];
                }

                if (nextTcpTarget != null)
                {
                    try
                    {
                        client.SetEndPoint(nextTcpTarget.Host, nextTcpTarget.Port);

                        if (Logger != null)
                            Logger.Info($"Rotate client fallback endpoint to \"{nextTcpTarget.Host}:{nextTcpTarget.Port}\"");
                    }
                    catch (Exception setEndpointEx)
                    {
                        if (Logger != null)
                            Logger.Error($"Could not set fallback endpoint! Host: {nextTcpTarget.Host}; Port: {nextTcpTarget.Port}; Exception: {setEndpointEx}");
                    }
                }
            }
        }

        private void SubscribeCommunicationEvents(AbstractTcpCom tcpComm)
        {
            tcpComm.ConnectionEstablished += new EventHandler<ConnectionStateChangedEventArgs>(OnTcpComm_ConnectionEstablished);
            tcpComm.ConnectionClosed += new EventHandler<ConnectionStateChangedEventArgs>(OnTcpComm_ConnectionClosed);

            tcpComm.RawMessageReceivedDelegate = OnRawMessageReceived;
        }

        private void OnTcpComm_ConnectionEstablished(object sender, ConnectionStateChangedEventArgs e)
        {
            try
            {
                this.CreateSession(e.Client.SessionId, e.Client.SessionInfo);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }

        private void OnTcpComm_ConnectionClosed(object sender, ConnectionStateChangedEventArgs e)
        {
            try
            {
                this.ProcessSessionTerminated(e.Client.SessionId);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            if (connectionType == ConnectionType.Client)
            {
                StartAutoReconnectAsync();
            }
        }

        /// <summary>
        /// Sends the generic message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="receiverSessionId">The receiver session id.</param>
        /// <param name="context">The context.</param>
        public void SendMessage(IGenericMessage message, int receiverSessionId, object context)
        {
            byte[] msgBytes = serializer.SerializeToBytes(message, context);
            byte[] encapsulatedMessageBytes = AbstractTcpCom.CreateMessage(serializer.MessageFormat, msgBytes);

            if (logDataStream)
            {
                dataStreamLogger.LogStreamMessage(receiverSessionId, false, msgBytes, serializer.MessageFormat != RawMessageFormat.JSON);
            }

            communication.Send(encapsulatedMessageBytes, receiverSessionId);
        }


        /// <summary>
        /// Called when [raw message received].
        /// </summary>
        /// <param name="rawMessage">The raw message.</param>
        public void OnRawMessageReceived(IRawMessage rawMessage)
        {
            this.ProcessReceivedMessageBytes(rawMessage.SessionId, rawMessage.Data);
        }


        /// <summary>
        /// Determines whether [is async send currently possible] depending on the current load situation.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>
        ///   <c>true</c> if [is async send currently possible] [the specified session]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAsyncVoidSendCurrentlyPossible(ISession session)
        {
            return !communication.IsSendBufferUnderPressure(session.SessionId);
        }



        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
