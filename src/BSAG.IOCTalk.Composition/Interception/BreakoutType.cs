using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Interception
{
    internal class BreakoutType
    {
        List<Type> interceptionTypes;

        internal BreakoutType(int interceptionHierarchyIndex, Type additionalImplementationType)
        {
            this.InterceptionHierarchyIndex = interceptionHierarchyIndex;
            this.AdditionalImplementationType = additionalImplementationType;
        }

        public int InterceptionHierarchyIndex { get; set; }

        public Type AdditionalImplementationType { get; set; }

        /// <summary>
        /// Number of interception types encapsulating the breakout implementation
        /// </summary>
        public int InterceptionCount
        {
            get { return interceptionTypes != null ? interceptionTypes.Count : 0; }
        }

        /// <summary>
        /// Encapsulates the breakout implementation with the given interception type.
        /// </summary>
        /// <param name="interceptType"></param>
        public void AddInterceptionType(Type interceptType)
        {
            if (interceptionTypes == null)
                interceptionTypes = new List<Type>();

            interceptionTypes.Add(interceptType);
        }

        /// <summary>
        /// Gets the outer implementation type of this breakout branch (last interception type or the breakout implementation itself if not intercepted).
        /// </summary>
        /// <returns></returns>
        public Type GetOuterImplementationType()
        {
            return GetImplementationTypeAtDepth(0);
        }

        /// <summary>
        /// Gets the implementation type at the given branch chain depth.
        /// Depth 0 = outermost interception (the multi import slot type), last depth = the breakout implementation itself.
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public Type GetImplementationTypeAtDepth(int depth)
        {
            int interceptCount = InterceptionCount;
            if (depth < interceptCount)
                return interceptionTypes[interceptCount - 1 - depth];
            else
                return AdditionalImplementationType;
        }
    }
}
