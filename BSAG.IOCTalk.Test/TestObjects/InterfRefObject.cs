using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Test.Common;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public class InterfRefObject
    {
        public ITestInterfaceBase BaseObject { get; set; }

        public IDeepTestInterface1 BaseObjectInstance { get; set; }

        public IPerformanceData PerfData { get; set; }
    }
}
