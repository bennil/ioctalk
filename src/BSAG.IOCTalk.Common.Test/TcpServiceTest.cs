using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace BSAG.IOCTalk.Common.Test
{
    public class TcpServiceTest
    {
        [Fact]
        public void ReadReceivedMessageSimple()
        {
            // prepare test messate
            string payloadStr = "{\"TEST\":123456789}";
            byte[] msgBytes = TcpServiceCom.CreateMessage(Interface.Communication.Raw.RawMessageFormat.JSON, payloadStr);


            RawMessage sharedMsg = new RawMessage(Interface.Communication.Raw.RawMessageFormat.JSON, new byte[20],0, 0);
            IRawMessage pendingMsg = null;
            int startIndex = 0;
            TcpServiceCom serviceCommTest = new TcpServiceCom();
            IRawMessage resultMsg = serviceCommTest.ReadRawMessage(msgBytes, ref startIndex, msgBytes.Length, sharedMsg, ref pendingMsg);

            Assert.NotNull(resultMsg);
            string resultPayloadStr = Encoding.UTF8.GetString(resultMsg.Data, 0, resultMsg.Length);

            Assert.Equal(payloadStr, resultPayloadStr);
        }


        [Fact]
        public void ReadReceivedMessageOverlappingTcpFrameHandling1()
        {
            ProcessReceivedMessageOverlappingTcpFrameHandling(1);
            ProcessReceivedMessageOverlappingTcpFrameHandling(2);
            ProcessReceivedMessageOverlappingTcpFrameHandling(3);
            ProcessReceivedMessageOverlappingTcpFrameHandling(5);
            ProcessReceivedMessageOverlappingTcpFrameHandling(10);

        }

        private static void ProcessReceivedMessageOverlappingTcpFrameHandling(int restLength)
        {
            // prepare test messate
            string payloadStr = "{\"TEST\":123456789}";
            byte[] msgBytes = TcpServiceCom.CreateMessage(Interface.Communication.Raw.RawMessageFormat.JSON, payloadStr);

            // separate in parts
            int firstPartLength = msgBytes.Length - restLength;
            byte[] firstPart = new byte[firstPartLength];
            Array.Copy(msgBytes, 0, firstPart, 0, firstPartLength);

            int secondPartLength = msgBytes.Length - firstPartLength;
            byte[] secondPart = new byte[secondPartLength];
            Array.Copy(msgBytes, firstPartLength, secondPart, 0, secondPartLength);

            RawMessage sharedMsg = new RawMessage(Interface.Communication.Raw.RawMessageFormat.IncompleteControlDataSlice, null, 0, 0);
            IRawMessage pendingMsg = null;
            int startIndex = 0;
            TcpServiceCom serviceCommTest = new TcpServiceCom();
            IRawMessage resultMsg = serviceCommTest.ReadRawMessage(firstPart, ref startIndex, firstPart.Length, sharedMsg, ref pendingMsg);

            // 1. expect only pending 
            Assert.Null(resultMsg);
            Assert.NotNull(pendingMsg);

            // 2. read rest and expect result msg
            startIndex = 0; // reset read index
            resultMsg = serviceCommTest.ReadRawMessage(secondPart, ref startIndex, secondPart.Length, sharedMsg, ref pendingMsg);
            Assert.NotNull(resultMsg);

            string resultPayloadStr = Encoding.UTF8.GetString(resultMsg.Data, 0, resultMsg.Length);
            Assert.Equal(payloadStr, resultPayloadStr);
        }
    }
}
