using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IMapTestDerivedInterface : IMapTestMainInterface
    {
        int SubProperty { get; set; }
    }
}
