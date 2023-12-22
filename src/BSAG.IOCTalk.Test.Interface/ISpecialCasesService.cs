using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface ISpecialCasesService
    {
        IEnumerable<int> GetEnumerableIntListValues();

        IEnumerable<int> GetEnumerableIntArrayValues();
    }
}
