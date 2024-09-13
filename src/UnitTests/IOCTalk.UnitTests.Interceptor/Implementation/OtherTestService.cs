using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    internal class OtherTestService : IOtherTestService
    {
        IMyImportantService injectedService;

        public OtherTestService(IMyImportantService injectedService)
        {
            this.injectedService = injectedService;
        }

        public IMyImportantService InjectedService => injectedService;
    }
}
