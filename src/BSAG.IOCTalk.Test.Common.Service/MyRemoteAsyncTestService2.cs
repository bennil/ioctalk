using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyRemoteAsyncTestService2 : IMyRemoteAsyncAwaitTestService
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
                    Name = $"Testobject{i+1}"
                };
                responseArr[i] = item;
            }

            return Task<IDataTransferTest[]>.FromResult(responseArr);
        }

        public async Task<IDataTransferTest> GetObjectDataAsync()
        {
            await Task.Delay(10);   // force async execution

            return new DTTest { ID = 5 };
        }

        public async void AsyncVoidTest(IDataTransferTest test)
        {
            //await Task.Delay(10);   // force async execution
        }



        public Task<IDataTransferTest> SameNameTest(int id)
        {
            return Task.FromResult<IDataTransferTest>(new DTTest { ID = id, Name = "SameNameTest" });
        }

        public Task<IDataTransferTest[]> SameNameTest(string[] list)
        {
            List<IDataTransferTest> items = new List<IDataTransferTest>();
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                items.Add(new DTTest { ID = i, Name = item });
            }
            return Task.FromResult<IDataTransferTest[]>(items.ToArray());
        }

        public Task SameNameTest(string test)
        {
            // do nothing
            return Task.CompletedTask;
        }



        public static int RunSomeWorkCounter { get; set; }
    }
}
