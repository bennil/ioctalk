using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Interception
{
    /// <summary>
    /// One element of a resolved multi import (collection/array constructor injection).
    /// </summary>
    internal class MultiImportSlot
    {
        internal MultiImportSlot(Type implementationType, MultiImportBranchContext branchContext)
        {
            this.ImplementationType = implementationType;
            this.BranchContext = branchContext;
        }

        public Type ImplementationType { get; }

        /// <summary>
        /// Branch creation context if the slot originates from a multi import breakout branch; null for the main chain or assembly scan results
        /// </summary>
        public MultiImportBranchContext BranchContext { get; }
    }
}
