using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public interface ITestInterfaceExtended : ITestInterfaceBase
    {
        /// <summary>
        /// Gets or sets the test base property.
        /// </summary>
        /// <value>
        /// The test base property.
        /// </value>
        string TestExtProperty { get; set; }



        /// <summary>
        /// Gets or sets the test collection.
        /// </summary>
        /// <value>
        /// The test collection.
        /// </value>
        IEnumerable<string> TestCollection { get; set; }
    }
}
