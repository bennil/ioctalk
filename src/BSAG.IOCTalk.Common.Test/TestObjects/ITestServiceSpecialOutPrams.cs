using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Test.TestObjects
{
    public interface ITestServiceSpecialOutPrams
    {
        void GetData(out int? nullableInteger, out IList<string> listTest);

        int? NullableReturnTestMethod(int input);

        bool? NullableBoolReturnTestMethod(int input);
    }
}
