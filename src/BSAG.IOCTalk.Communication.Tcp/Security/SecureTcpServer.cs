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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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

        private SslProtocols protocol = SslProtocols.Tls12;
        private X509Certificate2 certificate;
        private readonly object certificateSyncLock = new object();
        private bool certificateInfoLogged;

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpServer</c>.
        /// </summary>
        public SecureTcpServer()
        {
        }

        /// <summary>
        /// Creates and initializes an instance of the class <c>SecureTcpServer</c>.
        /// </summary>
        /// <param name="certificate">Used certificate</param>
        public SecureTcpServer(X509Certificate2 certificate)
        {
            this.certificate = certificate;
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
        /// Gets or sets the max. duration of the TLS handshake. Connections not completing the handshake in time are closed.
        /// <see cref="TimeSpan.Zero"/> or a negative value disables the timeout. Default: 30 seconds.
        /// </summary>
        public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
            }
            catch (ObjectDisposedException)
            {
                /* listener closed */
            }
            catch (Exception ex)
            {
                Logger?.Error(ex.ToString());
            }

            // Accept the next connection immediately. The TLS handshake must not run inside the accept loop:
            // a remote endpoint that connects but never completes the handshake (e.g. a port scanner) would otherwise
            // block all further connections until the listen backlog is full.
            try
            {
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (ObjectDisposedException)
            {
                /* listener closed */
            }
            catch (Exception ex)
            {
                Logger?.Error($"Error accepting new connections: {ex}");
            }

            if (clientSocket != null)
            {
                Task.Run(() => AuthenticateClientAsync(clientSocket));
            }
        }

        private async Task AuthenticateClientAsync(Socket clientSocket)
        {
            EndPoint remoteEndPoint = null;
            SslStream tlsStream = null;
            try
            {
                remoteEndPoint = clientSocket.RemoteEndPoint;
                clientSocket.ReceiveBufferSize = this.ReceiveBufferSize;

                X509Certificate2 serverCertificate = GetServerCertificate();

                tlsStream = new SslStream(new NetworkStream(clientSocket), false);

                Task authTask = tlsStream.AuthenticateAsServerAsync(serverCertificate, ClientCertificateRequired, protocol, true);

                TimeSpan handshakeTimeout = HandshakeTimeout;
                if (handshakeTimeout > TimeSpan.Zero)
                {
                    using (var delayCts = new CancellationTokenSource())
                    {
                        Task completedTask = await Task.WhenAny(authTask, Task.Delay(handshakeTimeout, delayCts.Token)).ConfigureAwait(false);
                        delayCts.Cancel();

                        if (completedTask != authTask)
                        {
                            // observe the exception raised by the pending handshake after the socket is closed
                            _ = authTask.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                            Logger?.Warn($"TLS handshake timeout ({handshakeTimeout.TotalSeconds:0.#} s) - close connection from {remoteEndPoint}");
                            CloseQuietly(tlsStream, clientSocket);
                            return;
                        }
                    }
                }

                // throws if the handshake failed
                await authTask.ConfigureAwait(false);

                Client client = new Client(clientSocket, tlsStream, new ConcurrentQueue<IGenericMessage>(), clientSocket.LocalEndPoint, remoteEndPoint, Logger, this);
                StartReceivingData(client);
                lock (clients)
                {
                    clients.Add(client.SessionId, client);
                }

                OnConnectionEstablished(client);
            }
            catch (ObjectDisposedException)
            {
                CloseQuietly(tlsStream, clientSocket);
            }
            catch (Exception ex)
            {
                Logger?.Error($"TLS connection setup for {remoteEndPoint} failed: {ex}");
                CloseQuietly(tlsStream, clientSocket);
            }
        }

        private X509Certificate2 GetServerCertificate()
        {
            lock (certificateSyncLock)
            {
                if (certificate == null)
                {
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
                }

                if (!certificateInfoLogged)
                {
                    Logger?.Info($"Use certificate \"{certificate.SubjectName.Name}\"; Thumbprint: {certificate.Thumbprint}; Issuer: {certificate.Issuer}");
                    certificateInfoLogged = true;
                }

                return certificate;
            }
        }

        private static void CloseQuietly(SslStream tlsStream, Socket clientSocket)
        {
            try
            {
                tlsStream?.Dispose();
            }
            catch
            {
                /* ignore */
            }

            try
            {
                clientSocket?.Close();
            }
            catch
            {
                /* ignore */
            }
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
