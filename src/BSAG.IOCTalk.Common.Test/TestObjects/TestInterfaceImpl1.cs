using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Attributes;

namespace BSAG.IOCTalk.Test.TestObjects
{
    [ExposeSubType(Type = typeof(ITestInterfaceBase))]
    public class TestInterfaceImpl1 : ITestInterfaceBase
    {
        public string TestBaseProperty { get; set; }

        public string AdditionalProperty { get; set; }


        public string DeepTestProperty1 { get; set; }

        public string DeepTestProperty2 { get; set; }

    }
}
