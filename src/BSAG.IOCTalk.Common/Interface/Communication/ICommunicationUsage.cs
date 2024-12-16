using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    public interface ICommunicationUsage
    {
        long ReceivedMessageCount { get; }

        long SentMessageCount { get; }


        long ReceivedByteCount { get; }

        long SentByteCount { get; }
    }
}
