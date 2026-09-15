using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface.Expose
{
    /// <summary>
    /// Derived interface of <see cref="IExposeTest4Base"/> - no explicit exposure registration (declared type must be transferred)
    /// </summary>
    public interface IExposeTest4Sub : IExposeTest4Base
    {
        string SubProperty { get; set; }
    }
}
