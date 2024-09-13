using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace BSAG.IOCTalk.Composition.Interception
{
    internal class TypeHierachy
    {
        List<Type> interceptionTypes;
        List<BreakoutType> breakoutTypes;

        public TypeHierachy(Type interfaceType, Type mainImplementationType)
        {
            InterfaceType = interfaceType;
            MainImplementationType = mainImplementationType;
        }

        public Type InterfaceType { get; private set; }

        public Type MainImplementationType { get; private set; }

        public void AddInterceptionType(Type interceptType)
        {
            if (interceptionTypes == null)
                interceptionTypes = new List<Type>();

            interceptionTypes.Add(interceptType);
        }


        /// <summary>
        /// Adds an additional implementation at the same interception layer used when the parent implementation requests multiple instances of the interface type.
        /// </summary>
        /// <param name="breakoutType"></param>
        public void AddBreakoutType(Type breakoutType)
        {
            if (interceptionTypes == null)
                throw new InvalidOperationException("Breakout implementations on top level not supported!");

            if (breakoutTypes == null)
                breakoutTypes = new List<BreakoutType>();

            breakoutTypes.Add(new BreakoutType(interceptionTypes.Count - 1, breakoutType));
        }


        internal Type GetNextImplementationType(Type injectTargetType, List<Type> pendingCreateList, out bool registerTargetInstance)
        {
            if (interceptionTypes == null)
            {
                registerTargetInstance = true;
                return MainImplementationType;
            }
            else
            {
                bool isInterfaceChainImport = pendingCreateList != null && injectTargetType != null && InterfaceType.IsAssignableFrom(injectTargetType);

                if (isInterfaceChainImport)
                {
                    registerTargetInstance = false;

                    int currentInterceptChainCount = GetCurrentInterceptChainCount(pendingCreateList);

                    int nextHierachyIndex = interceptionTypes.Count - currentInterceptChainCount - 1;
                    if (nextHierachyIndex < 0)
                        return MainImplementationType;
                    else
                        return interceptionTypes[nextHierachyIndex];
                }
                else
                {
                    registerTargetInstance = true;      // only register last interception level in container intance mapping
                    return interceptionTypes.Last();    // return last interception
                }
            }
        }

        private int GetCurrentInterceptChainCount(List<Type> pendingCreateList)
        {
            int currentInterceptChainCount = 0;
            for (int pendIndex = pendingCreateList.Count - 1; pendIndex >= 0; pendIndex--)
            {
                if (InterfaceType.IsAssignableFrom(pendingCreateList[pendIndex]))
                    currentInterceptChainCount++;
                else
                    break;
            }

            return currentInterceptChainCount;
        }

        /// <summary>
        /// For collection constructor injections
        /// </summary>
        /// <returns></returns>
        internal bool TryGetImplementationTypes(Type injectTargetType, List<Type> pendingCreateList, out Type[] implementationTypes)
        {
            if (breakoutTypes == null)
            {
                // no breakout types > return interception level as array
                Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                implementationTypes = new[] { interceptType };
                return true;
            }
            else
            {
                int currentInterceptChainCount = GetCurrentInterceptChainCount(pendingCreateList);

                int hierachyCurrentIndex = interceptionTypes.Count - currentInterceptChainCount;

                var additionalBreakOutImplementations = breakoutTypes.Where(bt => bt.InterceptionHierarchyIndex == hierachyCurrentIndex).ToList();
                if (additionalBreakOutImplementations.Any())
                {
                    // combine level implementation and breakout as array
                    Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                    implementationTypes = new[] { interceptType }.Concat(additionalBreakOutImplementations.Select(ab => ab.AdditionalImplementationType)).ToArray();

                    return true;
                }
                else
                {
                    // no breakout types at this level > return interception level as array
                    Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                    implementationTypes = new[] { interceptType };
                    return true;
                }
            }
        }
    }
}
