using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public interface IEnumerableTest : IEnumerable<string>
    {
        int Dummy { get; set; }
    }
}
