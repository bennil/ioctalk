using Bond.IO;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Communication.NetTcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BSAG.IOCTalk.Communication.NetTcp.WireFraming
{
    public sealed class LegacyWireFraming : AbstractWireFraming
    {
        private const int HeaderSize = 10;


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


        short rawMessageFormatShort;

        internal override void Init(IGenericCommunicationService parent)
        {
            this.rawMessageFormatShort = (short)parent.Serializer.MessageFormat;
        }


        public override bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messagePayload, out RawMessageFormat rawMessageFormat)
        {
            if (buffer.IsEmpty)
            {
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }
            else if (buffer.FirstSpan.Length < HeaderSize)
            {
                // not enough data to read header
                messagePayload = default;
                rawMessageFormat = default;
                return false;
            }
            else if (buffer.FirstSpan[0] == StartMessageIdentifier1)
            {
                // skip message prefix
                messagePayload = buffer.Slice(3);

                // read message type
                short messageFormatShort = BitConverter.ToInt16(messagePayload.FirstSpan);
                rawMessageFormat = (RawMessageFormat)messageFormatShort;

                messagePayload = messagePayload.Slice(2);   // set position end of raw message format


                // read message lenth
                int msgLength = BitConverter.ToInt32(messagePayload.FirstSpan);


                if (msgLength > MaxMessageSize)
                {
                    Logger?.Error($"Max message size threshold of {MaxMessageSize} bytes exeeded! Discard message!!! Received message length: {msgLength}");
                    buffer = buffer.Slice(buffer.End);  // consume invalid data to clear buffer
                }
                else if (messagePayload.Length >= msgLength)
                {
                    messagePayload = messagePayload.Slice(5);   // skip msg length + control byte
                    messagePayload = messagePayload.Slice(messagePayload.Start, msgLength);


                    var endMessagePos = buffer.GetPosition(messagePayload.Length + 11);      // (+ header/footer control bytes)
                    buffer = buffer.Slice(endMessagePos);  // consume buffer data 

                    return true;
                }
                else
                {
                    // buffer does not contain complete message yet
                    //todo: dead data parts check?
                }
            }
            else
            {
                string additionalInfo = string.Empty;
                //if ()
                Logger?.Error($"Unexpected raw data received! Expected first byte: {StartMessageIdentifier1}; Actual received: {buffer.FirstSpan[0]}{additionalInfo}; Expected Format: {GetType().Name}");
                buffer = buffer.Slice(buffer.End);  // consume invalid data to clear buffer
            }

            messagePayload = default;
            rawMessageFormat = default;
            return false;
        }


        public override void CreateTransportMessageStart(IBufferWriter<byte> writer)
        {
            writer.Write(StartMessageIdentifier.AsSpan());

            // two bytes message format
            if (BitConverter.TryWriteBytes(writer.GetSpan(2), this.rawMessageFormatShort) == false)
                throw new InvalidOperationException("Could not write message format span");

            // 2 msg type + 4 Bytes data length
            // write at the end
            writer.Advance(6);

            // write control byte
            var controlByteSpan = writer.GetSpan(1);
            controlByteSpan[0] = DataBorderControlByte;
            writer.Advance(1);
        }

        public override void CreateTransportMessageEnd(IBufferWriter<byte> writer, int payloadSize, Span<byte> startMemoryRef)
        {
            startMemoryRef = startMemoryRef.Slice(5);   // length field start at index 5
            if (BitConverter.TryWriteBytes(startMemoryRef, payloadSize) == false)
                throw new InvalidOperationException("Could not write start message length!");

            // write control byte
            var controlByteSpan = writer.GetSpan(1);
            controlByteSpan[0] = DataBorderControlByte;
            writer.Advance(1);
        }


    }

}
