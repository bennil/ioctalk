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
using System.IO.Pipelines;
using System.Buffers;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;

namespace BSAG.IOCTalk.Communication.NetTcp
{
    public delegate ValueTask RawMessageReceiveHandler(RawMessageFormat rawMsgFormat, int sessionId, ReadOnlyMemory<byte> messagePayload);


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


        protected Socket socket;

        private event EventHandler<ConnectionStateChangedEventArgs> connectionClosed;
        private event EventHandler<ConnectionStateChangedEventArgs> connectionEstablished;
        private int maxReadBufferSize = 3276800;
        private int receiveBufferSize = 65536;
        private int sendBufferSize = 65536;

        private CancellationTokenSource cancelTokenSource;

        private AbstractWireFraming wireFraming;

        int segmentedPayloadMessageBytesCount = 0;
        ILogger logger;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractTcpCom constructors
        // ----------------------------------------------------------------------------------------
        // AbstractTcpCom constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <c>AbstractTcpCom</c>.
        /// </summary>
        public AbstractTcpCom(AbstractWireFraming wireFraming)
        {
            this.wireFraming = wireFraming;
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
        public EndPoint EndPoint { get; protected set; }

        /// <summary>
        /// Gets the end point info
        /// </summary>
        public abstract string EndPointInfo { get; }

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




        public RawMessageReceiveHandler RawMessageReceiveHandler { get; set; }


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
        public ILogger Logger
        {
            get { return logger; }
            set
            {
                logger = value;

                wireFraming.Logger = value;
            }
        }



        /// <summary>
        /// Gets the latest utc connect time
        /// </summary>
        public abstract DateTime? ConnectTimeUtc { get; }


        public Action<Socket> AdjustSocketHandler { get; set; }


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
        public virtual void Close(Client client, string source)
        {
            if (client != null
                && client.socket != null
                && client.AcquireIsSocketClosedExecuted())
            {
                cancelTokenSource.Cancel();

                client.socket.Close();

                OnConnectionClosed(client, source);
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

            SocketState state = new SocketState();

            state.Client = client;

            Task.Run(async () => { await this.OnReceiveDataAsync(state); });
            Task.Run(async () => { await this.ReadReceivePipeAsync(state); });

            return state;
        }


        /// <summary>
        /// Called when [receive data].
        /// </summary>
        /// <param name="socketStateObj">The socket state obj.</param>
        protected async ValueTask OnReceiveDataAsync(SocketState state)
        {
            Socket clientSocket = state.Client.socket;
            Stream clientStream = state.Client.stream;
            PipeWriter writer = state.ReceivePipe.Writer;

            var cancelToken = cancelTokenSource.Token;

            int currentMemoryRequest = 1024;

            try
            {
                while (clientSocket.Connected)
                {
                    Memory<byte> memory = writer.GetMemory(currentMemoryRequest);
                    int bytesRead = await clientStream.ReadAsync(memory, cancelToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(bytesRead);

                    if (currentMemoryRequest <= bytesRead)
                    {
                        // received data dif fill out buffer
                        // increase for next request
                        if (currentMemoryRequest < receiveBufferSize)
                            currentMemoryRequest = bytesRead + 12;
                    }

                    // Make the data available to the PipeReader.
                    FlushResult result = await writer.FlushAsync();

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }

                // By completing PipeWriter, tell the PipeReader that there's no more data coming.
                await writer.CompleteAsync();

                //if (!state.Client.socket.Connected)
                //{
                Close(state.Client, "OnReceiveDataAsync state");
                //    return;
                //}
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
                    Close(state.Client, nameof(ObjectDisposedException));
                }
                else
                {
                    Logger.Error(ioEx.ToString());

                    Close(state.Client, $"{nameof(IOException)} {ioEx.Message}");
                }
            }
            catch (SocketException socketEx)
            {
                CloseOrLogSocketException(state, socketEx);
            }
            catch (OperationCanceledException)
            {
                /* connection closed exception */
                Close(state.Client, nameof(OperationCanceledException));
            }
            catch (ObjectDisposedException)
            {
                /* connection closed exception */
                Close(state.Client, nameof(ObjectDisposedException));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                Close(state.Client, $"{ex.GetType().Name} {ex.Message}");
            }
        }



        async ValueTask ReadReceivePipeAsync(SocketState socketState)
        {
            var reader = socketState.ReceivePipe.Reader;
            Socket clientSocket = socketState.Client.socket;

            var cancelToken = cancelTokenSource.Token;
            var sessionId = socketState.Client.SessionId;

            try
            {

                while (clientSocket.Connected)
                {
                    ReadResult result = await reader.ReadAsync(cancelToken);
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    RawMessageFormat rawMessageFormat;

                    while (wireFraming.TryReadMessage(ref buffer, out ReadOnlySequence<byte> messagePayload, out rawMessageFormat))
                    {
                        ReadOnlyMemory<byte> messageMemory;
                        if (messagePayload.IsSingleSegment == false)
                        {
                            // copy segmented memory message payload to continuous ReadOnlyMemory<byte>
                            // todo: check if slow copy path can be replaced with iterating over memory sequence chunks
                            messageMemory = new ReadOnlyMemory<byte>(messagePayload.ToArray());
                            segmentedPayloadMessageBytesCount++;
                        }
                        else
                        {
                            messageMemory = messagePayload.First;
                        }

                        if (RawMessageReceiveHandler != null)
                            await RawMessageReceiveHandler(rawMessageFormat, sessionId, messageMemory);
                    }

                    // Tell the PipeReader how much of the buffer has been consumed.
                    reader.AdvanceTo(buffer.Start, buffer.End);

                    // Stop reading if there's no more data coming.
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Close(socketState.Client, nameof(OperationCanceledException));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
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
                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:
                case SocketError.NetworkReset:

                case (SocketError)100:      // Linux/Android: Network is down
                case (SocketError)101:      // Linux/Android: Network is unreachable
                case (SocketError)102:      // Linux/Android: Network dropped connection on reset
                case (SocketError)103:      // Linux/Android: Software caused connection abort
                case (SocketError)104:      // Linux/Android: Connection reset by peer
                case (SocketError)110:      // Linux/Android: Connection timed out

                    Close(state.Client, $"{nameof(SocketException)} ErrorCode: {errorCode}");
                    break;

                default:
                    Logger.Error(sockEx.ToString());
                    Close(state.Client, $"{nameof(SocketException)} ErrorCode: {errorCode}");
                    break;
            }
        }



        /// <summary>
        /// Sends the specified data bytes.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public abstract void Send(byte[] dataBytes, int receiverId);

        public abstract void Send(ReadOnlySpan<byte> dataBytes, int receiverId);


        /// <summary>
        /// Sends the specified data bytes async.
        /// </summary>
        /// <param name="dataBytes">The data bytes.</param>
        /// <param name="receiverId">The receiver socket id.</param>
        /// <returns></returns>
        public abstract ValueTask SendAsync(byte[] dataBytes, int receiverId);


        public abstract ValueTask SendAsync(ReadOnlyMemory<byte> dataBytes, int receiverId);


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

            if (AdjustSocketHandler != null)
                AdjustSocketHandler(socket);
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
        internal void OnConnectionClosed(Client client, string source)
        {
            Logger.Info($"Tcp client connection \"{client.RemoteEndPoint}\" closed - Source: {source}");

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
                                ThrowDataEndMarkException("Append", source, startIndex, msg.Length);
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

                            if (dataLength > sourceDataLength
                                || length == index)    // received byte length ends exactly at payload end (separator byte still expected)
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
                                    ThrowDataEndMarkException($"param length: {length}; data.Length: {data.Length}; source.Length: {source.Length}", source, startIndex, data.Length);
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

        private static void ThrowDataEndMarkException(string additionalInfo, byte[] source, int startIndex, int length)
        {
            throw new InvalidOperationException($@"Message data end mark not found! Unexpected byte: {source[startIndex]}; startIndex: {startIndex}; Length: {length}; AdditionalInfo: {additionalInfo}; 
Msg data hex: {BitConverter.ToString(source, 0, length)}

Msg data utf8: {Encoding.UTF8.GetString(source, 0, length)}");
        }

        #endregion

        // ----------------------------------------------------------------------------------------
        #endregion




    }

}
