using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Container;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BSAG.IOCTalk.Communication.Common
{
    public class QueueObserver : IQueueObserver
    {
        ConcurrentDictionary<object, IQueueObserverItem> observerQueues = new ConcurrentDictionary<object, IQueueObserverItem>();

        //static readonly Lazy<QueueObserver> instance = new Lazy<QueueObserver>(() => new QueueObserver());

        //public static QueueObserver Instance => instance.Value;


        public int QueueCount => observerQueues.Count;

        public IEnumerable<IQueueObserverItem> GetQueueObserverItems()
        {
            foreach (var item in observerQueues.Values)
            {
                yield return item;
            }
        }



        public void RegisterQueue(ICollection queueInstance, string name)
        {
            observerQueues[queueInstance] = QueueObserverItem.FromCollectionQueue(queueInstance, name);
        }


        public void RegisterQueue<ItemType>(object queueInstance, string name)
        {
            if (queueInstance is null)
                throw new ArgumentNullException(nameof(queueInstance));

            if (queueInstance is Channel<ItemType> channel)
            {
                observerQueues[queueInstance] = QueueObserverItem.FromChannelQueue(channel, name);
            }
            else if (queueInstance is ICollection collectionQueue)
            {
                observerQueues[queueInstance] = QueueObserverItem.FromCollectionQueue(collectionQueue, name);
            }
            else
                throw new NotSupportedException($"Queue instance type {queueInstance.GetType().FullName} is not supported by the {nameof(QueueObserver)}");
        }

        public void UnregisterQueue(object queueInstance)
        {
            if (queueInstance != null)
                observerQueues.TryRemove(queueInstance, out var _);
        }


        public static IQueueObserver GetQueueObserverFromContainer(IGenericContainerHost containerHost, Type injectTargetType)
        {
            if (containerHost is ITalkContainer container)
            {
                if (container.TryGetExport(typeof(IQueueObserver), injectTargetType, out object queueObserverObj))
                    return (IQueueObserver)queueObserverObj;
                else
                {

                    var queueObserver = new QueueObserver();
                    container.RegisterLocalSharedService<IQueueObserver>(queueObserver);
                    return queueObserver;
                }
            }
            else
                return null;
        }

    }
}
