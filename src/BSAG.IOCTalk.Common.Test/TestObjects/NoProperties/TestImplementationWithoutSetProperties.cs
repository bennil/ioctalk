using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.NoProperties
{
    public class TestImplementationWithoutSetProperties : ITestInterfaceWithoutSetProperties
    {
        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName
        {
            get { return "Hello"; }
        }
    }
}
