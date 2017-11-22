using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.NoProperties
{
    public class NoPropertiesTestHolder
    {
        /// <summary>
        /// Gets or sets the no properties object.
        /// </summary>
        /// <value>
        /// The no properties object.
        /// </value>
        public ITestInterfaceWithoutSetProperties NoPropertiesObject { get; set;  }

        /// <summary>
        /// Gets or sets the dummy.
        /// </summary>
        /// <value>
        /// The dummy.
        /// </value>
        public string Dummy { get; set; }
    }
}
