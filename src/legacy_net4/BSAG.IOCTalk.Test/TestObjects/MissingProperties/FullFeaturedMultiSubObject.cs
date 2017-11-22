using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.TestObjects.MissingProperties
{
    public class FullFeaturedMultiSubObject : FullFeaturedSubObject
    {
        /// <summary>
        /// Gets or sets the holder1.
        /// </summary>
        /// <value>
        /// The holder1.
        /// </value>
        public SubObjectHolder Holder1 { get; set; }

        /// <summary>
        /// Gets or sets the holder2.
        /// </summary>
        /// <value>
        /// The holder2.
        /// </value>
        public SubObjectHolder Holder2 { get; set; }
    }
}
