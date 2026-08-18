using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Interception
{
    /// <summary>
    /// Creation context of a multi import breakout branch. Passed along the constructor import resolution
    /// so that nested interface imports within the branch subtree are resolved against the branch interception
    /// chain instead of the main interception chain (position based instead of type identity based resolution).
    /// </summary>
    internal class MultiImportBranchContext
    {
        internal MultiImportBranchContext(TypeHierachy hierachy, BreakoutType branch, int baseChainCount)
        {
            this.Hierachy = hierachy;
            this.Branch = branch;
            this.BaseChainCount = baseChainCount;
        }

        /// <summary>
        /// The interface type hierachy the branch belongs to
        /// </summary>
        public TypeHierachy Hierachy { get; }

        /// <summary>
        /// The multi import breakout branch
        /// </summary>
        public BreakoutType Branch { get; }

        /// <summary>
        /// Number of pending interface chain instances at the time the branch creation started.
        /// The difference to the current pending chain count determines the depth within the branch interception chain.
        /// </summary>
        public int BaseChainCount { get; }
    }
}
