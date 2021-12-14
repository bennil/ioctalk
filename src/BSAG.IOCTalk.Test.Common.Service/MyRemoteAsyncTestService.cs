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

        public static int RunSomeWorkCounter { get; set; }
    }
}
