using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Security.Authentication;


namespace BSAG.IOCTalk.Communication.Tcp.Security
{
    /// <summary>
    /// Secure tcp client using the TLS protocol
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/24/2015 4:53:46 PM.
    ///  </para>
    /// </remarks>
    public class SecureTcpClient : TcpClientCom
    {
        #region fields

        private SslStream tlsStream;
        private SslProtocols protocol = SslProtocols.Tls;

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpClient</c>.
        /// </summary>
        public SecureTcpClient()
        {
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the name of the server.
        /// </summary>
        /// <value>
        /// The name of the server.
        /// </value>
        public string ServerName { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether [provide client certificate].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [provide client certificate]; otherwise, <c>false</c>.
        /// </value>
        public bool ProvideClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets the name of the client certificate.
        /// </summary>
        /// <value>
        /// The name of the client certificate.
        /// </value>
        public string ClientCertificateName { get; set; }

        /// <summary>
        /// Gets or sets the protocol.
        /// </summary>
        /// <value>
        /// The protocol.
        /// </value>
        public SslProtocols Protocol
        {
            get { return protocol; }
            set { protocol = value; }
        }


        #endregion

        #region methods

        /// <summary>
        /// Connects to the remote endpoint
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public override bool Connect(out string errorMsg)
        {
            try
            {

                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.InitSocketProperties(this.socket);
                this.socket.Connect(EndPoint);

                var networkStream = new NetworkStream(this.socket, true);
                this.tlsStream = new SslStream(networkStream, false, OnValidateServerCertificate);

                Logger.Info("Authenticate client...");

                if (ProvideClientCertificate)
                {
                    X509Certificate2 clientCertificate = SecureTcpServer.GetCertificateByName(ClientCertificateName);
                    X509Certificate2Collection clientCerts = new X509Certificate2Collection(clientCertificate);

                    tlsStream.AuthenticateAsClient(ServerName, clientCerts, protocol, true);
                }
                else
                {
                    tlsStream.AuthenticateAsClient(ServerName);
                }

                this.client = new Client(this.socket, this.tlsStream, new ConcurrentQueue<IGenericMessage>(), socket.LocalEndPoint, socket.RemoteEndPoint, Logger);

                OnConnectionEstablished(client);

                StartReceivingData(client);
            }
            catch (Exception ex)
            {
                if (this.socket != null
                    && this.socket.Connected)
                {
                    this.socket.Close();
                }

                errorMsg = ex.ToString();
                return false;
            }

            errorMsg = null;
            return true;
        }

        /// <summary>
        /// Validates the server certificate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns></returns>
        private bool OnValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            else
            {
                this.Logger.Error(string.Format("Certificate error: {0}", sslPolicyErrors));
                return false;
            }
        }

        #endregion
    }
}
