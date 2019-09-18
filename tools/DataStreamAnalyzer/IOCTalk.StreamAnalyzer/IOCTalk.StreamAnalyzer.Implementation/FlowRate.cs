using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOCTalk.StreamAnalyzer.Implementation
{
    public class FlowRate
    {
        public FlowRate(int hour, int minute, int second)
        {
            this.Time = new TimeSpan(hour, minute, second);
        }

        public long StartLineNumber { get; set; }

        public TimeSpan Time { get; set; }

        public int TotalCallCount { get; set; }


        public int IncomingSyncCallCount { get; set; }

        public int OutgoingSyncCallCount { get; set; }



        public int IncomingAsyncCallCount { get; set; }


        public int OutgoingAsyncCallCount { get; set; }


        public int PayloadByteCount { get; set; }
    }
}
