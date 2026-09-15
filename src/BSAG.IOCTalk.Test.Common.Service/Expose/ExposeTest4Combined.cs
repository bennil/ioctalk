using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTest4Combined : IExposeTest4Sub
    {
        public string BaseProperty { get; set; }

        public string SubProperty { get; set; }
    }
}
