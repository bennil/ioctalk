using BSAG.IOCTalk.Test.TestObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Test.TestObjects
{
    public class TestInterfaceExtendedImple : ITestInterfaceExtended
    {
        public string TestExtProperty { get; set; }
        public IEnumerable<string> TestCollection { get; set; }
        public string TestBaseProperty { get; set; }
        public string DeepTestProperty2 { get; set; }
        public string DeepTestProperty1 { get; set; }
    }
}
