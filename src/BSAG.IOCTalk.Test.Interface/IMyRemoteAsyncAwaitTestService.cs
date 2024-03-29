﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IMyRemoteAsyncAwaitTestService
    {
        Task<string> GetDataAsync();


        Task<int> GetDataAsync2(int expected);


        Task<IDataTransferTest[]> GetDataAsync3(int expected);


        Task RunSomeWork();


        Task SimpleCall();


        Task<IDataTransferTest> ComplexRoundtrip(IDataTransferTest test);


        Task<IDataTransferTest> GetObjectDataAsync();

        void AsyncVoidTest(IDataTransferTest test);


        Task<IDataTransferTest> SameNameTest(int id);

        Task<IDataTransferTest[]> SameNameTest(string[] list);

        Task SameNameTest(string test);

    }
}
