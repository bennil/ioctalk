using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface
{
    public interface IQueueObserver
    {
        int QueueCount { get; }

        void RegisterQueue(ICollection queueInstance, string name);

        void RegisterQueue<ItemType>(object queueInstance, string name);


        void UnregisterQueue(object queueInstance);

        IEnumerable<IQueueObserverItem> GetQueueObserverItems();
    }
}
