using Bond.IO.Safe;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Communication.Common;
using IOCTalk.Communication.WebSocketFraming;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCTalk.Communication.WebSocketClient
{
    public class WebSocketClientController : AbstractWebSocketBase, ICommunicationBaseServiceSupport, IDisposable
    {
        string connectUrl;
        CancellationTokenSource? cts;
        int clientAutoReconnectLock = 0;
        int clientConnectCount = 0;
        int receiveBufferSize = 65536;
        ClientWebSocket? currentSocket;
        Pipe receivePipe;
        AbstractWireFraming wireFraming;
        ObjectPool<OutputBuffer> outputBufferObjectPool;
        int isSocketClosedExecuted = 0;
        bool isBrowserRuntime = false;

        public WebSocketClientController(IGenericMessageSerializer messageSerializer)
        {
            this.serializer = messageSerializer;
        }


        /// <summary>
        /// Initial create message buffer size
        /// </summary>
        public int CreateMessageBufferInitialSize { get; set; } = 16;


        public void InitWebSocketClient(string connectUrl)
        {
            if (this.wireFraming != null)
                throw new InvalidOperationException("Websocket client already initalized");

            this.connectUrl = connectUrl;
            this.isBrowserRuntime = RuntimeInformation.OSDescription.Contains("Browser", StringComparison.OrdinalIgnoreCase);

            // hardcoded in websocket context
            wireFraming = new WebsocketWireFraming();
            wireFraming.Logger = logger;
            wireFraming.Init(this);

            outputBufferObjectPool = new ObjectPool<OutputBuffer>(() => new OutputBuffer(CreateMessageBufferInitialSize));

            if (LogDataStream)
            {
                string containerName = string.Empty;
                if (containerHost is ITalkContainer cont
                    && cont.Name != null)
                {
                    containerName = cont.Name + "_";
                }
                dataStreamLogger.Init(this, $"{containerName}-WebsocketClient-{RemoveInvalidFilenameCharacters(connectUrl)}", null);
            }

            Task.Run(StartAutoReconnectAsync);
        }

        /// <summary>
        /// Gets or sets the client reconnect interval
        /// </summary>
        public TimeSpan ClientReconnectInterval { get; set; } = TimeSpan.FromSeconds(1);

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

        async void StartAutoReconnectAsync()
        {
            if (Interlocked.Exchange(ref clientAutoReconnectLock, 1) == 0)    // only start auto reconnect task once
            {
                try
                {
                    await Task.Delay(100);

                    if (!isActive)
                    {
                        if (Logger != null)
                            Logger.Info($"No reconnect to {connectUrl} because of shutdown");

                        return;
                    }

                    //if (this.communication.ConnectTimeUtc.HasValue)
                    //{
                    //    // only if established connection is immediately reset by the remote host
                    //    // sleep to prevent fast endless reconnect loop
                    //    var lastConnectDiff = DateTime.UtcNow - this.communication.ConnectTimeUtc.Value;

                    //    if (lastConnectDiff < ClientReconnectInterval)
                    //    {
                    //        await Task.Delay(ClientReconnectInterval);
                    //    }
                    //}

                    if (Logger != null)
                        Logger.Info($"Connect to {connectUrl} (IsBrowser: {isBrowserRuntime})...");

                    clientConnectCount++;

                    while (await WebsocketConnect() == false)
                    {
                        if (Logger != null)
                            Logger.Warn($"Connection refused {connectUrl}!");

                        await Task.Delay(ClientReconnectInterval);

                        clientConnectCount++;

                        //RotateFallbackClientTargets();
                    }

                    clientConnectCount = 0;
                }
                catch (Exception ex)
                {
                    if (Logger != null)
                        Logger.Error(ex.ToString());

                    await Task.Delay(1000);    // pause between reconnect
                }
                finally
                {
                    Interlocked.Exchange(ref clientAutoReconnectLock, 0);

                    //if (communication is TcpClientCom)
                    //{
                    //    TcpClientCom client = (TcpClientCom)communication;

                    //    if (!client.IsConnected && isActive)
                    //    {
                    //        if (ClientReconnectFailed != null)
                    //            ClientReconnectFailed(this, EventArgs.Empty);

                    //        if (logger != null)
                    //            logger.Debug("Restart reconnect processing");

                    //        _ = Task.Run(StartAutoReconnectAsync);
                    //    }
                    //}
                    //else
                    //{
                    //    if (logger != null)
                    //        logger.Debug("Reconnect processing exit");
                    //}
                }
            }
        }

        async ValueTask<bool> WebsocketConnect()
        {
            if (cts != null)
                cts.Cancel();

            this.cts = new CancellationTokenSource();
            var socket = new ClientWebSocket();
            try
            {

                var sessionId = GetNewConnectionSessionId();

                await socket.ConnectAsync(new Uri(connectUrl), cts.Token);

                currentSocket = socket;
                isSocketClosedExecuted = 0;

                CreateSession(sessionId, $"Websocket client {sessionId} - {socket}", () =>
                {
                    try
                    {
                        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting)
                            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "ForceClose", CancellationToken.None);
                    }
                    catch
                    {
                        // ignore close exceptions
                    }
                }
                , socket);

                StarReceivingData(sessionId);

                return true;

            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (WebSocketException webSocketEx)
            {
                if (webSocketEx.Message != null
                    && webSocketEx.Message.Contains("Unable to connect"))
                {
                    return false;
                }
                else
                {
                    logger.Error(webSocketEx.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                return false;
            }

        }

        void StarReceivingData(int sessionId)
        {
            receivePipe = new Pipe();

            Task.Run(async () => { await this.OnReceiveDataAsync(sessionId); });
            Task.Run(async () => { await this.ReadReceivePipeAsync(sessionId); });
        }

        public void SendMessage(IGenericMessage message, int receiverSessionId, object context)
        {
            if (isBrowserRuntime == true)
            {
                string errorMsg = $"Unable to call the remote method \"{message.Target} {message.Name}\" without async because .NET WebAssembly does not support multiple threads yet!";
                logger.Error(errorMsg);
                throw new NotSupportedException(errorMsg);
            }
            SendMessageAsync(message, receiverSessionId, context).ConfigureAwait(false).GetAwaiter().GetResult();
#if DEBUG
            logger.Debug("Send sync/async complete");
#endif
        }

        public async ValueTask SendMessageAsync(IGenericMessage message, int receiverSessionId, object context)
        {
            if (currentSocket != null)
            {
                OutputBuffer writer = outputBufferObjectPool.Get();
                try
                {
                    // write header
                    wireFraming.CreateTransportMessageStart(writer);
                    int headerSize = writer.WrittenCount;

                    // serialize message payload
                    serializer.Serialize(writer, message, context, receiverSessionId);
                    int payloadSize = writer.WrittenCount - headerSize;

                    // write footer / header length
                    var startMessageData = writer.DataBuffer.AsMemory(0, headerSize);

                    wireFraming.CreateTransportMessageEnd(writer, payloadSize, startMessageData.Span);

                    if (logDataStream)
                    {
                        dataStreamLogger.LogStreamMessage(receiverSessionId, false, writer.DataBuffer, writer.WrittenCount, serializer.MessageFormat != RawMessageFormat.JSON);
                    }

                    await Send(writer.WrittenMemory);
                }
                finally
                {
                    writer.Reset();
                    outputBufferObjectPool.Return(writer);
                }
            }
        }

        async ValueTask Send(ReadOnlyMemory<byte> data)
        {
            if (currentSocket != null)
            {
#if DEBUG
                logger.Debug("Websocket send...");
#endif
                await currentSocket.SendAsync(data, WebSocketMessageType.Binary, true, cts.Token);
#if DEBUG
                logger.Debug("Websocket send complete");
#endif
            }
            else
                throw new OperationCanceledException();

        }


        /// <summary>
        /// Called when [receive data].
        /// </summary>
        /// <param name="socketStateObj">The socket state obj.</param>
        protected async ValueTask OnReceiveDataAsync(int sessionId)
        {
            PipeWriter writer = receivePipe.Writer;


            int currentMemoryRequest = 512;

            try
            {

                while (currentSocket != null
                    && currentSocket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    Memory<byte> memory = writer.GetMemory(currentMemoryRequest);

                    ValueWebSocketReceiveResult receiveResult =
                        await currentSocket.ReceiveAsync(memory, cts.Token);
                    if (receiveResult.Count == 0)
                        break;

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
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
#if DEBUG
                        logger.Debug($"Rcv flush because EndOfMessage");
#endif
                        FlushResult result = await writer.FlushAsync();

                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                }

                // By completing PipeWriter, tell the PipeReader that there's no more data coming.
                await writer.CompleteAsync();

                OnClosed(sessionId, "OnReceiveDataAsync state");
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
                OnClosed(sessionId, nameof(OperationCanceledException));
            }
            catch (ObjectDisposedException)
            {
                /* connection closed exception */
                OnClosed(sessionId, nameof(ObjectDisposedException));
            }
            catch (WebSocketException webSocketEx)
            {
                OnClosed(sessionId, nameof(WebSocketException));
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());

                OnClosed(sessionId, $"{ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                logger.Debug("Websocket reader exit");
            }
        }


        async ValueTask ReadReceivePipeAsync(int sessionId)
        {
            var reader = receivePipe.Reader;


            try
            {
                bool readMsg;
                while (cts.Token.IsCancellationRequested == false)
                {
#if DEBUG
                    logger.Debug($"ReadAsync sessionID: {sessionId}..");
#endif
                    ReadResult result = await reader.ReadAsync(cts.Token);
#if DEBUG
                    logger.Debug($"ReadReceivePipeAsync read buffer length: {result.Buffer.Length}");
#endif
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    RawMessageFormat rawMessageFormat;

                    while (readMsg = wireFraming.TryReadMessage(ref buffer, out ReadOnlySequence<byte> messagePayload, out rawMessageFormat))
                    {
#if DEBUG
                        logger.Debug($"ReadMessage: {rawMessageFormat}; {sessionId}");
#endif
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

                        await OnRawMessageReceived(rawMessageFormat, sessionId, messageMemory);
                    }

#if DEBUG
                    if (readMsg == false)
                        logger.Debug("No more message in buffer...");
#endif

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
                OnClosed(sessionId, nameof(OperationCanceledException));
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }


        public void Dispose()
        {
            if (cts != null)
                cts.Cancel();
        }

        /// <summary>
        /// Acquires the socket close processing lock
        /// </summary>
        bool AcquireIsSocketClosedExecuted()
        {
            return Interlocked.Exchange(ref isSocketClosedExecuted, 1) == 0;
        }

        internal void OnClosed(int sessionId, string source)
        {
            if (AcquireIsSocketClosedExecuted())    // execute only once
            {
                if (currentSocket != null)
                {
                    currentSocket.Dispose();
                    currentSocket = null;
                }

                ProcessSessionTerminated(sessionId, source);


                Task.Run(StartAutoReconnectAsync);
            }
        }

        public bool IsAsyncVoidSendCurrentlyPossible(ISession session)
        {
            return true;
        }

        /// <summary>
        /// ThreadPool.RegisterWaitForSingleObject is not supported in browser wasm
        /// todo: remove override if supported
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout"></param>
        /// <param name="invokeState"></param>
        /// <param name="sessionId"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        protected override async Task WaitHandleTaskAsyncResponseHelper(WaitHandle handle, TimeSpan timeout, IInvokeState invokeState, int sessionId, long requestId)
        {
            if (isBrowserRuntime)
            {
                // Use polling workaround because brwoser does not support ThreadPool.RegisterWaitForSingleObject and only one thread (otherwise deadlock if simple waithandle.wait)
                var session = sessionDictionary[sessionId];

                if (session.PendingRequests.ContainsKey(requestId) == true)
                {
                    Logger.Debug("Wait for browser workaround completion...");
                    await Task.Delay(5);

                    while (session.PendingRequests.ContainsKey(requestId) == true)
                    {
                        await Task.Delay(50);
                    }

                    Logger.Debug("Polling wait browser workaround completed");
                }
            }
            else
            {
                await base.WaitHandleTaskAsyncResponseHelper(handle, timeout, invokeState, sessionId, requestId);
            }
        }
    }
}
