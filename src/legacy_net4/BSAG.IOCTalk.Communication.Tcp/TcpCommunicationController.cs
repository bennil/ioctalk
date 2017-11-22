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

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpCommunicationController properties
        // ----------------------------------------------------------------------------------------
        // TcpCommunicationController properties
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Gets or sets the config.
        /// </summary>
        /// <value>
        /// The config.
        /// </value>
        public XDocument Config { get; set; }

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
            if (Config == null)
            {
                throw new NullReferenceException("TCP Config XML is not defined!");
            }

            if (communication != null)
            {
                throw new InvalidOperationException(GetType().Name + " already initialized!");
            }

            ConnectionType connType = Config.Root.GetConfigParameterValue<ConnectionType>(ConfigParamConnectionType);
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

            this.LogDataStream = Config.Root.GetConfigParameterValueOrDefault<bool>(false, ConfigParamLogDataStream);
            
            if (connType == ConnectionType.Client)
            {
                InitClient(securityXml, isSecurityEnabled);
            }
            else if (connType == ConnectionType.Service)
            {
                InitService(securityXml, isSecurityEnabled);
            }
            else
            {
                throw new InvalidOperationException("TCP ConnectionType is undefined!");
            }
        }

        private void InitClient(XElement securityXml, bool isSecurityEnabled)
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

            client.Logger = this.logger;
            client.ConnectionClosed += new EventHandler<ConnectionStateChangedEventArgs>(OnClient_ConnectionClosed);
            SubscribeCommunicationEvents(client);

            string host = Config.Root.GetConfigParameterValue<string>(ConfigParamHost);
            int port = Config.Root.GetConfigParameterValue<int>(ConfigParamPort);
            client.Init(host, port);

            this.communication = client;

            if (LogDataStream)
            {
                dataStreamLogger.Init(this, host.Replace('.', '_') + "-" + port, Config.Root);
            }

            string errMsg;
            if (this.communication.Connect(out errMsg))
            {
                logger.Info(string.Format("Tcp client connection \"{0}\" established", client.EndPoint.ToString()));
            }
            else
            {
                logger.Error(errMsg);

                // Start async auto reconnect
                StartAutoReconnectAsync();
            }
        }

        private void InitService(XElement securityXml, bool isSecurityEnabled)
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
            service.Logger = this.logger;


            SubscribeCommunicationEvents(service);

            int servicePort = Config.Root.GetConfigParameterValue<int>(ConfigParamPort);
            service.Init(servicePort);
            this.communication = service;
            
            if (LogDataStream)
            {
                dataStreamLogger.Init(this, "Service-" + servicePort, Config.Root);
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
            this.communication.Close();

            base.Shutdown();
        }


        private void OnClient_ConnectionClosed(object sender, ConnectionStateChangedEventArgs e)
        {
            StartAutoReconnectAsync();
        }

        private void StartAutoReconnectAsync()
        {
            if (Interlocked.Exchange(ref clientAutoReconnectLock, 1) == 0)    // only start auto reconnect task once
            {
                Task taskClientReconnect = new Task(new Action(() =>
                {
                    Thread.Sleep(1000);

                    string errMsg;
                    while (!this.communication.Connect(out errMsg))
                    {
                        Thread.Sleep(1000);
                    }

                    Interlocked.Exchange(ref clientAutoReconnectLock, 0);
                }));
                taskClientReconnect.Start();
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
            this.CreateSession(e.Client.SessionId, e.Client.SessionInfo);
        }

        private void OnTcpComm_ConnectionClosed(object sender, ConnectionStateChangedEventArgs e)
        {
            this.ProcessSessionTerminated(e.Client.SessionId);
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
            byte[] encapsulatedMessageBytes = AbstractTcpCom.CreateMessage(RawMessageFormat.JSON, msgBytes);

            if (logDataStream)
            {
                dataStreamLogger.LogStreamMessage(receiverSessionId, false, msgBytes);
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
        public bool IsAsyncSendCurrentlyPossible(ISession session)
        {
            return !communication.IsSendBufferUnderPressure(session.SessionId);
        }



        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
