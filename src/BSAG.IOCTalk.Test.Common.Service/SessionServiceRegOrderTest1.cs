using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class SessionServiceRegOrderTest1 : ISessionServiceRegOrderTest1
    {
        public SessionServiceRegOrderTest1(ISessionServiceRegOrderTest2Other otherRegistrationOrderService)
        {
            InstanceCount++;

            if (otherRegistrationOrderService is null)
                throw new NullReferenceException("Order registration order service is null!");
        }

        public static int InstanceCount { get; set; }

        public void TestIt()
        {
            throw new NotImplementedException();
        }
    }
}
