using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeTest3Combined : IExposeTest3Base, IExposeTest3Other
    {
        public string SomeBasicProperty { get; set; }

        public string OtherTypeProperty { get; set; }
    }
}
