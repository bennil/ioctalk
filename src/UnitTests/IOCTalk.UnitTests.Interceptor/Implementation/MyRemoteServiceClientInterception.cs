using BSAG.IOCTalk.Common.Interface.Logging;
using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    internal class MyRemoteServiceClientInterception : IMyRemoteService
    {
        IMyRemoteService nestedService;
        ILogger log;

        public MyRemoteServiceClientInterception(IMyRemoteService nestedService, ILogger log)
        {
            this.nestedService = nestedService;
            this.log = log;
        }

        public int CallCounter { get; private set; }

        public string Foo(string bar)
        {
            CallCounter++;

            log.Info($"Client side MyRemoteServiceClientInterception.Foo({bar}) call");

            var result = nestedService.Foo(bar);

            log.Info($"Client side MyRemoteServiceClientInterception.Foo({bar}) result: {result}");

            return result;
        }
    }
}
