using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class SpecialCasesService : ISpecialCasesService
    {
        public IEnumerable<int> GetEnumerableIntArrayValues()
        {
            return new int[] {1, 2, 3};
        }

        public IEnumerable<int> GetEnumerableIntListValues()
        {
            List<int> ints = new List<int>();
            ints.Add(1);
            ints.Add(2);
            ints.Add(3);
            return ints;
        }

    }
}
