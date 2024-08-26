using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    /// <summary>
    /// Simple service implementation only for interception unit test
    /// </summary>
    public class MyImportantServiceImplementation : IMyImportantService
    {
        public double Divide(int number1, int number2)
        {
            return (double)number1 / (double)number2;
        }

        public int Multiply(int number1, int number2)
        {
            return number1 * number2;
        }
    }
}
