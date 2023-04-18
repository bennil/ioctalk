using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Communication.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.Communication.WebSocketFraming
{
    public abstract class AbstractWebSocketBase : GenericCommunicationBaseService //, ICommunicationBaseServiceSupport, IDisposable
    {
        protected async ValueTask OnRawMessageReceived(RawMessageFormat rawMsgFormat, int sessionId, ReadOnlyMemory<byte> messagePayload)
        {
            try
            {
                if (serializer.MessageFormat == rawMsgFormat)
                {
                    ISession session;
                    if (!sessionDictionary.TryGetValue(sessionId, out session))
                    {
                        WaitForPendingSessionById(sessionId, out session);

                        if (session == null)
                        {
                            // session terminated -> ignore packets
                            if (logDataStream)
                            {
                                dataStreamLogger.LogStreamMessage(sessionId, true, DismissInvalidSessionMessageLogTag + Encoding.UTF8.GetString(messagePayload.ToArray()));
                            }
                            return;
                        }
                    }

                    // Convert to byte array because serializer & logger API does not yet implement ReadOnlyMemory<byte>
                    // todo: implement memory serializer API
                    var arrayPool = ArrayPool<byte>.Shared;
                    int msgLength = messagePayload.Length;
                    byte[] messagePayloadArray = arrayPool.Rent(msgLength);
                    messagePayload.CopyTo(messagePayloadArray);
                    IGenericMessage? message = null;
                    try
                    {
                        if (logDataStream)
                        {
                            dataStreamLogger.LogStreamMessage(sessionId, true, messagePayloadArray, msgLength, serializer.MessageFormat != RawMessageFormat.JSON);
                        }

                        message = serializer.DeserializeFromBytes(messagePayloadArray, msgLength, session, sessionId);
                    }
                    finally
                    {
                        // message object deserialized > return array to pool
                        arrayPool.Return(messagePayloadArray);
                    }

                    await ProcessReceivedMessage(session, message).ConfigureAwait(false);
                }
                else
                {
                    logger.Error($"Unexpected message format received: {rawMsgFormat}; Expected: {serializer.MessageFormat}; Message length: {messagePayload.Length}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }
        }
    }
}
