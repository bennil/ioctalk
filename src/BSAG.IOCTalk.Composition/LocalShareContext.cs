using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Session;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Composition
{
    public class LocalShareContext
    {
        private List<Type> localMultipleSharedInterfaceTypes;
        private Dictionary<Type, IList> localSharedMultipleInterfaceInstances;
        private List<Type> manualManagedServiceInterfaceTypes;

        private List<Type> localSharedInterfaceTypes;
        private Dictionary<Type, object> sharedLocalInstances = new Dictionary<Type, object>();

        internal Dictionary<Type, SessionChangeSubscription> sessionCreatedSubscriptions = new Dictionary<Type, SessionChangeSubscription>();
        internal Dictionary<Type, SessionChangeSubscription> sessionTerminatedSubscriptions = new Dictionary<Type, SessionChangeSubscription>();

        private List<TalkCompositionHost> assignedHosts = new List<TalkCompositionHost>();
        private Session inMemorySession;
        private SessionContract inMemorySessionContract;

        public Dictionary<Type, object> SharedLocalInstances
        {
            get
            {
                return sharedLocalInstances;
            }
        }

        public List<Type> LocalSharedInterfaceTypes
        {
            get
            {
                return localSharedInterfaceTypes;
            }
        }


        public bool TryGetCachedLocalExport(Type type, out object instance)
        {
            if (sharedLocalInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            if (type.IsArray)
            {
                IList resultList;
                if (localSharedMultipleInterfaceInstances.TryGetValue(type, out resultList))
                {
                    instance = resultList;
                    return true;
                }
            }

            instance = null;
            return false;
        }

        //todo move to share ctx
        //public bool TryGetExport(Type type, Type injectTargetType, out object instance)
        //{
        //    if (SharedLocalInstances.TryGetValue(type, out instance))
        //    {
        //        return true;
        //    }

        //    //if (hostSessionsSharedInstances.TryGetValue(type, out instance))
        //    //{
        //    //    return true;
        //    //}

        //    Type targetType;
        //    if (type.IsInterface)
        //    {
        //        //var contract = currentContract;
        //        //if (contract != null)
        //        //{
        //        //    if (contract.TryGetSessionInstance(type, out instance))
        //        //    {
        //        //        return true;
        //        //    }
        //        //}

        //        if (!TryFindInterfaceImplementation(type, injectTargetType, out targetType))
        //        {
        //            // not found > check if multiple import
        //            if (type.GetInterface(typeof(System.Collections.IEnumerable).FullName) != null)
        //            {
        //                var multiImportColl = localShare.CollectLocalMultiImports(this, type, injectTargetType);
        //                if (multiImportColl != null)
        //                {
        //                    instance = multiImportColl;
        //                    return true;
        //                }
        //            }
        //            else
        //            {
        //                instance = null;
        //                return false;
        //            }
        //        }
        //    }
        //    else if (type.IsArray)
        //    {
        //        var multiImportColl = this.CollectLocalMultiImports(this, type, injectTargetType);
        //        if (multiImportColl != null)
        //        {
        //            instance = multiImportColl;
        //            return true;
        //        }
        //        else
        //        {
        //            instance = null;
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        targetType = type;
        //    }

        //    object[] outParams;
        //    ParameterInfo[] outParamsInfo;
        //    instance = TypeService.CreateInstance(targetType, DetermineConstructorImportInstance, out outParams, out outParamsInfo);
        //    CheckOutParamsSubscriptions(instance, outParams);

        //    RegisterSharedConstructorInstances(type, instance, outParams, outParamsInfo);

        //    return true;
        //}



        internal void AssignHost(TalkCompositionHost host)
        {
            if (!assignedHosts.Contains(host))
            {
                assignedHosts.Add(host);
            }
        }


        #region Multi Shared

        private bool IsMultiShared(Type interfaceType)
        {
            return localMultipleSharedInterfaceTypes != null && localMultipleSharedInterfaceTypes.Contains(interfaceType);
        }


        internal IEnumerable CollectLocalMultiImports(TalkCompositionHost host, Type type, Type injectTargetType)
        {
            // multiple imports
            // 1. determine generic target interface type
            // 2. create concreate target collection instance
            // 3. add one instance of all local implementations
            Type genericCollectionInterface = type.GetInterface("IEnumerable`1");
            if (type.IsGenericType
                || genericCollectionInterface != null)
            {
                Type enumerableClassType = null;
                Type[] genericTypes = type.GetGenericArguments();
                if (genericTypes.Length == 1)
                {
                    Type listType = typeof(List<>);
                    enumerableClassType = listType.MakeGenericType(genericTypes);
                }
                else if (genericTypes.Length == 0
                   && genericCollectionInterface != null)
                {
                    genericTypes = genericCollectionInterface.GetGenericArguments();
                    Type listType = typeof(List<>);
                    enumerableClassType = listType.MakeGenericType(genericTypes);
                }

                if (enumerableClassType != null && genericTypes.Length == 1)
                {
                    Type targetInterfaceType = genericTypes[0];

                    bool isShared = IsMultiShared(targetInterfaceType);
                    IList targetCollection = null;
                    if (isShared)
                    {
                        targetCollection = GetLocalSharedMultipleInstances(targetInterfaceType);
                    }

                    if (targetCollection == null)
                    {
                        targetCollection = (IList)TypeService.CreateInstance(enumerableClassType);

                        // create new instances
                        HashSet<Type> createdTypes = new HashSet<Type>();
                        foreach (Type implType in host.FindInterfaceImplementations(targetInterfaceType, injectTargetType))
                        {
                            if (!createdTypes.Contains(implType) && injectTargetType != implType)
                            {
                                object[] outParams;
                                ParameterInfo[] outParamsInfo;
                                var itemInstance = TypeService.CreateInstance(implType, host.DetermineConstructorImportInstance, out outParams, out outParamsInfo);
                                CheckOutParamsSubscriptions(itemInstance, outParams);
                                targetCollection.Add(itemInstance);

                                RegisterSharedConstructorInstances(targetInterfaceType, itemInstance, outParams, outParamsInfo);

                                createdTypes.Add(implType);
                            }
                        }

                        if (isShared)
                        {
                            if (localSharedMultipleInterfaceInstances == null)
                                localSharedMultipleInterfaceInstances = new Dictionary<Type, IList>();

                            // add to shared dictionary
                            localSharedMultipleInterfaceInstances.Add(targetInterfaceType, targetCollection);
                        }
                    }

                    if (type.IsArray)
                    {
                        Type arrayItemType = type.GetElementType();

                        Array targetArray = Array.CreateInstance(arrayItemType, targetCollection.Count);
                        for (int i = 0; i < targetCollection.Count; i++)
                        {
                            targetArray.SetValue(targetCollection[i], i);
                        }

                        return targetArray;
                    }
                    else
                    {
                        return targetCollection;
                    }
                }
            }

            return null;
        }

        internal void RegisterSharedConstructorInstances(Type mainInterfaceType, object mainInstance, object[] outParams, ParameterInfo[] outParamsInfo)
        {
            if (localSharedInterfaceTypes != null
                && localSharedInterfaceTypes.Contains(mainInterfaceType))
            {
                if (!sharedLocalInstances.ContainsKey(mainInterfaceType))
                {
                    RegisterLocalSharedService(mainInterfaceType, mainInstance);
                }
            }

            //todo: check multi collection import. allowed?
            //todo: check if necessary:
            //if (outParamsInfo != null)
            //{
            //    for (int i = 0; i < outParamsInfo.Length; i++)
            //    {
            //        var paramInfo = outParamsInfo[i];

            //        if (localSharedInterfaceTypes.Contains(paramInfo.ParameterType))
            //        {
            //            RegisterLocalSharedService(paramInfo.ParameterType, outParams[i]);
            //        }
            //    }
            //}
        }

        internal IList GetLocalSharedMultipleInstances(Type interfaceType)
        {
            if (localSharedMultipleInterfaceInstances != null)
            {
                IList result;
                if (localSharedMultipleInterfaceInstances.TryGetValue(interfaceType, out result))
                {
                    return result;
                }
            }

            return null;
        }


        #endregion



        internal void CheckOutParamsSubscriptions(object instance, object[] outParams)
        {
            if (outParams != null)
            {
                // todo: subscribe action
                foreach (var outParam in outParams)
                {
                    if (outParam is Delegate)
                    {
                        Delegate sessionDelegate = (Delegate)outParam;

                        var method = sessionDelegate.Method;
                        string methodName = method.Name;
                        var parameters = method.GetParameters();

                        Type serviceDelegateType = null;
                        foreach (var p in parameters)
                        {
                            if (p.ParameterType != typeof(int) && p.ParameterType != typeof(string))
                            {
                                serviceDelegateType = p.ParameterType;
                                break;
                            }
                        }

                        if (methodName.EndsWith("Created", false, CultureInfo.InvariantCulture))
                        {
                            SessionChangeSubscription subscription;
                            if (!sessionCreatedSubscriptions.TryGetValue(serviceDelegateType, out subscription))
                            {
                                subscription = new SessionChangeSubscription(serviceDelegateType);
                                sessionCreatedSubscriptions.Add(serviceDelegateType, subscription);

                                if (!CheckIfSubscriptionInterfaceIsRegistered(serviceDelegateType))
                                {
                                    throw new InvalidOperationException($"Cannot subscribe session events for {serviceDelegateType.FullName}! No registration was found in the assigned container hosts. Are you missing a RegisterRemoteService<{serviceDelegateType.Name}>()?");
                                }
                            }

                            subscription.AddDelegate(sessionDelegate, parameters);

                        }
                        else if (methodName.EndsWith("Terminated", false, CultureInfo.InvariantCulture))
                        {
                            SessionChangeSubscription subscription;
                            if (!sessionTerminatedSubscriptions.TryGetValue(serviceDelegateType, out subscription))
                            {
                                subscription = new SessionChangeSubscription(serviceDelegateType);
                                sessionTerminatedSubscriptions.Add(serviceDelegateType, subscription);

                                if (!CheckIfSubscriptionInterfaceIsRegistered(serviceDelegateType))
                                {
                                    throw new InvalidOperationException($"Cannot subscribe session events for {serviceDelegateType.FullName}! No registration was found in the assigned container hosts. Are you missing a RegisterRemoteService<{serviceDelegateType.Name}>()?");
                                }
                            }

                            subscription.AddDelegate(sessionDelegate, parameters);
                        }
                        else
                        {
                            throw new NotSupportedException($"The Action subscription method name \"{methodName}\" is not supported! Method name must end either with \"Created\" or \"Terminated\".");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected out parameter in \"{instance.GetType().FullName}\" constructor! Only delegate types (e.g. Action<IMyService>) are allowed.");
                    }
                }
            }
        }

        private bool CheckIfSubscriptionInterfaceIsRegistered(Type serviceDelegateType)
        {
            if (localSharedInterfaceTypes?.IndexOf(serviceDelegateType) >= 0)
            {
                return true;
            }

            foreach (var host in assignedHosts)
            {
                if (Array.IndexOf(host.RemoteServiceInterfaceTypes, serviceDelegateType) >= 0)
                {
                    return true;
                }

                if (Array.IndexOf(host.LocalServiceInterfaceTypes, serviceDelegateType) >= 0)
                {
                    return true;
                }
            }

            if (manualManagedServiceInterfaceTypes != null
                && manualManagedServiceInterfaceTypes.Contains(serviceDelegateType))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Creates a single instance of the local implmentation of the given interface.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface service type</typeparam>
        public void RegisterLocalSharedService<InterfaceType>()
        {
            RegisterLocalSharedService(typeof(InterfaceType));
        }

        /// <summary>
        /// Creates a single instance of the local implmentation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedService(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given service type must be an interface!", nameof(interfaceType));

            if (localSharedInterfaceTypes == null)
                localSharedInterfaceTypes = new List<Type>();

            if (localMultipleSharedInterfaceTypes != null && localMultipleSharedInterfaceTypes.Contains(interfaceType))
                throw new InvalidOperationException("You cannot register single and mutliple shared implementation instances for the interface type: " + interfaceType.FullName);

            localSharedInterfaceTypes.Add(interfaceType);
        }

        /// <summary>
        /// Creates a instance for every local implementation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedServices<T>()
        {
            RegisterLocalSharedServices(typeof(T));
        }

        /// <summary>
        /// Creates a instance for every local implementation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedServices(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given service type must be an interface!", nameof(interfaceType));

            if (localMultipleSharedInterfaceTypes == null)
                localMultipleSharedInterfaceTypes = new List<Type>();

            if (localSharedInterfaceTypes != null && localSharedInterfaceTypes.Contains(interfaceType))
                throw new InvalidOperationException("You cannot register single and mutliple shared implementation instances for the interface type: " + interfaceType.FullName);

            localMultipleSharedInterfaceTypes.Add(interfaceType);
        }


        public void RegisterLocalSharedService<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            Type type = typeof(T);

            RegisterLocalSharedService(type, instance);
        }

        public void RegisterLocalSharedService(Type type, object instance)
        {
            object existingService;
            if (sharedLocalInstances.TryGetValue(type, out existingService))
            {
                if (!object.ReferenceEquals(instance, existingService))
                {
                    throw new InvalidOperationException($"Only one local shared singelton service for the interface {type.FullName} is allowed!");
                }
            }
            else
            {
                sharedLocalInstances.Add(type, instance);

                // check other interface implementations within the same shared service
                Type[] allInterfaceTypes = instance.GetType().GetInterfaces();

                foreach (var interfType in allInterfaceTypes)
                {
                    if (!sharedLocalInstances.ContainsKey(interfType)
                        && localSharedInterfaceTypes?.IndexOf(interfType) >= 0)
                    {
                        // register other expected and implemented interface as well
                        sharedLocalInstances.Add(interfType, instance);
                    }
                }
            }
        }


        public void RegisterManualManagedService(Type interfaceType)
        {
            if (manualManagedServiceInterfaceTypes == null)
                manualManagedServiceInterfaceTypes = new List<Type>();

            if (!manualManagedServiceInterfaceTypes.Contains(interfaceType))
                manualManagedServiceInterfaceTypes.Add(interfaceType);
        }

        public void RegisterManualManagedService<T>()
        {
            RegisterManualManagedService(typeof(T));
        }

        public void RaiseManualServiceCreated<T>(T serviceInstance)
        {
            Type type = typeof(T);

            if (manualManagedServiceInterfaceTypes != null
                && manualManagedServiceInterfaceTypes.Contains(type))
            {
                TryRaiseInMemorySessionCreated(type, serviceInstance);
            }
            else
            {
                throw new InvalidOperationException($"No manual mangaged service registration for {type.FullName}");
            }
        }


        internal bool TryRaiseInMemorySessionCreated(Type serviceType, object serviceInstance)
        {
            return TryRaiseInMemorySessionCreated(serviceType, serviceInstance, null, null);
        }

        internal bool TryRaiseInMemorySessionCreated(Type serviceType, object serviceInstance, TalkCompositionHost host, IGenericCommunicationService communicationService)
        {
            SessionChangeSubscription subscr;
            if (sessionCreatedSubscriptions.TryGetValue(serviceType, out subscr))
            {
                if (inMemorySession == null)
                {
                    inMemorySession = new Session(communicationService, 0, "In-Memory Session");
                    inMemorySessionContract = new SessionContract(host, inMemorySession, new object[0], new object[0]);
                }

                subscr.Invoke(serviceInstance, inMemorySessionContract, inMemorySession);
                return true;
            }
            else
            {
                return false;
            }
        }


        //public void InitAssignedHosts()
        //{
        //    foreach (var host in assignedHosts)
        //    {
        //        host.InitGenericCommunication(host.CommunicationService, false);
        //    }

        //    CreateLocalSharedServices(assignedHosts[0]);
        //}


        public void CreateLocalSharedServices(TalkCompositionHost source)
        {
            if (this.LocalSharedInterfaceTypes != null)
            {
                // create single instance of registered shared local service types
                foreach (Type servicetype in this.LocalSharedInterfaceTypes)
                {
                    if (this.SharedLocalInstances.ContainsKey(servicetype))
                        continue;   // service instance already craeted

                    object sharedinstance = source.GetExport(servicetype);
                    RegisterLocalSharedService(servicetype, sharedinstance);
                }

                // call in-memory state change callback subscriptions for local global instances
                foreach (var item in this.SharedLocalInstances)
                {
                    this.TryRaiseInMemorySessionCreated(item.Key, item.Value, source, source.CommunicationService);
                }
            }
        }
    }
}
