using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public interface ITestInterfaceBase : IDeepTestInterface2
    {
        /// <summary>
        /// Gets or sets the test base property.
        /// </summary>
        /// <value>
        /// The test base property.
        /// </value>
        string TestBaseProperty { get; set; }


    }
}
