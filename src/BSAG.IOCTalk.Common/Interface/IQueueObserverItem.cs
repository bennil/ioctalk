using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface
{
    public interface IQueueObserverItem
    {
        string Name { get; }

        int? CurrentQueueCount { get; }

        //int? MaxCount { get; }
    }
}
