using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class SessionServiceRegOrderTest2 : ISessionServiceRegOrderTest2, ISessionServiceRegOrderTest2Other
    {
        public SessionServiceRegOrderTest2()
        {
            InstanceCount++;
        }

        public static int InstanceCount { get; set; }

        public void OtherCall()
        {
            throw new NotImplementedException();
        }

        public void TestIt2()
        {
            throw new NotImplementedException();
        }
    }
}
