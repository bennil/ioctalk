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
            if (breakoutTypes == null)
                breakoutTypes = new List<BreakoutType>();

            if (interceptionTypes is null)
                breakoutTypes.Add(new BreakoutType(0, breakoutType));     // top level multi implementation
            else
                breakoutTypes.Add(new BreakoutType(interceptionTypes.Count - 1, breakoutType));
        }


        /// <summary>
        /// Encapsulates the last registered multi import branch (breakout) implementation with the given interception type.
        /// </summary>
        /// <param name="interceptType"></param>
        public void AddBreakoutInterceptionType(Type interceptType)
        {
            if (breakoutTypes == null || breakoutTypes.Count == 0)
                throw new InvalidOperationException($"No multi import branch implementation registered for the interface {InterfaceType.FullName}! Call AddMultiImportBranchImplementation before InterceptMultiImportWithImplementation.");

            breakoutTypes[breakoutTypes.Count - 1].AddInterceptionType(interceptType);
        }


        internal Type GetNextImplementationType(Type injectTargetType, List<Type> pendingCreateList, out bool registerTargetInstance)
        {
            return GetNextImplementationType(injectTargetType, pendingCreateList, null, out registerTargetInstance);
        }

        internal Type GetNextImplementationType(Type injectTargetType, List<Type> pendingCreateList, MultiImportBranchContext branchContext, out bool registerTargetInstance)
        {
            if (TryGetBranchImplementationType(injectTargetType, pendingCreateList, branchContext, out Type branchImplementationType))
            {
                // creation within a multi import breakout branch > resolve against the branch interception chain
                registerTargetInstance = false;
                return branchImplementationType;
            }

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

        /// <summary>
        /// Resolves the next implementation type within a multi import breakout branch based on the given creation context (position based resolution).
        /// </summary>
        private bool TryGetBranchImplementationType(Type injectTargetType, List<Type> pendingCreateList, MultiImportBranchContext branchContext, out Type branchImplementationType)
        {
            if (branchContext != null
                && object.ReferenceEquals(branchContext.Hierachy, this)
                && injectTargetType != null
                && InterfaceType.IsAssignableFrom(injectTargetType))
            {
                int branchDepth = GetCurrentInterceptChainCount(pendingCreateList) - branchContext.BaseChainCount;
                if (branchDepth >= 1 && branchDepth <= branchContext.Branch.InterceptionCount)
                {
                    branchImplementationType = branchContext.Branch.GetImplementationTypeAtDepth(branchDepth);
                    return true;
                }
            }

            branchImplementationType = null;
            return false;
        }

        private int GetCurrentInterceptChainCount(List<Type> pendingCreateList)
        {
            if (pendingCreateList == null)
                return 0;

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
        internal bool TryGetMultiImportSlots(Type injectTargetType, List<Type> pendingCreateList, MultiImportBranchContext branchContext, out MultiImportSlot[] multiImportSlots)
        {
            if (TryGetBranchImplementationType(injectTargetType, pendingCreateList, branchContext, out Type branchImplementationType))
            {
                // collection import within a multi import breakout branch > continue single branch chain
                multiImportSlots = new[] { new MultiImportSlot(branchImplementationType, branchContext) };
                return true;
            }

            if (breakoutTypes == null)
            {
                // no breakout types > return interception level as array
                Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                multiImportSlots = new[] { new MultiImportSlot(interceptType, null) };
                return true;
            }
            else
            {
                if (interceptionTypes is null)
                {
                    // no interception > top level multi import registration
                    int baseChainCount = GetCurrentInterceptChainCount(pendingCreateList);
                    multiImportSlots = new[] { new MultiImportSlot(MainImplementationType, null) }
                        .Concat(breakoutTypes.Select(bt => new MultiImportSlot(bt.GetOuterImplementationType(), new MultiImportBranchContext(this, bt, baseChainCount))))
                        .ToArray();
                    return true;
                }
                else
                {
                    int currentInterceptChainCount = GetCurrentInterceptChainCount(pendingCreateList);

                    int hierachyCurrentIndex = interceptionTypes.Count - currentInterceptChainCount;

                    var additionalBreakOutImplementations = breakoutTypes.Where(bt => bt.InterceptionHierarchyIndex == hierachyCurrentIndex).ToList();
                    if (additionalBreakOutImplementations.Any())
                    {
                        // combine level implementation and breakout branches as array
                        Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                        multiImportSlots = new[] { new MultiImportSlot(interceptType, null) }
                            .Concat(additionalBreakOutImplementations.Select(bt => new MultiImportSlot(bt.GetOuterImplementationType(), new MultiImportBranchContext(this, bt, currentInterceptChainCount))))
                            .ToArray();

                        return true;
                    }
                    else
                    {
                        // no breakout types at this level > return interception level as array
                        Type interceptType = GetNextImplementationType(injectTargetType, pendingCreateList, out var _);
                        multiImportSlots = new[] { new MultiImportSlot(interceptType, null) };
                        return true;
                    }
                }
            }
        }
    }
}
