using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.ChangedLayout
{
    public class PropertyExtended
    {
        public int ID { get; set; }

        // Name was extended (see UnitTest: TestMethodBinaryLayoutModification_PropertyExtension)
        public string Name { get; set; }

    }
}
