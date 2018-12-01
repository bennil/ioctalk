using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    public interface IDiscoveryCondition
    {
        bool IsMatching(IDiscoveryContext context);

        ITalkContainer TargetContainer { get; }
    }
}
