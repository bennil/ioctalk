using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Logging.DataStream.Replay
{
    public class InvokeRequest
    {
        public ReplaySession Session { get; internal set; }

        public long RequestId { get; internal set; }

        public string TargetInterfaceName { get; internal set; }

        public string TargetMethodName { get; internal set; }

        public string MessageData { get; set; }

        //public string Payload { get; internal set; }

        //public DateTime OriginalRequestUtc { get; set; }
    }
}
