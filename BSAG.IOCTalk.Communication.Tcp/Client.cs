using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Exceptions;

namespace BSAG.IOCTalk.Communication.Tcp
{
    /// <summary>
    /// Tcp client
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 22.09.2010
    /// </remarks>
    public class Client
    {
        #region Client fields
        // ----------------------------------------------------------------------------------------
        // Client fields
        // ----------------------------------------------------------------------------------------

        public Socket socket = null;
        public ConcurrentQueue<IGenericMessage> queueReceivedPackets;

        private DateTime connectTime;
        private long isSocketClosedExecuted = 0;
        private EndPoint localEndPoint = null;
        private EndPoint remoteEndPoint = null;
        private int connectionSessionId;
        private bool isSendBufferUnderPressure = false;
        private SpinLock spinLock = new SpinLock();
        private ILogger logger;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region Client constructors
        // ----------------------------------------------------------------------------------------
        // Client constructors
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="queueReceivedPackets">The queue received packets.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="logger">The logger.</param>
        public Client(Socket socket, ConcurrentQueue<IGenericMessage> queueReceivedPackets, EndPoint localEndPoint, EndPoint remoteEndPoint, ILogger logger)
        {
            this.logger = logger;
            this.socket = socket;
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.queueReceivedPackets = queueReceivedPackets;
            this.connectTime = DateTime.Now;
            this.connectionSessionId = AbstractTcpCom.GetNewConnectionSessionId();
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region Client properties
        // ----------------------------------------------------------------------------------------
        // Client properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the connect time.
        /// </summary>
        /// <value>The connect time.</value>
        public DateTime ConnectTime
        {
            get
            {
                return connectTime;
            }
        }

        /// <summary>
        /// Gets the local socket IP end point.
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get
            {
                return localEndPoint;
            }
        }

        /// <summary>
        /// Gets the remote IP end point.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                return remoteEndPoint;
            }
        }


        /// <summary>
        /// Gets the receive message queue.
        /// </summary>
        public ConcurrentQueue<IGenericMessage> ReceiveMessageQueue
        {
            get { return queueReceivedPackets; }
        }



        /// <summary>
        /// Gets the session id.
        /// </summary>
        public int SessionId
        {
            get { return connectionSessionId; }
        }

        /// <summary>
        /// Gets the session info.
        /// </summary>
        public string SessionInfo
        {
            get
            {
                return string.Concat(localEndPoint.ToString(), " <> ", remoteEndPoint.ToString());
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ITransferPacketSession"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get { return socket.Connected; }
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region Client methods
        // ----------------------------------------------------------------------------------------
        // Client methods
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Sends the specified data bytes.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <returns></returns>
        public void Send(byte[] dataBytes)
        {
            bool lockTaken = false;
            try
            {
                //int sentCount = 0;
                int length = dataBytes.Length;

                // lock socket send
                do
                {
                    spinLock.Enter(ref lockTaken);

                    if (!lockTaken)
                        Thread.Sleep(0);
                } while (!lockTaken);


                socket.Send(dataBytes);

                // non blocking socket code:
                //do
                //{
                //    SocketError errorCode;
                //    sentCount += socket.Send(dataBytes, sentCount, length - sentCount, SocketFlags.None, out errorCode);

                //    if (errorCode != SocketError.Success)
                //    {
                //        switch (errorCode)
                //        {
                //            case SocketError.NoBufferSpaceAvailable:
                //            case SocketError.IOPending:
                //            case SocketError.WouldBlock:
                //                isSendBufferUnderPressure = true;

                //                Thread.Sleep(50);   // wait 50 milliseconds before retry
                //                break;

                //            default:
                //                logger.Error(string.Format("Socket.Send error code: {0}; Session: {1}", errorCode, SessionInfo));
                //                return false;
                //        }
                //    }

                //    if (sentCount < length)
                //    {
                //        isSendBufferUnderPressure = true;
                //    }
                //} while (sentCount < length);
            }
            catch (ObjectDisposedException)
            {
                throw new RemoteConnectionLostException(null);
            }
            catch (SocketException sockEx)
            {
                SocketError errorCode = (SocketError)sockEx.ErrorCode;
                switch (errorCode)
                {
                    case SocketError.Shutdown:
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                    case SocketError.Disconnecting:
                        throw new RemoteConnectionLostException(null);

                    default:
                        throw sockEx;
                }
            }
            finally
            {
                spinLock.Exit();
            }
        }

        /// <summary>
        /// Determines whether [is send buffer under pressure] and resets the flag.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is send buffer under pressure]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSendBufferUnderPressure()
        {
            if (isSendBufferUnderPressure)
            {
                isSendBufferUnderPressure = false;  // reset flag
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Acquires the socket close processing lock
        /// </summary>
        internal bool AcquireIsSocketClosedExecuted()
        {
            return Interlocked.Exchange(ref isSocketClosedExecuted, 1) == 0;
        }

        /// <summary>
        /// Resets the socket close processing lock
        /// </summary>
        internal void ResetIsSocketClosedExecuted()
        {
            Interlocked.Exchange(ref isSocketClosedExecuted, 0);
        }
        // ----------------------------------------------------------------------------------------
        #endregion

    }

}
