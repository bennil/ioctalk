using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyTestService : IMyTestService
    {
        public MyTestService(IMyRemoteTestService remoteService, int sessionId)
        {

        }
    }
}
