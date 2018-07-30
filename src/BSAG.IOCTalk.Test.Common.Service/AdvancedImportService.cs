using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class AdvancedImportService : IAdvancedSessionStateChangeService
    {
        public AdvancedImportService(out Action<IMyTestService, int> onTestServiceCreated, out Action<IMyTestService> onTestServiceTerminated, out Action<IAdvancedSessionStateChangeService> onMyselfCreated)
        {
            onTestServiceCreated = OnMyTestServiceSessionCreated;
            onTestServiceTerminated = OnMyTestServiceSessionTerminated;
            onMyselfCreated = OnMyselfCreated;
        }

        private void OnMyTestServiceSessionCreated(IMyTestService testService, int sessionId)
        {
            CreatedCount++;
        }

        private void OnMyTestServiceSessionTerminated(IMyTestService testService)
        {
            CreatedCount--;
        }

        private void OnMyselfCreated(IAdvancedSessionStateChangeService me)
        {

        }


        public static int CreatedCount { get; set; }
    }
}
