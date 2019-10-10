using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary.Test.Model
{
    public interface ITestItem
    {
        int ID { get; set; }

        string Name { get; set; }
    }
}
