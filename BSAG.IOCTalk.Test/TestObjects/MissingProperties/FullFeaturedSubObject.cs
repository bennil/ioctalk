using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.MissingProperties
{
    public class FullFeaturedSubObject : FullFeaturedObject
    {
        public SubObject SubObjectProperty { get; set; }
    }
}
