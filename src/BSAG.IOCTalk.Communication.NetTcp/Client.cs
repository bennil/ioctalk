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
using System.IO;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Communication.NetTcp
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

        internal Socket socket = null;
        internal Stream stream;
        public ConcurrentQueue<IGenericMessage> queueReceivedPackets;

        private DateTime connectTimeUtc;
        private long isSocketClosedExecuted = 0;
        private EndPoint localEndPoint = null;
        private EndPoint remoteEndPoint = null;
        private int connectionSessionId;
        private SpinLock spinLock = new SpinLock();
        private ILogger logger;

        SemaphoreSlim semaphoreSlimSendLock = new SemaphoreSlim(1, 1);

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
        /// <param name="stream">The stream.</param>
        /// <param name="queueReceivedPackets">The queue received packets.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="logger">The logger.</param>
        public Client(Socket socket, Stream stream, ConcurrentQueue<IGenericMessage> queueReceivedPackets, EndPoint localEndPoint, EndPoint remoteEndPoint, ILogger logger)
        {
            this.logger = logger;
            this.socket = socket;
            this.stream = stream;
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.queueReceivedPackets = queueReceivedPackets;
            this.connectTimeUtc = DateTime.UtcNow;
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
        public DateTime ConnectTimeUtc
        {
            get
            {
                return connectTimeUtc;
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
            if (!socket.Connected)
                throw new OperationCanceledException("Remote connction lost");

            bool lockTaken = false;
            try
            {
                int length = dataBytes.Length;

                // lock socket send
                do
                {
                    spinLock.Enter(ref lockTaken);

                    if (!lockTaken)
                        Thread.Sleep(0);
                } while (!lockTaken);


                stream.Write(dataBytes, 0, length);
            }
            catch (ObjectDisposedException)
            {
                throw new OperationCanceledException("Remote connction lost");
            }
            catch (IOException ioEx)
            {
                if (ioEx.InnerException is SocketException)
                {
                    SocketException sockEx = (SocketException)ioEx.InnerException;

                    SocketError errorCode = (SocketError)sockEx.ErrorCode;
                    throw new OperationCanceledException($"Remote connction lost (socket error: {errorCode})");
                }
                else
                {
                    throw ioEx;
                }
            }
            catch (SocketException sockEx)
            {
                SocketError errorCode = (SocketError)sockEx.ErrorCode;

                throw new OperationCanceledException($"Remote connction lost (socket error: {errorCode})");
            }
            finally
            {
                spinLock.Exit();
            }
        }

        public void Send(ReadOnlySpan<byte> data)
        {
            if (!socket.Connected)
                throw new OperationCanceledException("Remote connction lost");

            bool lockTaken = false;
            try
            {

                // lock socket send
                do
                {
                    spinLock.Enter(ref lockTaken);

                    if (!lockTaken)
                        Thread.Sleep(0);
                } while (!lockTaken);



                stream.Write(data);



            }
            catch (ObjectDisposedException)
            {
                throw new OperationCanceledException("Remote connction lost");
            }
            catch (IOException ioEx)
            {
                if (ioEx.InnerException is SocketException)
                {
                    SocketException sockEx = (SocketException)ioEx.InnerException;

                    SocketError errorCode = (SocketError)sockEx.ErrorCode;
                    throw new OperationCanceledException($"Remote connction lost (socket error: {errorCode})");
                }
                else
                {
                    throw ioEx;
                }
            }
            catch (SocketException sockEx)
            {
                SocketError errorCode = (SocketError)sockEx.ErrorCode;

                throw new OperationCanceledException($"Remote connction lost (socket error: {errorCode})");
            }
            finally
            {
                spinLock.Exit();
            }
        }


        public async ValueTask SendAsync(byte[] dataBytes)
        {
            if (!socket.Connected)
                throw new OperationCanceledException("Remote connction lost");

            try
            {
                int length = dataBytes.Length;

                // synchronize socket (stream) write
                await semaphoreSlimSendLock.WaitAsync();

                await stream.WriteAsync(dataBytes, 0, length);
            }
            catch (ObjectDisposedException)
            {
                throw new OperationCanceledException("Remote connction lost");
            }
            catch (IOException ioEx)
            {
                if (ioEx.InnerException is SocketException)
                {
                    SocketException sockEx = (SocketException)ioEx.InnerException;

                    SocketError errorCode = (SocketError)sockEx.ErrorCode;
                    switch (errorCode)
                    {
                        case SocketError.Shutdown:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                        case SocketError.Disconnecting:
                            throw new OperationCanceledException("Remote connction lost");

                        default:
                            throw sockEx;
                    }
                }
                else
                {
                    throw ioEx;
                }
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
                        throw new OperationCanceledException("Remote connction lost");

                    default:
                        throw sockEx;
                }
            }
            finally
            {
                semaphoreSlimSendLock.Release();
            }
        }



        public async ValueTask SendAsync(ReadOnlyMemory<byte> dataBytes)
        {
            if (!socket.Connected)
                throw new OperationCanceledException("Remote connction lost");

            try
            {
                int length = dataBytes.Length;

                // synchronize socket (stream) write
                await semaphoreSlimSendLock.WaitAsync();

                //todo: cancelation token ?
                await stream.WriteAsync(dataBytes);
            }
            catch (ObjectDisposedException)
            {
                throw new OperationCanceledException("Remote connction lost");
            }
            catch (IOException ioEx)
            {
                if (ioEx.InnerException is SocketException)
                {
                    SocketException sockEx = (SocketException)ioEx.InnerException;

                    SocketError errorCode = (SocketError)sockEx.ErrorCode;
                    switch (errorCode)
                    {
                        case SocketError.Shutdown:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                        case SocketError.Disconnecting:
                            throw new OperationCanceledException("Remote connction lost");

                        default:
                            throw sockEx;
                    }
                }
                else
                {
                    throw ioEx;
                }
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
                        throw new OperationCanceledException("Remote connction lost");

                    default:
                        throw sockEx;
                }
            }
            finally
            {
                semaphoreSlimSendLock.Release();
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
