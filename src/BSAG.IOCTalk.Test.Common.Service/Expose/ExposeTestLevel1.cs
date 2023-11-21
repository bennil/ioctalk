using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTestLevel1 : ExposeTestBase, IExposeTestLevel1
    {
        public string TestLevel1 { get; set; }
        public int TestId { get; set; }
    }
}
