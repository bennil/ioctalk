using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Net.Security;
using System.Security.Authentication;

namespace BSAG.IOCTalk.Communication.Tcp.Security
{
    /// <summary>
    /// Secure tcp server using the TLS protocol
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/24/2015 5:40:46 PM.
    ///  </para>
    /// </remarks>
    public class SecureTcpServer : TcpServiceCom
    {
        #region fields

        private SslProtocols protocol = SslProtocols.Tls;

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpServer</c>.
        /// </summary>
        public SecureTcpServer()
        {
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the name of the certificate.
        /// </summary>
        /// <value>
        /// The name of the certificate.
        /// </value>
        public string CertificateName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [client certificate required].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [client certificate required]; otherwise, <c>false</c>.
        /// </value>
        public bool ClientCertificateRequired { get; set; }


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
        public string CertificateFilename { get; set; }

        /// <summary>
        /// Gets or sets the local certificate file password.
        /// </summary>
        public string CertificateFilePassword { get; set; }


        #endregion

        #region methods

        protected override void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket listener = (Socket)asyncResult.AsyncState;

            Socket clientSocket = null;
            try
            {
                clientSocket = listener.EndAccept(asyncResult);
                clientSocket.ReceiveBufferSize = this.ReceiveBufferSize;
                
                X509Certificate2 certificate;
                if (!string.IsNullOrEmpty(CertificateFilename))
                {
                    Logger?.Info($"Load certificate file: \"{CertificateFilename}\"");
                    certificate = GetCertificateByFilename(CertificateFilename, CertificateFilePassword);
                }
                else
                {
                    Logger?.Info($"Load certificate from store: \"{CertificateName}\"; Location: {Location}");
                    certificate = GetCertificateByName(CertificateName, Location);
                }
                Logger?.Info($"Certificate \"{certificate.SubjectName.Name}\" loaded successfully - Thumbprint: {certificate.Thumbprint}");

                SslStream tlsStream = new SslStream(new NetworkStream(clientSocket), false);
                tlsStream.AuthenticateAsServer(certificate, ClientCertificateRequired, protocol, true);

                Client client = new Client(clientSocket, tlsStream, new ConcurrentQueue<IGenericMessage>(), clientSocket.LocalEndPoint, clientSocket.RemoteEndPoint, Logger);
                StartReceivingData(client);
                clients.Add(client.SessionId, client);

                OnConnectionEstablished(client);
            }
            catch (ObjectDisposedException)
            {
                /* ignore */
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                try
                {
                    if (clientSocket != null
                        && clientSocket.Connected)
                    {
                        clientSocket.Close();
                    }
                }
                catch
                {
                    /* ignore */
                }
            }

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);         
        }

        public static X509Certificate2 GetCertificateByName(string name, StoreLocation location)
        {
            X509Store certStore = new X509Store(StoreName.My, location);
            certStore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certificates = certStore.Certificates.Find(X509FindType.FindBySubjectName, name, true);
            certStore.Close();

            if (certificates.Count == 0)
            {
                throw new KeyNotFoundException(string.Format("Certificate \"{0}\" not found! Store: {1}; Location: {2}", name, certStore.Name, certStore.Location));
            }

            return certificates[0];

        }

        public static X509Certificate2 GetCertificateByFilename(string fileName, string password)
        {
            X509Certificate2 cert = new X509Certificate2(fileName, password);
            return cert;
        }

        #endregion
    }
}
