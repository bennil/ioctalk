using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public interface ITestServiceInterface
    {
        bool StartService(int priority);
    }
}
