using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Communication.Common;
using IOCTalk.Communication.WebSocketFraming;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCTalk.Communication.WebSocketListener
{
    internal class Client : IDisposable
    {
        WebSocket webSocket;
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        RawMessageReceiveHandler rawMessageReceiveHandler;

        int receiveBufferSize = 65536;

        int isSocketClosedExecuted = 0;

        WebSocketServiceController parentController;
        ILogger log;
        AbstractWireFraming wireFraming;

        public Client(WebSocketServiceController parent, WebSocket webSocket, RawMessageReceiveHandler rawMessageReceiveHandler, AbstractWireFraming wireFraming)
        {
            SessionId = GenericCommunicationBaseService.GetNewConnectionSessionId();

            this.parentController = parent;
            this.log = parent.Logger;
            this.webSocket = webSocket;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = cancellationTokenSource.Token;

            this.wireFraming = wireFraming;

            this.ReceivePipe = new Pipe();
            this.rawMessageReceiveHandler = rawMessageReceiveHandler;
        }

        public int SessionId { get; private set; }

        public Pipe ReceivePipe { get; private set; }

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

        public CancellationTokenSource CancellationTokenSource => cancellationTokenSource;

        public async ValueTask SendAsync(ReadOnlyMemory<byte> dataBytes)
        {
            if (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                // todo: redirect websocket connection exceptions to OperationCanceledException
                await webSocket.SendAsync(
                                dataBytes,
                                WebSocketMessageType.Binary,
                                endOfMessage: true,
                                cancellationToken);
            }
            else
                throw new OperationCanceledException("Websocket client connction lost");
        }


        public void StarReceivingData()
        {
            Task.Run(async () => { await this.OnReceiveDataAsync(); });
            Task.Run(async () => { await this.ReadReceivePipeAsync(); });
        }

        /// <summary>
        /// Acquires the socket close processing lock
        /// </summary>
        internal bool AcquireIsSocketClosedExecuted()
        {
            return Interlocked.Exchange(ref isSocketClosedExecuted, 1) == 0;
        }


        /// <summary>
        /// Called when [receive data].
        /// </summary>
        /// <param name="socketStateObj">The socket state obj.</param>
        protected async ValueTask OnReceiveDataAsync()
        {
            PipeWriter writer = ReceivePipe.Writer;


            int currentMemoryRequest = 1024;

            try
            {

                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    Memory<byte> memory = writer.GetMemory(currentMemoryRequest);

                    ValueWebSocketReceiveResult receiveResult =
                        await webSocket.ReceiveAsync(memory, cancellationToken);
                    if (receiveResult.Count == 0)
                        break;

                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(receiveResult.Count);


                    if (currentMemoryRequest <= receiveResult.Count)
                    {
                        // received data diff fill out buffer
                        // increase for next request
                        if (currentMemoryRequest < receiveBufferSize)
                            currentMemoryRequest = receiveResult.Count + 12;
                    }


                    if (receiveResult.EndOfMessage)
                    {
                        // Make the data available to the PipeReader.
                        FlushResult result = await writer.FlushAsync();

                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                }

                // By completing PipeWriter, tell the PipeReader that there's no more data coming.
                await writer.CompleteAsync();

                Close("OnReceiveDataAsync state");
            }
            //catch (IOException ioEx)
            //{
            //    if (ioEx.InnerException is SocketException)
            //    {
            //        SocketException sockEx = (SocketException)ioEx.InnerException;

            //        CloseOrLogSocketException(state, sockEx);
            //    }
            //    else if (ioEx.InnerException is ObjectDisposedException)
            //    {
            //        Close(state.Client, nameof(ObjectDisposedException));
            //    }
            //    else
            //    {
            //        Logger.Error(ioEx.ToString());

            //        Close(state.Client, $"{nameof(IOException)} {ioEx.Message}");
            //    }
            //}
            //catch (SocketException socketEx)
            //{
            //    CloseOrLogSocketException(state, socketEx);
            //}
            catch (OperationCanceledException)
            {
                /* connection closed exception */
                Close(nameof(OperationCanceledException));
            }
            catch (ObjectDisposedException)
            {
                /* connection closed exception */
                Close(nameof(ObjectDisposedException));
            }
            catch (WebSocketException webSocketEx)
            {
                Close(nameof(WebSocketException));
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());

                Close($"{ex.GetType().Name} {ex.Message}");
            }
        }



        async ValueTask ReadReceivePipeAsync()
        {
            var reader = ReceivePipe.Reader;


            try
            {

                while (cancellationToken.IsCancellationRequested == false)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken);
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
                            //segmentedPayloadMessageBytesCount++;
                        }
                        else
                        {
                            messageMemory = messagePayload.First;
                        }

                        await rawMessageReceiveHandler(rawMessageFormat, SessionId, messageMemory);
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
                Close(nameof(OperationCanceledException));
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }


        public virtual void Close(string source)
        {
            if (webSocket != null
                && AcquireIsSocketClosedExecuted())
            {
                Dispose();

                // caused deadlocks if already closed
                //if (webSocket.State == WebSocketState.Open)
                //    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close from " + source, cancellationToken);


                //OnConnectionClosed(client, source);
                parentController.OnClosed(SessionId, source);
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
