using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Interception
{
    internal class BreakoutType
    {
        internal BreakoutType(int interceptionHierarchyIndex, Type additionalImplementationType)
        {
            this.InterceptionHierarchyIndex = interceptionHierarchyIndex;
            this.AdditionalImplementationType = additionalImplementationType;
        }

        public int InterceptionHierarchyIndex { get; set; }

        public Type AdditionalImplementationType { get; set; }
    }
}
