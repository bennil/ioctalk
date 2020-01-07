using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Exceptions;

namespace BSAG.IOCTalk.Communication.Tcp
{
    /// <summary>
    /// The TcpServiceCom class manages a TCP service listener.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 06.09.2010
    /// </remarks>
    public class TcpServiceCom : AbstractTcpCom
    {
        #region TcpServiceCom fields
        // ----------------------------------------------------------------------------------------
        // TcpServiceCom fields
        // ----------------------------------------------------------------------------------------
        protected Dictionary<int, Client> clients = new Dictionary<int, Client>();
        private DateTime connectTime;
        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpServiceCom constructors
        // ----------------------------------------------------------------------------------------
        // TcpServiceCom constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <c>TcpServiceCom</c>.
        /// </summary>
        public TcpServiceCom()
        {
            MaxConnectionCount = 350;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpServiceCom properties
        // ----------------------------------------------------------------------------------------
        // TcpServiceCom properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the max connection count.
        /// </summary>
        /// <value>The max connection count.</value>
        public int MaxConnectionCount { get; set; }


        /// <summary>
        /// Gets the clients.
        /// </summary>
        /// <value>The clients.</value>
        public IEnumerable<Client> Clients
        {
            get
            {
                return clients.Values;
            }
        }
         
        /// <summary>
        /// Gets the transfer session connect time.
        /// </summary>
        public DateTime ConnectTime
        {
            get { return connectTime; }
        }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public int SessionId
        {
            get { return socket.GetHashCode(); }
        }

        /// <summary>
        /// Gets the session info.
        /// </summary>
        public string SessionInfo
        {
            get
            {
                if (EndPoint == null)
                    return null;

                return EndPoint.ToString();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ITransferSession"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get
            {
                if (socket != null)
                    return socket.Connected;

                return false;
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpServiceCom methods
        // ----------------------------------------------------------------------------------------
        // TcpServiceCom methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Initialisiert die TCP Kommunikation.
        /// </summary>
        public void Init(int servicePort)
        {
            EndPoint = new IPEndPoint(IPAddress.Any, servicePort);

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.InitSocketProperties(socket);
        }




        /// <summary>
        /// Startet die Verbindung
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public override bool Connect(out string errorMsg)
        {
            try
            {
                this.socket.Bind(EndPoint);
                this.socket.Listen(MaxConnectionCount);

                connectTime = DateTime.Now;

                // Wait for remote connection
                this.socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);
            }
            catch (Exception ex)
            {
                errorMsg = $"Error connect \"{EndPoint}\" Details: {ex.Message} {ex.GetType().Name}";
                return false;
            }

            errorMsg = null;
            return true;
        }

        protected virtual void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket listener = (Socket)asyncResult.AsyncState;

            try
            {
                Socket clientSocket = listener.EndAccept(asyncResult);
                clientSocket.ReceiveBufferSize = this.ReceiveBufferSize;
                
                Client client = new Client(clientSocket, new NetworkStream(clientSocket), new ConcurrentQueue<IGenericMessage>(), clientSocket.LocalEndPoint, clientSocket.RemoteEndPoint, Logger);
                clients.Add(client.SessionId, client);

                OnConnectionEstablished(client);

                StartReceivingData(client);
                
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (SocketException)
            {
                /* ignore */
            }
            catch (ObjectDisposedException)
            {
                /* ignore */
            }
        }



        /// <summary>
        /// Closes the TCP Connection.
        /// </summary>
        public override void Close()
        {
            if (this.socket != null)
            {
                this.socket.Close();
            }

            foreach (var client in clients.Values)
            {
                Close(client);
            }

            clients.Clear();
        }



        /// <summary>
        /// Sends the specified data bytes.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public override void Send(byte[] dataBytes, int receiverId)
        {
            Client client;
            if (clients.TryGetValue(receiverId, out client))
            {
                client.Send(dataBytes);
            }
            else
            {
                throw new OperationCanceledException("Remote connction lost - Session ID: " + receiverId);
            }
        }

        public override bool IsSendBufferUnderPressure(int receiverId)
        {
            return false; // always return false using blocking tcp sockets
            //Client client;
            //if (clients.TryGetValue(receiverId, out client))
            //{
            //    return client.IsSendBufferUnderPressure();
            //}
            //return false;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

    }

}
