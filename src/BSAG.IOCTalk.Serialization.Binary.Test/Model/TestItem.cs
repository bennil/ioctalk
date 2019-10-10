using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary.Test.Model
{
    public class TestItem : ITestItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
