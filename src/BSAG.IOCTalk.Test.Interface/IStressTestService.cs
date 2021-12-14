using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IStressTestService
    {
        void AsyncCallTest(int number);

        int SyncCallTest(int number);

        int ComplexCall(int number, IDataTransferTest data);

        void SimpleCall();
    }
}
