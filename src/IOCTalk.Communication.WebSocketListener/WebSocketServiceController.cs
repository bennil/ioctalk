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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCTalk.Communication.WebSocketListener
{
    public delegate ValueTask RawMessageReceiveHandler(RawMessageFormat rawMsgFormat, int sessionId, ReadOnlyMemory<byte> messagePayload);


    public class WebSocketServiceController : AbstractWebSocketBase, ICommunicationBaseServiceSupport, IDisposable
    {
        HttpListener? httpListener;
        ConcurrentDictionary<int, Client> clients = new ConcurrentDictionary<int, Client>();
        CancellationTokenSource? cts;
        AbstractWireFraming wireFraming;
        ObjectPool<OutputBuffer> outputBufferObjectPool;

        public WebSocketServiceController(IGenericMessageSerializer messageSerializer)
        {
            this.serializer = messageSerializer;
        }

        public WebSocketServiceController(IGenericMessageSerializer messageSerializer, ILogger logger)
            : this(messageSerializer)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Initial create message buffer size
        /// </summary>
        public int CreateMessageBufferInitialSize { get; set; } = 256;


        public void InitWebSocketListener(string listenUri, params string[] additionalUriAddresses)
        {
            if (httpListener != null)
                throw new InvalidOperationException("Websocket listener already initalized");


            httpListener = new HttpListener();
            //httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Prefixes.Add(listenUri);
            if (additionalUriAddresses != null)
                foreach (var uri in additionalUriAddresses)
                    httpListener.Prefixes.Add(uri);

            cts = new CancellationTokenSource();

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
                dataStreamLogger.Init(this, $"{containerName}-WebsocketListener-{RemoveInvalidFilenameCharacters(listenUri)}", null);
            }

            logger.Info($"Websocket listener start with bindings: {string.Join(", ", httpListener.Prefixes)}");

            httpListener.Start();

            Task.Run(ListenForClients);
        }

        public bool IsAsyncVoidSendCurrentlyPossible(ISession session)
        {
            return true;
        }

        public void SendMessage(IGenericMessage message, int receiverSessionId, object context)
        {
            // workaround because no sync support in websocket api
            //Task.Run(() => SendMessageAsync(message, receiverSessionId, context)).GetAwaiter().GetResult();
            SendMessageAsync(message, receiverSessionId, context).GetAwaiter().GetResult();
            //SendMessageAsync(message, receiverSessionId, context);
        }

        public async ValueTask SendMessageAsync(IGenericMessage message, int receiverSessionId, object context)
        {
            if (clients.TryGetValue(receiverSessionId, out Client client))
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

                    await client.SendAsync(writer.WrittenMemory);
                }
                finally
                {
                    writer.Reset();
                    outputBufferObjectPool.Return(writer);
                }
            }
        }


        async ValueTask ListenForClients()
        {
            try
            {
                var cancellationToken = cts.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext? context = await httpListener.GetContextAsync().WithCancellationToken(cancellationToken);
                    if (context is null)
                        return;

                    if (!context.Request.IsWebSocketRequest)
                        context.Response.Abort();
                    else
                    {
                        HttpListenerWebSocketContext? webSocketContext =
                            await context.AcceptWebSocketAsync(subProtocol: null).WithCancellationToken(cancellationToken);

                        if (webSocketContext is null)
                            return;

                        WebSocket webSocket = webSocketContext.WebSocket;
                        var client = new Client(this, webSocket, OnRawMessageReceived, wireFraming);
                        clients[client.SessionId] = client;

                        CreateSession(client.SessionId, $"Websocket {client.SessionId} - {webSocketContext.RequestUri}", () => webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "ForceClose", CancellationToken.None), webSocket);

                        client.StarReceivingData();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
            finally
            {
                logger.Info("Websocket listener stopped");
            }
        }

        public void Dispose()
        {
            Shutdown();
        }

        public override void Shutdown()
        {
            if (cts != null)
                cts.Cancel();

            if (httpListener != null)
                httpListener.Stop();

            if (clients != null)
            {
                foreach (var client in clients)
                {
                    client.Value.Close("listener shutdown");
                }
                clients.Clear();
            }

            this.outputBufferObjectPool?.Clear();

            base.Shutdown();
        }

        internal void OnClosed(int sessionId, string source)
        {
            clients.TryRemove(sessionId, out var _);

            ProcessSessionTerminated(sessionId, source);
        }

    }
}
