using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Test.TestObjects.InterfaceMapTest
{
    public class MapTestDerived : IMapTestDerivedInterface
    {
        public int SubProperty { get; set; }
        public string MainProperty { get; set; }
    }
}
