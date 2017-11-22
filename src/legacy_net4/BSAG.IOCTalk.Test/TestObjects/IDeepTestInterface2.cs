using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public interface IDeepTestInterface2 : IDeepTestInterface1
    {
        string DeepTestProperty2 { get; set; }
    }
}
