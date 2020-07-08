using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Attributes;

namespace BSAG.IOCTalk.Test.Common
{
    public interface IPerformanceMonitorClientNotification
    {
        //[RemoteInvokeBehaviour(IsAsyncRemoteInvoke=true)] //todo: fluent register async void method
        void OnPerformanceData(IPerformanceData perfData);

        void OnPerformanceData(IPerformanceData perfData, string computerName);

        void OnPerformancedDataSubscribed();

        bool NestedTestCall();
    }
}
