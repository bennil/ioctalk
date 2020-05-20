using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Exceptions;

namespace BSAG.IOCTalk.Communication.Tcp
{
    /// <summary>
    /// The TcpClientCom manages a TCP client connection.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 06.09.2010
    /// </remarks>
    public class TcpClientCom : AbstractTcpCom
    {
        #region TcpClientCom fields
        // ----------------------------------------------------------------------------------------
        // TcpClientCom fields
        // ----------------------------------------------------------------------------------------

        protected Client client = null;
        private string host;
        private int port;
        private string endPointInfo;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpClientCom constructors
        // ----------------------------------------------------------------------------------------
        // TcpClientCom constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <c>TcpClientCom</c>.
        /// </summary>
        public TcpClientCom()
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpClientCom properties
        // ----------------------------------------------------------------------------------------
        // TcpClientCom properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        public Client Client
        {
            get
            {
                return client;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (socket == null)
                    return false;

                return socket.Connected;
            }
        }


        /// <summary>
        /// Gets the transfer session connect time.
        /// </summary>
        public DateTime ConnectTime
        {
            get { return client.ConnectTime; }
        }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public int SessionId
        {
            get { return client.SessionId; }
        }

        /// <summary>
        /// Gets the session info.
        /// </summary>
        public string SessionInfo
        {
            get { return client.SessionInfo; }
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
                if (socket == null)
                    return false;

                return socket.Connected;
            }
        }

        public override string EndPointInfo => endPointInfo;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpClientCom methods
        // ----------------------------------------------------------------------------------------
        // TcpClientCom methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes the TCP client communication
        /// </summary>
        public void Init(string host, int port)
        {
            this.host = host;
            this.port = port;

            try
            {
                SetEndPoint(host, port);
            }
            catch 
            {
                // ignore dns failure > set endpoint on connect
            }
        }

        /// <summary>
        /// Connects to the remote endpoint
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public override bool Connect(out string errorMsg)
        {
            try
            {
                if (EndPoint == null)
                {
                    // try get dns
                    SetEndPoint(this.host, this.port);
                }

                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.InitSocketProperties(this.socket);
                this.socket.Connect(EndPoint);

                this.client = new Client(this.socket, new NetworkStream(this.socket), new ConcurrentQueue<IGenericMessage>(), socket.LocalEndPoint, socket.RemoteEndPoint, Logger);

                StartReceivingData(client);

                OnConnectionEstablished(client);
            }
            catch (Exception ex)
            {
                errorMsg = $"Error connect to \"{EndPoint}\" Details: {ex.Message} {ex.GetType().Name}";

                return false;
            }

            errorMsg = null;
            return true;
        }



        /// <summary>
        /// Sets the end point.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        internal void SetEndPoint(string host, int port)
        {
            IPAddress ip = null;
            if (IPAddress.TryParse(host, out ip))
            {
                // IP Adresse setzen
                EndPoint = new IPEndPoint(ip, port);
                endPointInfo = EndPoint.ToString();
            }
            else
            {
                // Determine IP using DNS hostname
                IPHostEntry hostEntry = Dns.GetHostEntry(host);

                if (hostEntry.AddressList.Length > 0)
                {
                    var resolvedIp = hostEntry.AddressList[0];
                    this.EndPoint = new IPEndPoint(resolvedIp, port);
                    endPointInfo = $"{host}:{port} ({resolvedIp})";
                }
                else
                {
                    throw new InvalidOperationException("Could not resolve specified host: \"" + host + "\" address!");
                }
            }
        }


        /// <summary>
        /// Closes the TCP connection.
        /// </summary>
        public override void Close()
        {
            if (client != null)
            {
                this.Close(client);

                if (client.queueReceivedPackets != null)
                {
                    // Alle Queue Objekte entfernen
                    IGenericMessage temp;
                    while (client.queueReceivedPackets.TryDequeue(out temp)) ;
                }
            }
        }

        /// <summary>
        /// Sends the specified data bytes.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public override void Send(byte[] dataBytes, int receiverId)
        {
            if (client != null)
            {
                client.Send(dataBytes);
            }
            else
            {
                throw new OperationCanceledException("Remote connction lost - Session ID: " + receiverId);
            }
        }

        /// <summary>
        /// Determines whether [is send buffer under pressure] [the specified receiver id].
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <returns>
        ///   <c>true</c> if [is send buffer under pressure] [the specified receiver id]; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsSendBufferUnderPressure(int receiverId)
        {
            return false; // always return false using blocking tcp socket
            //if (client != null)
            //{
            //    return client.IsSendBufferUnderPressure();
            //}
            //else
            //{
            //    return false;
            //}
        }

        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
