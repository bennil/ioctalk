using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    internal class DTTest : IDataTransferTest
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
