using BSAG.IOCTalk.Common.Interface;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BSAG.IOCTalk.Communication.Common
{
    internal class QueueObserverItem : IQueueObserverItem
    {
        object queueInstance;
        Func<int?> getCurrentCountFunc;

        internal QueueObserverItem(object queueInstance, Func<int?> getCurrentCountFunc, string name)
        {
            this.queueInstance = queueInstance;
            this.getCurrentCountFunc = getCurrentCountFunc;
            this.Name = name;
        }

        public static QueueObserverItem FromCollectionQueue(ICollection queueInstance, string name)
        {
            return new QueueObserverItem(queueInstance, () => queueInstance.Count, name);
        }

        public static QueueObserverItem FromChannelQueue<T>(Channel<T> queueInstance, string name)
        {
            return new QueueObserverItem(queueInstance, () => queueInstance.Reader.CanCount ? queueInstance.Reader.Count : (int?)null, name);
        }


        public string Name { get; private set; }

        public int? CurrentQueueCount => getCurrentCountFunc();

        //public int? MaxCount { get; private set; }
    }
}
