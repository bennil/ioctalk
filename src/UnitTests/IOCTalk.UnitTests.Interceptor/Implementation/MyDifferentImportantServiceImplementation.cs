using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    /// <summary>
    /// Simple service implementation only for interception unit test
    /// Different implementation to showcase Y routing
    /// </summary>
    public class MyDifferentImportantServiceImplementation : IMyImportantService
    {
        ITestOutputHelper log;

        public MyDifferentImportantServiceImplementation(ITestOutputHelper logger)
        {
            this.log = logger;
        }

        public int CallCount { get; private set; }

        public double Divide(int number1, int number2)
        {
            CallCount++;

            return (double)number1 / (double)number2;
        }


        public int Multiply(int number1, int number2)
        {
            CallCount++;

            return number1 * number2;
        }
    }
}
