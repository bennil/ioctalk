using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    public class MyImportantServiceLogInterception : IMyImportantService
    {
        IMyImportantService nestedService;
        ITestOutputHelper log;

        public MyImportantServiceLogInterception(IMyImportantService nestedService, ITestOutputHelper logger)
        {
            this.nestedService = nestedService;
            this.log = logger;
        }

        public IMyImportantService NestedService => nestedService;

        public double Divide(int number1, int number2)
        {
            log.WriteLine($"Call {nameof(IMyImportantService)}.{nameof(Divide)}(number1: {number1}, number2: {number2})");

            var result = nestedService.Divide(number1, number2);

            log.WriteLine($"{nameof(Divide)} result = {result}");

            return result;
        }

        public int Multiply(int number1, int number2)
        {
            log.WriteLine($"Call {nameof(IMyImportantService)}.{nameof(Multiply)}(number1: {number1}, number2: {number2})");

            var result = nestedService.Multiply(number1, number2);

            log.WriteLine($"{nameof(Multiply)} result = {result}");

            return result;
        }
    }
}
