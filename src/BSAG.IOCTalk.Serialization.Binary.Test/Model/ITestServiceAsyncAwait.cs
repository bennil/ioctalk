using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.Test.Model
{
    public interface ITestServiceAsyncAwait
    {
        Task CallTest(ITestItem testItem);

        Task<ITestItem> GetTestItem();
    }
}
