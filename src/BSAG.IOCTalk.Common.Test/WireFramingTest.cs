using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Serialization.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace BSAG.IOCTalk.Common.Test
{
    /// <summary>
    /// Verifies the message parsing if the receive buffer is split over multiple segments (pipe segments of separate network reads)
    /// </summary>
    public class WireFramingTest
    {
        [Fact]
        public void TestShortWireFramingSplitAtEveryPosition()
        {
            VerifySplitAtEveryPosition(new ShortWireFraming());
        }

        [Fact]
        public void TestLegacyWireFramingSplitAtEveryPosition()
        {
            VerifySplitAtEveryPosition(new LegacyWireFraming());
        }

        [Fact]
        public void TestShortWireFramingIncompleteMessage()
        {
            VerifyIncompleteMessage(new ShortWireFraming());
        }

        [Fact]
        public void TestLegacyWireFramingIncompleteMessage()
        {
            VerifyIncompleteMessage(new LegacyWireFraming());
        }

        private static void VerifySplitAtEveryPosition(AbstractWireFraming wireFraming)
        {
            InitWireFraming(wireFraming);

            byte[] payload1 = Encoding.UTF8.GetBytes("{\"Type\":9,\"RequestId\":1,\"Name\":\"First\"}");
            byte[] payload2 = Encoding.UTF8.GetBytes("{\"Type\":10,\"RequestId\":2,\"Name\":\"Second\"}");
            byte[] data = CreateMessage(wireFraming, payload1).Concat(CreateMessage(wireFraming, payload2)).ToArray();

            for (int splitIndex = 1; splitIndex < data.Length; splitIndex++)
            {
                for (int secondSplitIndex = splitIndex + 1; secondSplitIndex <= data.Length; secondSplitIndex++)
                {
                    var buffer = CreateSegmentedSequence(data, splitIndex, secondSplitIndex);

                    Assert.True(wireFraming.TryReadMessage(ref buffer, out var messagePayload1, out var format1), $"First message not read; split: {splitIndex}/{secondSplitIndex}");
                    Assert.Equal(RawMessageFormat.JSON, format1);
                    Assert.Equal(payload1, messagePayload1.ToArray());

                    Assert.True(wireFraming.TryReadMessage(ref buffer, out var messagePayload2, out var format2), $"Second message not read; split: {splitIndex}/{secondSplitIndex}");
                    Assert.Equal(RawMessageFormat.JSON, format2);
                    Assert.Equal(payload2, messagePayload2.ToArray());

                    Assert.True(buffer.IsEmpty);
                    Assert.False(wireFraming.TryReadMessage(ref buffer, out _, out _));
                }
            }
        }

        private static void VerifyIncompleteMessage(AbstractWireFraming wireFraming)
        {
            InitWireFraming(wireFraming);

            byte[] data = CreateMessage(wireFraming, Encoding.UTF8.GetBytes("{\"Type\":9,\"RequestId\":1}"));

            for (int length = 1; length < data.Length; length++)
            {
                for (int splitIndex = 1; splitIndex < length; splitIndex++)
                {
                    var buffer = CreateSegmentedSequence(data.AsSpan(0, length).ToArray(), splitIndex, length);
                    var bufferBefore = buffer;

                    Assert.False(wireFraming.TryReadMessage(ref buffer, out _, out _), $"Incomplete message read; length: {length}; split: {splitIndex}");
                    Assert.Equal(bufferBefore.Length, buffer.Length);
                }
            }
        }

        private static void InitWireFraming(AbstractWireFraming wireFraming)
        {
            var parent = new TcpCommunicationController(wireFraming, new JsonMessageSerializer());

            // AbstractWireFraming.Init is internal
            typeof(AbstractWireFraming)
                .GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(wireFraming, new object[] { parent });
        }

        private static byte[] CreateMessage(AbstractWireFraming wireFraming, byte[] payload)
        {
            var writer = new ArrayBufferWriter<byte>();
            wireFraming.CreateTransportMessageStart(writer);
            int headerSize = writer.WrittenCount;

            writer.Write(payload);

            byte[] startMessageData = writer.WrittenSpan.Slice(0, headerSize).ToArray();
            wireFraming.CreateTransportMessageEnd(writer, payload.Length, startMessageData);

            byte[] message = writer.WrittenSpan.ToArray();
            startMessageData.CopyTo(message, 0);
            return message;
        }

        /// <summary>
        /// Creates a sequence with the segments [0, splitIndex), [splitIndex, secondSplitIndex), [secondSplitIndex, end)
        /// </summary>
        private static ReadOnlySequence<byte> CreateSegmentedSequence(byte[] data, int splitIndex, int secondSplitIndex)
        {
            var first = new TestSegment(data.AsMemory(0, splitIndex), 0);
            var last = first.Append(data.AsMemory(splitIndex, secondSplitIndex - splitIndex));

            if (secondSplitIndex < data.Length)
                last = last.Append(data.AsMemory(secondSplitIndex));

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        private sealed class TestSegment : ReadOnlySequenceSegment<byte>
        {
            public TestSegment(ReadOnlyMemory<byte> memory, long runningIndex)
            {
                Memory = memory;
                RunningIndex = runningIndex;
            }

            public TestSegment Append(ReadOnlyMemory<byte> memory)
            {
                var next = new TestSegment(memory, RunningIndex + Memory.Length);
                Next = next;
                return next;
            }
        }
    }
}
