using BSAG.IOCTalk.Common.Interface.Logging;
using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    internal class MyRemoteServiceInterceptionImplementation : IMyRemoteService
    {
        IMyRemoteService nestedSessionService;
        ILogger log;

        public MyRemoteServiceInterceptionImplementation(IMyRemoteService nestedSessionService, ILogger log)
        {
            this.nestedSessionService = nestedSessionService;
            this.log = log;
        }

        public int ServiceCallCount { get; set; }

        public string Foo(string bar)
        {
            ServiceCallCount++;

            log.Info($"Remote side MyRemoteServiceInterceptionImplementation.Foo({bar}) call");

            var result = nestedSessionService.Foo(bar);

            log.Info($"Remote side MyRemoteServiceInterceptionImplementation.Foo({bar}) result: {result}");

            return result;
        }
    }
}
