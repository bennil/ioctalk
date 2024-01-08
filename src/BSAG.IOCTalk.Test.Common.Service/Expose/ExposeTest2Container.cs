using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTest2Container : IExposeTest2Container
    {
        public IExposeTest2Base NestedItem { get; set; }
    }
}
