using BSAG.IOCTalk.Common.Interface.Container;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Composition.Condition
{
    /// <summary>
    /// Checks if the target inject class (assembly based) should be handled by another container.
    /// </summary>
    public class InjectTargetAssemblyCondition : IDiscoveryCondition
    {
        private Assembly injectTargetAssembly;
        private readonly ITalkContainer targetContainer;

        
        public InjectTargetAssemblyCondition(Assembly injectTargetAssembly, ITalkContainer targetContainer)
        {
            this.targetContainer = targetContainer;
            this.injectTargetAssembly = injectTargetAssembly;
        }


        public ITalkContainer TargetContainer => targetContainer;

        

        public bool IsMatching(IDiscoveryContext context)
        {
            if (context.InjectTargetType == null)
                return false;

            Assembly targetAssembly = context.InjectTargetType.Assembly;

            return injectTargetAssembly.Equals(targetAssembly);
        }
    }
}
