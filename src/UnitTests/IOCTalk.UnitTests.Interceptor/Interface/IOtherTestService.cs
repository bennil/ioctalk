using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Interface
{
    public interface IOtherTestService
    {
        IMyImportantService InjectedService { get; }
    }
}
