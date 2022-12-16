using Bond.IO;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Communication.NetTcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BSAG.IOCTalk.Communication.NetTcp.WireFraming
{
    public sealed class ShortWireFraming : AbstractWireFraming
    {
        public static readonly byte MessageTypeOffset = 5;

        private const int HeaderSize = 4;

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

        internal override void Init(IGenericCommunicationService parent)
        {
            this.messageFormat = parent.Serializer.MessageFormat;
            this.messageFormatByte = (byte)(MessageTypeOffset + (int)messageFormat);
        }


        public override bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messagePayload, out RawMessageFormat rawMessageFormat)
        {
            if (buffer.IsEmpty)
            {
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }

            if (buffer.FirstSpan.Length < HeaderSize)
            {
                // not enough data to read header
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }

            var firstMsgTypeByte = buffer.FirstSpan[0];

            if (firstMsgTypeByte == messageFormatByte)
            {
                // skip message type byte
                messagePayload = buffer.Slice(1);

                // read message length
                uint msgLength = IntegerHelper.DecodeUInt24(messagePayload.FirstSpan);
                messagePayload = messagePayload.Slice(3);

                if (msgLength > MaxMessageSize)
                {
                    Logger?.Error($"Max message size threshold of {MaxMessageSize} bytes exeeded! Discard message!!! Received message length: {msgLength}");
                    buffer = buffer.Slice(buffer.End);  // consume invalid data to clear buffer
                }
                else if (messagePayload.Length >= msgLength)
                {
                    messagePayload = messagePayload.Slice(messagePayload.Start, msgLength);
                    rawMessageFormat = messageFormat;


                    buffer = buffer.Slice(messagePayload.End);  // consume buffer data

                    return true;
                }
                //else
                //{
                //    // buffer does not contain complete message yet
                //    //todo: dead data parts check?
                //}
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

            writer.Advance(4);  // msg type + uint24
        }

        public override void CreateTransportMessageEnd(IBufferWriter<byte> writer, int payloadSize, Span<byte> startMessageRef)
        {
            startMessageRef = startMessageRef.Slice(1);   // length field start at index 1
            IntegerHelper.EncodeUInt24(startMessageRef, payloadSize);
        }


    }

}
