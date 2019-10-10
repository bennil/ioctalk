using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public class TestInterfaceImpl1Collections
    {
        public object AnyObjectArray { get; set; }

        public List<ITestInterfaceBase> List { get; set; }

        public ITestInterfaceBase[] Array { get; set; }

        public object[] ObjectArray { get; set; }

        public OwnCollection OwnCollection { get; set; }
    }
}
