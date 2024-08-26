using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Interface
{
    public interface IMyImportantService
    {
        int Multiply(int number1, int number2);

        double Divide(int number1, int number2);
    }
}
