using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTest2Level1 : IExposeTest2Base, IExposeTest2Level1
    {
        public string Level1Property { get; set; }
        public int BaseProperty { get; set; }
    }
}
