using IOCTalk.UnitTests.Interceptor.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.UnitTests.Interceptor.Implementation
{
    public class MyImportantServiceCache : IMyImportantService
    {
        Dictionary<int, int> multipyCache = new Dictionary<int, int>();
        IMyImportantService nestedService;

        public MyImportantServiceCache(IMyImportantService nestedService)
        {
            this.nestedService = nestedService;
        }

        public int CacheHitCount { get; set; } = 0;

        public IMyImportantService NestedService => nestedService;

        public double Divide(int number1, int number2)
        {
            return nestedService.Divide(number1, number2);  // no caching
        }

        public int Multiply(int number1, int number2)
        {
            int key = number1 * number2;

            int result;
            if (multipyCache.TryGetValue(key, out result))
            {
                CacheHitCount++;
                return result;
            }
            else
            {
                result = nestedService.Multiply(number1, number2);
                multipyCache[key] = result;
                return result;
            }
        }
    }
}
