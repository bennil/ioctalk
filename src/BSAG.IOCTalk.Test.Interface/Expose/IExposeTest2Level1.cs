using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface.Expose
{
    public interface IExposeTest2Level1 : IExposeTest2Base
    {
        string Level1Property { get; set; }
    }
}
