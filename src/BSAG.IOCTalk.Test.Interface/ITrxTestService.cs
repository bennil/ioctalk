﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface ITrxTestService
    {
        Guid StartTransactionTest();

        void PushTrxData(Guid testSessionId);

        void CompleteTransactionTest(Guid testSessionId);
    }
}
