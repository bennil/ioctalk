﻿using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace BSAG.IOCTalk.Composition
{
    public class SessionContract : IContract
    {
        private TalkCompositionHost source;
        private Dictionary<string, object> interfaceTypeNameInstanceCache = new Dictionary<string, object>();

        public SessionContract(TalkCompositionHost source, ISession session, object[] localServiceInstances, object[] remoteServiceInstances)
        {
            this.Session = session;
            this.RemoteServices = remoteServiceInstances;
            this.LocalServices = localServiceInstances;
            this.source = source;
        }

        public ISession Session { get; private set; }

        public object[] RemoteServices { get; private set; }

        public object[] LocalServices { get; private set; }


        public object GetInterfaceImplementationInstance(string interfaceType)
        {
            object result;
            if (!interfaceTypeNameInstanceCache.TryGetValue(interfaceType, out result))
            {
                // Determine type
                Type interfType;
                if (TypeService.TryGetTypeByName(interfaceType, out interfType))
                {
                    if (interfType.IsInterface == false)
                        throw new SecurityException($"Given type \"{interfType.FullName}\" is not an interface!");

                    if (!TryGetSessionInstanceInternalFunctionalOnly(interfType, out result))
                    {
                        // not found in session context > check if local share instance
                        if (!source.TryGetExport(interfType, out result))
                        {
                            throw new InvalidOperationException($"Can't find implementation for {interfaceType}");
                        }
                    }

                    interfaceTypeNameInstanceCache[interfaceType] = result;
                }
            }

            return result;
        }

        public InterfType GetSessionInstance<InterfType>()
        {
            InterfType result;
            if (TryGetSessionInstance<InterfType>(out result))
            {
                return result;
            }
            else
            {
                throw new TypeLoadException($"No session instance for interface {typeof(InterfType).FullName} found!");
            }
        }

        public bool TryGetSessionInstance<InterfType>(out InterfType instance)
        {
            object instanceObj;
            if (TryGetSessionInstance(typeof(InterfType), out instanceObj))
            {
                instance = (InterfType)instanceObj;
                return true;
            }
            else
            {
                instance = default(InterfType);
                return false;
            }
        }

        public bool TryGetSessionInstance(Type interfType, out object instance)
        {
            if (interfType == typeof(ISession))
            {
                instance = Session;
                return true;
            }
            else if (interfType == typeof(IGenericCommunicationService))
            {
                instance = Session.CommunicationService;
                return true;
            }
            else
            {
                return TryGetSessionInstanceInternalFunctionalOnly(interfType, out instance);
            }
        }

        private bool TryGetSessionInstanceInternalFunctionalOnly(Type interfType, out object instance)
        {
            // check local services
            Type alternativeLocalSourceType;
            int foundIndexAlternative;
            instance = null;
            int foundIndex = Array.IndexOf<Type>(source.LocalServiceInterfaceTypes, interfType);
            if (foundIndex >= 0)
            {
                instance = LocalServices[foundIndex];
            }
            else if (source.LocalSessionServiceTypeMappings != null
                && source.LocalSessionServiceTypeMappings.TryGetValue(interfType, out alternativeLocalSourceType)
                && (foundIndexAlternative = Array.IndexOf<Type>(source.LocalServiceInterfaceTypes, alternativeLocalSourceType)) >= 0)
            {
                instance = LocalServices[foundIndexAlternative];
            }
            else
            {
                // check remote services
                foundIndex = Array.IndexOf<Type>(source.RemoteServiceInterfaceTypes, interfType);
                if (foundIndex >= 0)
                {
                    instance = RemoteServices[foundIndex];
                }
            }

            return instance != null;
        }
    }
}
