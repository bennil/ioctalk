﻿using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Config;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Communication.Tcp.Config;
using BSAG.IOCTalk.Communication.Tcp.Security;
using BSAG.IOCTalk.Communication.Tcp.Utils;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        protected AbstractTcpCom communication;
        private int clientAutoReconnectLock = 0;
        protected ConnectionType connectionType;
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
        protected TcpTarget originalTarget;


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



        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        /// <value>
        /// The initial size of the receive buffer.
        /// </value>
        public int ReceiveBufferSize { get; set; } = 65536;


        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        /// <value>
        /// The size of the send buffer.
        /// </value>
        public int SendBufferSize { get; set; } = 65536;


        public Action<Socket> AdjustSocketHandler { get; set; }


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
            client.ReceiveBufferSize = this.ReceiveBufferSize;
            client.SendBufferSize = this.SendBufferSize;
            client.AdjustSocketHandler = this.AdjustSocketHandler;
            SubscribeCommunicationEvents(client);

            originalTarget = new TcpTarget() { Host = host, Port = port };

            client.Init(host, port);

            this.communication = client;

            if (LogDataStream)
            {
                string containerName = string.Empty;
                if (containerHost is ITalkContainer cont
                    && cont.Name != null)
                {
                    containerName = cont.Name + "_";
                }

                dataStreamLogger.Init(this, $"{containerName}{host.Replace('.', '_')}-{port}", Config != null ? Config.Root : null);
            }

            // async client connect
            Task.Run(StartAutoReconnectAsync);
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
            service.ReceiveBufferSize = this.ReceiveBufferSize;
            service.SendBufferSize = this.SendBufferSize;
            service.AdjustSocketHandler = this.AdjustSocketHandler;


            SubscribeCommunicationEvents(service);

            service.Init(servicePort);
            this.communication = service;

            if (LogDataStream)
            {
                string containerName = string.Empty;
                if (containerHost is ITalkContainer cont
                    && cont.Name != null)
                {
                    containerName = cont.Name + "_";
                }
                dataStreamLogger.Init(this, $"{containerName}Service-{servicePort}", Config != null ? Config.Root : null);
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


        protected async void StartAutoReconnectAsync()
        {
            if (Interlocked.Exchange(ref clientAutoReconnectLock, 1) == 0)    // only start auto reconnect task once
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

                    if (this.communication.ConnectTimeUtc.HasValue)
                    {
                        // only if established connection is immediately reset by the remote host
                        // sleep to prevent fast endless reconnect loop
                        var lastConnectDiff = DateTime.UtcNow - this.communication.ConnectTimeUtc.Value;

                        if (lastConnectDiff < ClientReconnectInterval)
                        {
                            await Task.Delay(ClientReconnectInterval);
                        }
                    }

                    if (Logger != null)
                        Logger.Info($"Connect to {communication?.EndPointInfo}...");

                    clientConnectCount++;

                    string errMsg;
                    while (!this.communication.Connect(out errMsg))
                    {
                        if (Logger != null)
                            Logger.Warn($"Connection refused {communication?.EndPointInfo}! {errMsg}");

                        if (ClientReconnectFailed != null)
                            ClientReconnectFailed(this, EventArgs.Empty);

                        if (isActive == false)
                            return;

                        await Task.Delay(ClientReconnectInterval);

                        clientConnectCount++;

                        RotateFallbackClientTargets();
                    }

                    clientConnectCount = 0;
                }
                catch (Exception ex)
                {
                    if (Logger != null)
                        Logger.Error($"Unexpected tcp auto reconnect error: {ex}");

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

                            _ = Task.Run(StartAutoReconnectAsync);
                        }
                    }
                    else
                    {
                        if (logger != null)
                            logger.Debug("Reconnect processing exit");
                    }
                }
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
                int targetFallbackIndex = clientConnectCount % (configObject.ClientFallbackTargets.Count + 1);

                TcpTarget nextTcpTarget = null;
                if (targetFallbackIndex == 0)
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
                        {
                            if (targetFallbackIndex == 0)
                                Logger.Warn($"Could not set endpoint! Host: {nextTcpTarget.Host}; Port: {nextTcpTarget.Port}; Details: {setEndpointEx.Message} ({setEndpointEx.GetType().Name})");
                            else
                                Logger.Warn($"Could not set fallback endpoint number {targetFallbackIndex}! Host: {nextTcpTarget.Host}; Port: {nextTcpTarget.Port}; Details: {setEndpointEx.Message} ({setEndpointEx.GetType().Name})");
                        }
                    }
                }
            }
        }

        protected void SubscribeCommunicationEvents(AbstractTcpCom tcpComm)
        {
            tcpComm.ConnectionEstablished += new EventHandler<ConnectionStateChangedEventArgs>(OnTcpComm_ConnectionEstablished);
            tcpComm.ConnectionClosed += new EventHandler<ConnectionStateChangedEventArgs>(OnTcpComm_ConnectionClosed);

            tcpComm.RawMessageReceivedDelegate = OnRawMessageReceived;
        }

        private void OnTcpComm_ConnectionEstablished(object sender, ConnectionStateChangedEventArgs e)
        {
            try
            {
                this.CreateSession(e.Client.SessionId, e.Client.SessionInfo, e.Client.ForceClose, e.Client);
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
                this.ProcessSessionTerminated(e.Client.SessionId, "Tcp");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            if (connectionType == ConnectionType.Client)
            {
                Task.Run(StartAutoReconnectAsync);
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


        public async ValueTask SendMessageAsync(IGenericMessage message, int receiverSessionId, object context)
        {
            byte[] msgBytes = serializer.SerializeToBytes(message, context);
            byte[] encapsulatedMessageBytes = AbstractTcpCom.CreateMessage(serializer.MessageFormat, msgBytes);

            if (logDataStream)
            {
                // todo: create async version
                dataStreamLogger.LogStreamMessage(receiverSessionId, false, msgBytes, serializer.MessageFormat != RawMessageFormat.JSON);
            }

            await communication.SendAsync(encapsulatedMessageBytes, receiverSessionId);
        }


        /// <summary>
        /// Called when [raw message received].
        /// </summary>
        /// <param name="rawMessage">The raw message.</param>
        public async ValueTask OnRawMessageReceived(IRawMessage rawMessage)
        {
            var tuple = new Tuple<int, byte[]>(rawMessage.SessionId, rawMessage.Data);
            await receiverQueue.Writer.WriteAsync(tuple);
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
