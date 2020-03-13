using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Logging;
using System.IO;

namespace BSAG.IOCTalk.Communication.Tcp
{

    /// <summary>
    /// Abstract tcp communication implementation
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 06.09.2010
    /// </remarks>
    public abstract class AbstractTcpCom
    {
        #region AbstractTcpCom fields
        // ----------------------------------------------------------------------------------------
        // AbstractTcpCom fields
        // ----------------------------------------------------------------------------------------

        private static int lastConnectionSessionId = 0;


        protected Socket socket;

        private event EventHandler<ConnectionStateChangedEventArgs> connectionClosed;
        private event EventHandler<ConnectionStateChangedEventArgs> connectionEstablished;
        private RawMessageReceivedDelegate rawMessageReceivedDelegate;
        private int maxReadBufferSize = 3276800;
        private int receiveBufferSize = 16384;
        private int sendBufferSize = 16384;

        private CancellationTokenSource cancelTokenSource;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractTcpCom constructors
        // ----------------------------------------------------------------------------------------
        // AbstractTcpCom constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <c>AbstractTcpCom</c>.
        /// </summary>
        public AbstractTcpCom()
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractTcpCom properties
        // ----------------------------------------------------------------------------------------
        // AbstractTcpCom properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the end point.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        public EndPoint EndPoint { get; set; }


        /// <summary>
        /// Occurs when [connection established].
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionEstablished
        {
            add
            {
                connectionEstablished += value;
            }
            remove
            {
                connectionEstablished -= value;
            }
        }


        /// <summary>
        /// Occurs when [connection closed].
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionClosed
        {
            add
            {
                connectionClosed += value;
            }
            remove
            {
                connectionClosed -= value;
            }
        }



        /// <summary>
        /// Gets or sets the raw message received delegate.
        /// </summary>
        /// <value>
        /// The raw message received delegate.
        /// </value>
        public RawMessageReceivedDelegate RawMessageReceivedDelegate
        {
            get { return rawMessageReceivedDelegate; }
            set { rawMessageReceivedDelegate = value; }
        }


        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        /// <value>
        /// The initial size of the receive buffer.
        /// </value>
        public int ReceiveBufferSize
        {
            get { return receiveBufferSize; }
            set { receiveBufferSize = value; }
        }


        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        /// <value>
        /// The size of the send buffer.
        /// </value>
        public int SendBufferSize
        {
            get { return sendBufferSize; }
            set { sendBufferSize = value; }
        }


