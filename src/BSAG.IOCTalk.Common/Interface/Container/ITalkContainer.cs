using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    public interface ITalkContainer
    {
        ITalkContainer ParentContainer { get; set; }

        void Init(bool initSubContainers);

        bool IsSubscriptionRegistered(Type serviceDelegateType);

        bool TryGetExport(Type type, Type injectTargetType, out object instance);
    }
}
