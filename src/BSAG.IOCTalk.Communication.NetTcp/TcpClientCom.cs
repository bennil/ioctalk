﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Exceptions;
using System.Threading.Tasks;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;

namespace BSAG.IOCTalk.Communication.NetTcp
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
        protected string host;
        protected int port;
        private string endPointInfo;
        protected DateTime? dnsResolveTimeUtc = null;
        IPAddress[] multiDnsResolve = null;
        int lastMultiDnsIndex = 0;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TcpClientCom constructors
        // ----------------------------------------------------------------------------------------
        // TcpClientCom constructors
        // ----------------------------------------------------------------------------------------
        ///// <summary>
        ///// Erstellt eine neue Instanz der Klasse <c>TcpClientCom</c>.
        ///// </summary>
        //public TcpClientCom()
        //    : base(new LegacyWireFraming())
        //{
        //}

        public TcpClientCom(AbstractWireFraming wireFraming)
            : base(wireFraming)
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

        public virtual bool IsConnected
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
        public override DateTime? ConnectTimeUtc
        {
            get { return client?.ConnectTimeUtc; }
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


        public TimeSpan RenewDnsResolutionTime { get; set; } = TimeSpan.FromMinutes(5);


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
                if (EndPoint == null
                    || (dnsResolveTimeUtc.HasValue && (DateTime.UtcNow - dnsResolveTimeUtc.Value) > RenewDnsResolutionTime))  // renew outdated dns resolution
                {
                    // try get dns
                    SetEndPoint(this.host, this.port);
                }

                this.socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.InitSocketProperties(this.socket);
                this.socket.Connect(EndPoint);

                this.client = new Client(this.socket, new NetworkStream(this.socket), new ConcurrentQueue<IGenericMessage>(), socket.LocalEndPoint, socket.RemoteEndPoint, Logger, this);

                OnConnectionEstablished(client);

                StartReceivingData(client);
            }
            catch (Exception ex)
            {
                errorMsg = $"Error connect to \"{EndPoint}\" Details: {ex.Message} {ex.GetType().Name}";

                if (multiDnsResolve != null)
                {
                    lastMultiDnsIndex++;
                    if (multiDnsResolve.Length <= lastMultiDnsIndex)
                        lastMultiDnsIndex = 0;

                    var alternativeDnsAddress = multiDnsResolve[lastMultiDnsIndex];
                    EndPoint = new IPEndPoint(alternativeDnsAddress, port);
                }

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
                    if (hostEntry.AddressList.Length > 1)
                    {
                        Logger.Info($"{hostEntry.AddressList.Length} DNS resolve items for host \"{hostEntry.HostName}\" ({string.Join("; ", hostEntry.AddressList.Select(a => a.ToString()))}");
                        multiDnsResolve = hostEntry.AddressList;
                    }

                    var resolvedIp = hostEntry.AddressList[lastMultiDnsIndex < hostEntry.AddressList.Length ? lastMultiDnsIndex : 0];
                    this.EndPoint = new IPEndPoint(resolvedIp, port);
                    endPointInfo = $"{host}:{port} ({resolvedIp}) {resolvedIp.AddressFamily}";

                    this.dnsResolveTimeUtc = DateTime.UtcNow;
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
                this.Close(client, "Close");

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

        public override void Send(ReadOnlySpan<byte> dataBytes, int receiverId)
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
        /// Sends the specified data bytes async.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public override async ValueTask SendAsync(byte[] dataBytes, int receiverId)
        {
            if (client != null)
            {
                await client.SendAsync(dataBytes);
            }
            else
            {
                throw new OperationCanceledException("Remote connction lost - Session ID: " + receiverId);
            }
        }

        public override async ValueTask SendAsync(ReadOnlyMemory<byte> dataBytes, int receiverId)
        {
            if (client != null)
            {
                await client.SendAsync(dataBytes);
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
