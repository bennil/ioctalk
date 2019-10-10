using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.ChangedLayout
{
    public class PropertyRemoved
    {
        public int ID { get; set; }

        // Name was removed (see UnitTest: TestMethodBinaryLayoutModification_RemovedProperty)
        //public string Name { get; set; }
    }
}
