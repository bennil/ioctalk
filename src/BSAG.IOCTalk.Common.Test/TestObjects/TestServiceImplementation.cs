using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public class TestServiceImplementation : ITestServiceInterface
    {
        /// <summary>
        /// Starts the service. (explicit interface implementation)
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <returns></returns>
        bool ITestServiceInterface.StartService(int priority)
        {
            return true;
        }
    }
}
