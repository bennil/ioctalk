using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    public class MyRemoteServiceImplementation : IMyRemoteService
    {
        public MyRemoteServiceImplementation()
        {
        }

        public string Foo(string bar)
        {
            return $"Hi {bar}";
        }
    }
}
