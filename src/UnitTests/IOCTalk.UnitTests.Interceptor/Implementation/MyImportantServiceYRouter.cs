using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    public class MyImportantServiceYRouter : IMyImportantService
    {
        IMyImportantService service1, service2;

        volatile bool toggleSwitch;

        public MyImportantServiceYRouter(IMyImportantService[] services)
        {
            if (services.Length != 2)
                throw new InvalidOperationException($"Exactly two implementations are expectected! Received: {services.Length}; Check your registration ceremony code.");

            this.service1 = services[0];
            this.service2 = services[1];

            if (service1.GetType().Equals(service2.GetType()))
                throw new InvalidOperationException($"Y service router does not expect two services with the same type in this unit test context! Type: {service1.GetType()}");
        }


        public IMyImportantService Service1 => service1;

        public IMyImportantService Service2 => service2;


        public double Divide(int number1, int number2)
        {
            toggleSwitch = !toggleSwitch;

            if (toggleSwitch)
                return service1.Divide(number1, number2);
            else
                return service2.Divide(number1, number2);
        }

        public int Multiply(int number1, int number2)
        {
            toggleSwitch = !toggleSwitch;

            if (toggleSwitch)
                return service1.Multiply(number1, number2);
            else
                return service2.Multiply(number1, number2);
        }
    }
}
