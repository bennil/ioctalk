using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface.Expose
{
    public interface IExposeTestLevel1 : IExposeTestBase
    {
        string TestLevel1 { get; set;}
    }
}
