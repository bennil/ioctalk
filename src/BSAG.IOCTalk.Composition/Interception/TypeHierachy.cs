using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Composition.Interception
{
    internal class TypeHierachy
    {
        List<Type> interceptionTypes;

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

        internal Type GetNextImplementationType(Type injectTargetType, List<Type> pendingCreateList, out bool registerTargetInstance)
        {
            if (interceptionTypes == null)
            {
                registerTargetInstance = true;
                return MainImplementationType;
            }
            else
            {
                if (pendingCreateList == null)
                {
                    registerTargetInstance = true;      // only register last interception level in container intance mapping
                    return interceptionTypes.Last();    // return last interception
                }
                else
                {
                    registerTargetInstance = false;

                    int hierachyIndex = interceptionTypes.Count - pendingCreateList.Count - 1;
                    if (hierachyIndex < 0)
                        return MainImplementationType;
                    else
                        return interceptionTypes[hierachyIndex];
                }
            }
        }
    }
}
