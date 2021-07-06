using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class StressTestService : IStressTestService
    {
        int expectedNumber = 0;

        public void AsyncCallTest(int number)
        {
            CheckNumber(number);
        }

        public int SyncCallTest(int number)
        {
            return CheckNumber(number);
        }

        private int CheckNumber(int number)
        {
            if (number == expectedNumber)
            {
                expectedNumber++;
                return number;
            }
            else
                throw new InvalidOperationException($"Unexpected number {number} received");
        }

        public int ComplexCall(int number, IDataTransferTest data)
        {
            if (data.ID != number)
                throw new InvalidOperationException();

            return CheckNumber(number);
        }

        public int CurrentNumber => expectedNumber;
    }
}
