using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTestBase : IExposeTestBase, IExposeTestOther
    {
        public int TestId { get; set; }

        public int OtherTypeProperty { get; set; }
    }
}
