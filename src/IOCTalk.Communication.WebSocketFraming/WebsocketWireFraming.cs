using Bond.IO;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace IOCTalk.Communication.WebSocketFraming
{
    /// <summary>
    /// Websockets have their own message boundary protocol. 
    /// </summary>
    public sealed class WebsocketWireFraming : AbstractWireFraming
    {
        public static readonly byte MessageTypeOffset = 5;

        private const int HeaderSize = 1;

        static readonly byte MessageTypeJson = (byte)(MessageTypeOffset + (byte)RawMessageFormat.JSON);
        static readonly byte MessageTypeBinary = (byte)(MessageTypeOffset + (byte)RawMessageFormat.Binary);

        byte messageFormatByte;
        RawMessageFormat messageFormat;


        public override int MaxMessageSize
        {
            get => base.MaxMessageSize;
            set
            {
                if (value > IntegerHelper.MaxUInt24)
                    throw new ArgumentOutOfRangeException($"{GetType().Name} only supports a max message length of {IntegerHelper.MaxUInt24}!");

                base.MaxMessageSize = value;
            }
        }

        public override void Init(IGenericCommunicationService parent)
        {
            this.messageFormat = parent.Serializer.MessageFormat;
            this.messageFormatByte = (byte)(MessageTypeOffset + (int)messageFormat);
        }


        public override bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messagePayload, out RawMessageFormat rawMessageFormat)
        {
#if DEBUG
            Logger.Debug("WebsocketWireFraming.TryReadMessage");
#endif
            if (buffer.IsEmpty)
            {
#if DEBUG
                Logger.Debug("WebsocketWireFraming.TryReadMessage: buffer is empty");
#endif
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }

            if (buffer.FirstSpan.Length < HeaderSize)
            {
                // not enough data to read header
#if DEBUG
                Logger.Debug($"WebsocketWireFraming.TryReadMessage: header size to small: {buffer.FirstSpan.Length} ");
#endif
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }

            var firstMsgTypeByte = buffer.FirstSpan[0];

            if (firstMsgTypeByte == messageFormatByte)
            {
                // skip message type byte
                messagePayload = buffer.Slice(1);

                
                if (messagePayload.Length > MaxMessageSize)
                {
                    Logger?.Error($"Max message size threshold of {MaxMessageSize} bytes exeeded! Discard message!!! Received message length: {messagePayload.Length}");
                    buffer = buffer.Slice(buffer.End);  // consume invalid data to clear buffer
                }
                else
                {
                    rawMessageFormat = messageFormat;
#if DEBUG
                    Logger.Debug("WebsocketWireFraming.TryReadMessage complete");
#endif
                    buffer = buffer.Slice(messagePayload.End);  // consume buffer data
                    return true;
                }
            }
            else
            {
                string additionalInfo = string.Empty;
                if (firstMsgTypeByte == MessageTypeJson)
                {
                    additionalInfo = $" Looks like the remote host uses JSON format and own serializer {messageFormat}";
                }
                else if (firstMsgTypeByte == MessageTypeBinary)
                {
                    additionalInfo = $" Looks like the remote host uses Binary format and own serializer {messageFormat}";
                }
                Logger?.Error($"Unexpected raw data received! Expected first byte: {messageFormatByte}; Actual received: {buffer.FirstSpan[0]}{additionalInfo}; Expected Format: {GetType().Name}");
                buffer = buffer.Slice(buffer.End);  // consume invalid data to clear buffer
            }

            messagePayload = default;
            rawMessageFormat = default;
            return false;
        }

        public override void CreateTransportMessageStart(IBufferWriter<byte> writer)
        {
            var startMsgType = writer.GetSpan(1);
            startMsgType[0] = messageFormatByte;

            writer.Advance(1);  // msg type
        }

        public override void CreateTransportMessageEnd(IBufferWriter<byte> writer, int payloadSize, Span<byte> startMessageRef)
        {
            // no message end data
        }
    }

}
