using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface.Expose
{
    public interface IExposeTest2Container
    {
        IExposeTest2Base NestedItem { get; set; }
    }
}
