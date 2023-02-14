using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyRemoteAsyncTestService : IMyRemoteAsyncAwaitTestService
    {
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(1);

            return "Hello world";
        }

        public Task<int> GetDataAsync2(int expected)
        {
            return Task.FromResult(expected);
        }

        public async Task RunSomeWork()
        {
            await Task.Delay(1);

            RunSomeWorkCounter++;
        }

        public Task SimpleCall()
        {
            return Task.CompletedTask;
        }

        public Task<IDataTransferTest> ComplexRoundtrip(IDataTransferTest test)
        {
            return Task.FromResult<IDataTransferTest>(test);
        }

        public Task<IDataTransferTest[]> GetDataAsync3(int expected)
        {
            var responseArr = new IDataTransferTest[expected];

            for (int i = 0; i < expected; i++)
            {
                var item = new DTTest()
                {
                    ID = i,
                    Name = $"Testobject{i + 1}"
                };
                responseArr[i] = item;
            }

            return Task<IDataTransferTest[]>.FromResult(responseArr);
        }

        public static int RunSomeWorkCounter { get; set; }
    }
}
