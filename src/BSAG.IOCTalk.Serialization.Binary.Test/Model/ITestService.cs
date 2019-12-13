using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary.Test.Model
{
    public interface ITestService
    {
        void CallTest(ITestItem testItem);

        ITestItem GetTestItem();
    }
}
