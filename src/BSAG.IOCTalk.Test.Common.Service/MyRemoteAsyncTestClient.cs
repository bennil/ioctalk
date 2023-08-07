using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyRemoteAsyncTestClient : IMyRemoteAsyncAwaitTestClient
    {
        public async Task<IMapTestMainInterface> GetObjectData()
        {
            return new MapTestMain() { MainProperty = "test" };
        }

        public Task<IMapTestMainInterface> GetObjectData2()
        {
            return Task.FromResult<IMapTestMainInterface>(new MapTestMain() { MainProperty = "test" });
        }

        public async Task<IDataTransferTest> GetObjectDataID10Async()
        {
            await Task.Delay(10);   // force async execution

            return new DTTest { ID = 10 };
        }
    }
}
