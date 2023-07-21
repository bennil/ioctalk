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
        private SslProtocols protocol = SslProtocols.Tls12;
        private X509Certificate2 clientCertificate;

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpClient</c>.
        /// </summary>
        public SecureTcpClient()
        {
        }

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpClient</c>.
        /// </summary>
        /// <param name="clientCertificate">Provides the given certificate to the server.</param>
        public SecureTcpClient(X509Certificate2 clientCertificate)
        {
            this.clientCertificate = clientCertificate;
            this.ProvideClientCertificate = true;
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

        /// <summary>
        /// Gets or sets the store location
        /// </summary>
        public StoreLocation Location { get; set; } = StoreLocation.LocalMachine;

        /// <summary>
        /// Gets or sets the certificate filename (no store is used).
        /// </summary>
        public string ClientCertificateFilename { get; set; }

        /// <summary>
        /// Gets or sets the local certificate file password.
        /// </summary>
        public string ClientCertificateFilePassword { get; set; }

        /// <summary>
        /// If the store does not accept the remote certificate the chain will be rechecked against the LocalCertFilename.
        /// </summary>
        public bool CheckLocalCertFile { get; set; }

        /// <summary>
        /// The local certificate file for remote chain validation check
        /// </summary>
        public string LocalCertFilename { get; set; }

        /// <summary>
        /// Gets or sets a custom remote certificate validation method
        /// </summary>
        public RemoteCertificateValidationCallback CustomRemoteCertificationValidation { get; set; }

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
                if (EndPoint == null
                    || (dnsResolveTimeUtc.HasValue && (DateTime.UtcNow - dnsResolveTimeUtc.Value) > RenewDnsResolutionTime))  // renew outdated dns resolution
                {
                    // try get dns
                    SetEndPoint(this.host, this.port);
                }

                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.InitSocketProperties(this.socket);
                this.socket.Connect(EndPoint);

                var networkStream = new NetworkStream(this.socket, true);
                if (CustomRemoteCertificationValidation != null)
                    this.tlsStream = new SslStream(networkStream, false, CustomRemoteCertificationValidation);
                else
                    this.tlsStream = new SslStream(networkStream, false, OnValidateServerCertificate);

                if (ServerName == null)
                    ServerName = host;

                Logger.Info($"Authenticate client for \"{ServerName}\"...");

                if (ProvideClientCertificate)
                {
                    if (clientCertificate == null)
                    {
                        if (!string.IsNullOrEmpty(ClientCertificateFilename))
                        {
                            Logger?.Info($"Load client certificate file: \"{ClientCertificateFilename}\"");
                            clientCertificate = SecureTcpServer.GetCertificateByFilename(ClientCertificateFilename, ClientCertificateFilePassword);
                        }
                        else
                        {
                            Logger?.Info($"Load client certificate from store: \"{ClientCertificateName}\"; Location: {Location}");
                            clientCertificate = SecureTcpServer.GetCertificateByName(ClientCertificateName, Location);
                        }
                    }
                    Logger?.Info($"Use client certificate \"{clientCertificate.SubjectName.Name}\"; Thumbprint: {clientCertificate.Thumbprint}; Issuer: {clientCertificate.Issuer}");

                    X509Certificate2Collection clientCerts = new X509Certificate2Collection(clientCertificate);

                    tlsStream.AuthenticateAsClient(ServerName, clientCerts, protocol, true);
                }
                else
                {
                    tlsStream.AuthenticateAsClient(ServerName);
                }

                this.client = new Client(this.socket, this.tlsStream, new ConcurrentQueue<IGenericMessage>(), socket.LocalEndPoint, socket.RemoteEndPoint, Logger, this);

                StartReceivingData(client);

                OnConnectionEstablished(client);
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
                if (CheckLocalCertFile)
                {
                    X509Certificate2 customCertAuthority = new X509Certificate2(LocalCertFilename);

                    // Check if CA certificate is available in the chain.
                    var isInChain = chain.ChainElements.Cast<X509ChainElement>()
                                              .Select(element => element.Certificate)
                                              .Where(chainCertificate => chainCertificate.Subject == customCertAuthority.Subject)
                                              .Where(chainCertificate => chainCertificate.GetRawCertData().SequenceEqual(customCertAuthority.GetRawCertData()))
                                              .Any();

                    if (!isInChain)
                    {
                        this.Logger.Error($"Certificate error: {sslPolicyErrors} - local cert also not in chain; Certificate: {certificate}");
                    }
                    return isInChain;
                }
                else
                {
                    this.Logger.Error($"Certificate error: {sslPolicyErrors}; Certificate: {certificate}");
                    return false;
                }
            }
        }

        #endregion
    }
}