        /// <summary>
        /// Gets or sets the max size of the read buffer.
        /// </summary>
        /// <value>
        /// The size of the max read buffer.
        /// </value>
        public int MaxReadBufferSize
        {
            get { return maxReadBufferSize; }
            set { maxReadBufferSize = value; }
        }



        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILogger Logger { get; set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractTcpCom methods
        // ----------------------------------------------------------------------------------------
        // AbstractTcpCom methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Startet die Verbindung
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        public abstract bool Connect(out string errorMsg);

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Closes the TCP Connection.
        /// </summary>
        public virtual void Close(Client client)
        {
            if (client != null
                && client.socket != null
                && client.AcquireIsSocketClosedExecuted())
            {
                cancelTokenSource.Cancel();

                client.socket.Close();

                OnConnectionClosed(client);
            }
        }


        /// <summary>
        /// Starts the receiving data.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        protected SocketState StartReceivingData(Client client)
        {
            this.cancelTokenSource = new CancellationTokenSource();

            SocketState state = new SocketState(receiveBufferSize);
            state.Client = client;

            Task.Run(async () => { await this.OnReceiveDataAsync(state); });

            return state;
        }


        /// <summary>
        /// Called when [receive data].
        /// </summary>
        /// <param name="socketStateObj">The socket state obj.</param>
        protected async Task OnReceiveDataAsync(SocketState state)
        {
            Socket clientSocket = state.Client.socket;
            Stream clientStream = state.Client.stream;
            byte[] readBuffer = state.readBuffer;
            int readBufferLength = readBuffer.Length;
            var cancelToken = cancelTokenSource.Token;

            try
            {
                IRawMessage rawMessage = new RawMessage(state.Client.SessionId);
                IRawMessage pendingMessage = null;
                IRawMessage receivedMessage;
                while (clientSocket.Connected)
                {
                    int bytesReadCount = await clientStream.ReadAsync(readBuffer, 0, readBufferLength, cancelToken);
                    if (bytesReadCount > 0)
                    {
                        int readIndex = 0;
                        while ((receivedMessage = ReadRawMessage(readBuffer, ref readIndex, bytesReadCount, rawMessage, ref pendingMessage)) != null)
                        {
                            // raw message received
                            rawMessageReceivedDelegate(receivedMessage);
                        }

                        if (bytesReadCount == readBuffer.Length
                            && bytesReadCount < maxReadBufferSize)
                        {
                            // auto extend internal read buffer size if complete buffer is filled
                            int newSize = readBuffer.Length * 2;
                            state.readBuffer = new byte[newSize];
                            readBuffer = state.readBuffer;
                            readBufferLength = newSize;
                        }
                    }
                    else
                    {
                        // Connection closed
                        Close(state.Client);
                        return;
                    }
                }

                if (!state.Client.socket.Connected)
                {
                    Close(state.Client);
                    return;
                }
            }
            catch (IOException ioEx)
            {
                if (ioEx.InnerException is SocketException)
                {
                    SocketException sockEx = (SocketException)ioEx.InnerException;

                    CloseOrLogSocketException(state, sockEx);
                }
                else if (ioEx.InnerException is ObjectDisposedException)
                {
                    Close(state.Client);
                }
                else
                {
                    Logger.Error(ioEx.ToString());

                    Close(state.Client);
                }
            }
            catch (SocketException socketEx)
            {
                CloseOrLogSocketException(state, socketEx);
            }
            catch (OperationCanceledException)
            {
                /* connection closed exception */
                Close(state.Client);
            }
            catch (ObjectDisposedException)
            {
                /* connection closed exception */
                Close(state.Client);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                Close(state.Client);
            }
        }

        private void CloseOrLogSocketException(SocketState state, SocketException sockEx)
        {
            SocketError errorCode = (SocketError)sockEx.ErrorCode;
            switch (errorCode)
            {
                case SocketError.Shutdown:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:
                case SocketError.Disconnecting:
                    Close(state.Client);
                    break;

                default:
                    Logger.Error(sockEx.ToString());
                    Close(state.Client);
                    break;
            }
        }

        /// <summary>
        /// Gets the new connection session id.
        /// </summary>
        /// <returns></returns>
        public static int GetNewConnectionSessionId()
        {
            return Interlocked.Increment(ref lastConnectionSessionId);
        }

        /// <summary>
        /// Sends the specified data bytes.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public abstract void Send(byte[] dataBytes, int receiverId);

        /// <summary>
        /// Determines whether [is send buffer under pressure] [the specified receiver id].
        /// </summary>
        /// <param name="receiverId">The receiver id.</param>
        /// <returns>
        ///   <c>true</c> if [is send buffer under pressure] [the specified receiver id]; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsSendBufferUnderPressure(int receiverId);


        /// <summary>
        /// Inits the socket properties.
        /// </summary>
        /// <param name="socket">The socket.</param>
        protected void InitSocketProperties(Socket socket)
        {
            socket.ReceiveBufferSize = receiveBufferSize;
            socket.SendBufferSize = sendBufferSize;
        }


        /// <summary>
        /// Called when [connection established].
        /// </summary>
        /// <param name="client">The client.</param>
        internal void OnConnectionEstablished(Client client)
        {
            Logger.Info(string.Format("Tcp client connection \"{0}\" established", client.RemoteEndPoint.ToString()));

            if (connectionEstablished != null)
            {
                connectionEstablished(this, new ConnectionStateChangedEventArgs(client));
            }
        }


        /// <summary>
        /// Called when [connection closed].
        /// </summary>
        /// <param name="client">The client.</param>
        internal void OnConnectionClosed(Client client)
        {
            Logger.Info(string.Format("Tcp client connection \"{0}\" closed", client.RemoteEndPoint.ToString()));

            if (connectionClosed != null)
            {
                connectionClosed(this, new ConnectionStateChangedEventArgs(client));
            }
        }

        #region message encapsulation


        /// <summary>
        /// Specifies the min start message byte count
        /// </summary>
        public const int StartMessageControlMinByteCount = 12;

        /// <summary>
        /// Specifies the start message control byte count (12)
        /// </summary>
        public const int StartMessageControlByteCount = 10;

        /// <summary>
        /// Message format: 5, 70 , 10 + (2 Bytes message type - short) + (4 Bytes data length - int) + 3 + (message bytes) + 3
        /// </summary>
        public static readonly byte[] StartMessageIdentifier = new byte[] { 5, 70, 10 };

        /// <summary>
        /// First start message identifier byte
        /// </summary>
        public static readonly byte StartMessageIdentifier1 = 5;

        /// <summary>
        /// Data border control mark byte
        /// </summary>
        public static readonly byte DataBorderControlByte = 3;

        public static byte[] CreateMessage(RawMessageFormat messageFormat, string stringData)
        {
            return CreateMessage(messageFormat, System.Text.Encoding.UTF8.GetBytes(stringData));
        }

        /// <summary>
        /// Message format: 5, 70 , 10 + (2 Bytes message format - short) + (4 Bytes data length - int) + 3 + (message bytes) + 3
        /// </summary>
        /// <param name="messageFormat"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] CreateMessage(RawMessageFormat messageFormat, byte[] data)
        {
            byte[] target = new byte[data.Length + 11];

            Array.Copy(StartMessageIdentifier, target, StartMessageIdentifier.Length);

            int currentIndex = StartMessageIdentifier.Length;

            // copy message type bytes
            short msgType = (short)messageFormat;
            byte[] msgTypeBytes = BitConverter.GetBytes(msgType);
            if (msgTypeBytes.Length != 2)
            {
                throw new InvalidCastException("Unexpected message type bytes: " + msgTypeBytes.Length);
            }

            Array.Copy(msgTypeBytes, 0, target, currentIndex, msgTypeBytes.Length);

            currentIndex += msgTypeBytes.Length;

            // copy messgae length bytes
            int dataLength = data.Length;
            byte[] msgLengthBytes = BitConverter.GetBytes(dataLength);

            if (msgLengthBytes.Length != 4)
            {
                throw new InvalidCastException("Unexpected message length bytes: " + msgLengthBytes.Length);
            }

            Array.Copy(msgLengthBytes, 0, target, currentIndex, msgLengthBytes.Length);

            currentIndex += msgLengthBytes.Length;

            // set 3 DataBorderControlByte separator
            target[currentIndex] = DataBorderControlByte;
            currentIndex++;

            // copy data
            Array.Copy(data, 0, target, currentIndex, data.Length);

            currentIndex += data.Length;

            // set 3 DataBorderControlByte end mark
            target[currentIndex] = DataBorderControlByte;

            return target;
        }


        /// <summary>
        /// Reads the raw message.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="length">The length.</param>
        /// <param name="unusedSharedMessage">The unused shared message.</param>
        /// <param name="pendingMessage">The pending message.</param>
        /// <returns></returns>
        public IRawMessage ReadRawMessage(byte[] source, ref int startIndex, int length, IRawMessage unusedSharedMessage, ref IRawMessage pendingMessage)
        {
            try
            {
                if (pendingMessage != null)
                {
                    if (pendingMessage.MessageFormat == RawMessageFormat.IncompleteControlDataSlice)
                    {
                        if (startIndex == 0)
                        {
                            // put separated control bytes in front
                            int startPartLength = pendingMessage.Length;
                            int targetLength = startPartLength + length;
                            byte[] targetData = new byte[targetLength];
                            pendingMessage.Data.CopyTo(targetData, 0);
                            Array.Copy(source, startIndex, targetData, startPartLength, length);

                            if (targetLength > StartMessageControlMinByteCount)
                            {
                                try
                                {
                                    pendingMessage = null;
                                    return ReadRawMessage(targetData, ref startIndex, targetLength, unusedSharedMessage, ref pendingMessage);
                                }
                                finally
                                {
                                    // reset start index to fit previous buffer
                                    startIndex -= startPartLength;
                                }
                            }
                        }
                        else
                        {
                            // not expected -> corrupt data
                            Logger.Error(string.Format("Invalid raw data received! Expected start index = 0; actual: {0}", startIndex));
                        }
                    }
                    else
                    {
                        // append message data
                        int expectedRestBytes = pendingMessage.Data.Length - pendingMessage.Length;
                        expectedRestBytes = Math.Min(expectedRestBytes, length);

                        Array.Copy(source, startIndex, pendingMessage.Data, pendingMessage.Length, expectedRestBytes);
                        pendingMessage.Length += expectedRestBytes;

                        startIndex += expectedRestBytes;

                        if (pendingMessage.Data.Length == pendingMessage.Length)
                        {
                            // message data completed
                            var msg = pendingMessage;
                            pendingMessage = null;

                            // check data border byte
                            if (source[startIndex] != DataBorderControlByte)
                            {
                                throw new InvalidOperationException("Message data end mark not found! Unexpected byte: " + source[startIndex]);
                            }

                            return msg;
                        }
                    }
                }
                else
                {
                    int oldStartIndex = startIndex;
                    int index = startIndex;
                    if (index < length
                        && (source[index] == StartMessageIdentifier1
                            || (index = Array.IndexOf(source, StartMessageIdentifier1, index)) >= 0))
                    {
                        if (index + StartMessageControlMinByteCount < length
                            && source[index + 1] == StartMessageIdentifier[1]
                            && source[index + 2] == StartMessageIdentifier[2])
                        {
                            // message start
                            index += 3;
                            short messageFormatShort = BitConverter.ToInt16(source, index);
                            RawMessageFormat msgFormat = (RawMessageFormat)messageFormatShort;

                            index += 2;

                            int dataLength = BitConverter.ToInt32(source, index);
                            byte[] data = new byte[dataLength];

                            index += 5;

                            int sourceDataLength = length - index;
                            sourceDataLength = Math.Min(sourceDataLength, dataLength);

                            Array.Copy(source, index, data, 0, sourceDataLength);

                            index += sourceDataLength;

                            startIndex = index;

                            if (dataLength > sourceDataLength)
                            {
                                // first message part read
                                pendingMessage = unusedSharedMessage;
                                pendingMessage.MessageFormat = msgFormat;
                                pendingMessage.Data = data;
                                pendingMessage.Length = sourceDataLength;
                            }
                            else
                            {
                                // complete message

                                // check data border byte
                                if (source[startIndex] != DataBorderControlByte)
                                {
                                    if (length == data.Length + StartMessageControlByteCount)
                                    {
                                        // end separator not transferred yet > create pending message
                                        pendingMessage = unusedSharedMessage;
                                        pendingMessage.MessageFormat = RawMessageFormat.IncompleteControlDataSlice;
                                        byte[] messageWithoutEndTag = new byte[length];
                                        Array.Copy(source, oldStartIndex, messageWithoutEndTag, 0, length);
                                        pendingMessage.Data = messageWithoutEndTag;
                                        pendingMessage.Length = length;
                                        return null;    // skip and wait for end tag
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Message data end mark not found! Unexpected byte: " + source[startIndex]);
                                    }
                                }
                                startIndex++;

                                pendingMessage = null;

                                var message = unusedSharedMessage;
                                // update message data
                                message.MessageFormat = msgFormat;
                                message.Data = data;
                                message.Length = dataLength;

                                return message;
                            }
                        }
                        else
                        {
                            var restByteCount = length - index;

                            if (restByteCount > 0)
                            {
                                // incomplete encapsulation control data slice
                                pendingMessage = unusedSharedMessage;
                                pendingMessage.MessageFormat = RawMessageFormat.IncompleteControlDataSlice;
                                byte[] endRawControlData = new byte[restByteCount];
                                Array.Copy(source, index, endRawControlData, 0, restByteCount);
                                pendingMessage.Data = endRawControlData;
                                pendingMessage.Length = restByteCount;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                pendingMessage = null;
            }

            return null;
        }

        #endregion

        // ----------------------------------------------------------------------------------------
        #endregion




    }

}
