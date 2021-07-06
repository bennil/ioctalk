using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Test.TestObjects
{
    public class DataTransferTest : IDataTransferTest
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
