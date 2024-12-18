﻿using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Composition.Fluent;
using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BSAG.IOCTalk.Composition
{
    public class LocalShareContext : ITalkContainer, IContainerSharedByType
    {
        private static readonly string[] IgnoreAssemblyStartNames = new string[] { "System", "Microsoft.", "netstandard", "Mono.", "mscorlib", "api-ms-", "hostfxr", "mscor", "hostfxr", "clrcompression", "clretwrc", "clrjit", "coreclr", "dbgshim", "hostpolicy", "sos", "ucrtbase", "PresentationFramework", "WindowsBase", "PresentationCore", "sni.dll", "SOS.NETCore" };

        private static Dictionary<string, Assembly> globalLoadedAssemblies = new Dictionary<string, Assembly>();

        private List<Type> localMultipleSharedInterfaceTypes;
        private Dictionary<Type, IList> localSharedMultipleInterfaceInstances;
        private List<Type> manualManagedServiceInterfaceTypes;

        private List<Type> localSharedInterfaceTypes;
        private Dictionary<Type, object> sharedLocalInstances = new Dictionary<Type, object>();

        internal Dictionary<Type, SessionChangeSubscription> sessionCreatedSubscriptions = new Dictionary<Type, SessionChangeSubscription>();
        internal Dictionary<Type, SessionChangeSubscription> sessionTerminatedSubscriptions = new Dictionary<Type, SessionChangeSubscription>();

        private List<ITalkContainer> subContainers = new List<ITalkContainer>();
        private Session inMemorySession;
        private SessionContract inMemorySessionContract;

        private List<Assembly> assemblies = new List<Assembly>();
        private List<IDiscoveryCondition> discoveryConditionItems;
        private Dictionary<Type, TypeHierachy> interfaceImplementationMapping = new Dictionary<Type, TypeHierachy>();

        private ILogger logger;
        private bool isInitalized = false;

        private static int localShareAutoNameCounter = 0;
        private string name;

        public LocalShareContext()
        {
            SetAutoNameIfNecessary(null);
        }

        public LocalShareContext(string name = null)
        {
            SetAutoNameIfNecessary(name);
        }


        public IDictionary<Type, object> SharedLocalInstances
        {
            get
            {
                return sharedLocalInstances;
            }
        }

        /// <summary>
        /// Gets the container shared local instances and all of the sub containers.
        /// </summary>
        public IEnumerable<object> SharedLocalInstanceItemTree
        {
            get
            {
                foreach (var item in sharedLocalInstances.Values)
                {
                    yield return item;
                }

                foreach (var subContainer in subContainers)
                {
                    // include sub containers as well (for e.g. scanning ILifetimeSerice)
                    yield return subContainer;

                    if (subContainer is IContainerSharedByType sharedByType)
                    {
                        foreach (var subItem in sharedByType.SharedLocalInstances.Values)
                        {
                            yield return subItem;
                        }
                    }
                }
            }
        }

        public List<Type> LocalSharedInterfaceTypes
        {
            get
            {
                return localSharedInterfaceTypes;
            }
        }

        public IReadOnlyCollection<Assembly> Assemblies
        {
            get
            {
                return assemblies.AsReadOnly();
            }
        }

        public ITalkContainer ParentContainer { get; set; }

        /// <summary>
        /// Gets the functional instance name
        /// </summary>
        public string Name => name;


        /// <summary>
        /// Assigns an interface type to a fixed implementation type. This prevents assembly scanning and improves discovery performance.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface type</typeparam>
        /// <typeparam name="ImplementationType">The implmentation type</typeparam>
        void ITalkContainer.MapInterfaceImplementationType<InterfaceType, ImplementationType>()
        {
            MapInterfaceImplementationType<InterfaceType, ImplementationType>();
        }

        /// <summary>
        /// Assigns an interface type to a fixed implementation type. This prevents assembly scanning and improves discovery performance.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface type</typeparam>
        /// <typeparam name="ImplementationType">The implmentation type</typeparam>
        public MapInterfaceImplementationType<InterfaceType> MapInterfaceImplementationType<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            var typeHierachy = MapInterfaceImplementationTypeInternal(typeof(InterfaceType), typeof(ImplementationType));

            return new MapInterfaceImplementationType<InterfaceType>(this, typeHierachy);
        }

        /// <summary>
        /// Assigns an interface type to a fixed implementation type. This prevents assembly scanning and improves discovery performance.
        /// </summary>
        /// <param name="interfaceType">The interface type</param>
        /// <param name="implementationType">The implmentation type</param>
        /// <exception cref="ArgumentException">Throws if unexpected types are received.</exception>
        public void MapInterfaceImplementationType(Type interfaceType, Type implementationType)
        {
            MapInterfaceImplementationTypeInternal(interfaceType, implementationType);
        }

        private TypeHierachy MapInterfaceImplementationTypeInternal(Type interfaceType, Type implementationType)
        {
            if (interfaceType.IsInterface == false)
                throw new ArgumentException($"Interface type expected. Actual: {interfaceType.FullName}; Share context: {name}", nameof(interfaceType));

            if (implementationType.IsClass == false)
                throw new ArgumentException($"Class type expected. Actual: {interfaceType.FullName}; Share context: {name}", nameof(implementationType));

            var typeHierachy = new TypeHierachy(interfaceType, implementationType);
            
            interfaceImplementationMapping[interfaceType] = typeHierachy;

            return typeHierachy;
        }

        internal TypeHierachy GetInterfaceImplementationTypeHierachy(Type interfaceType)
        {
            return interfaceImplementationMapping[interfaceType];
        }

        #region Assembly management


        public void AddReferencedAssemblies()
        {
            AddLoadedAssemblies();

            AddAssemblyTree(Assembly.GetExecutingAssembly(), assemblies);
            AddAssemblyTree(Assembly.GetEntryAssembly(), assemblies);
            AddAssemblyTree(Assembly.GetCallingAssembly(), assemblies);
        }

        public void AddExecutionDirAssemblies()
        {
            AddLoadedAssemblies();

            var startAssembly = Assembly.GetEntryAssembly();

            if (startAssembly == null)
            {
                // xamarin case
                startAssembly = Assembly.GetExecutingAssembly();
            }

            var location = startAssembly.Location;
            var execDirectory = new DirectoryInfo(System.IO.Path.GetDirectoryName(location));

            foreach (var aFile in execDirectory.GetFiles("*.dll"))
            {
                if (IgnoreAssemblyStartNames.Any(startN => aFile.Name.StartsWith(startN)))
                    continue;

                try
                {
                    AssemblyName aName = AssemblyName.GetAssemblyName(aFile.FullName);

                    Assembly assembly;
                    if (globalLoadedAssemblies.TryGetValue(aName.FullName, out assembly))   // do not load assemblies twice
                    {
                        AddAssembly(assembly);
                    }
                    else
                    {
                        // load assembly in current AppDomain
                        assembly = Assembly.LoadFrom(aFile.FullName);
                        AddAssemblyTree(assembly, assemblies);
                    }
                }
                catch (Exception loadEx)
                {
                    string errMsg = "AddExecutionDirAssemblies ignore assembly with load error:\r\n" + loadEx.ToString();
                    if (logger != null)
                        logger.Warn(errMsg);
                    else
                        Console.WriteLine(errMsg);

                }
            }
        }

        public void AddLoadedAssemblies()
        {
            var alreadyLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in alreadyLoadedAssemblies)
            {
                if (IgnoreAssemblyStartNames.Any(startN => assembly.FullName.StartsWith(startN)))
                    continue;

                AddAssembly(assembly);
            }
        }

        public void AddDiscoveryCondition(IDiscoveryCondition discoveryCondition)
        {
            lock (TalkCompositionHost.syncObj)
            {
                if (discoveryConditionItems == null)
                    discoveryConditionItems = new List<IDiscoveryCondition>();

                if (!discoveryConditionItems.Contains(discoveryCondition))
                    discoveryConditionItems.Add(discoveryCondition);
            }
        }

        public bool AddAssembly(Assembly assembly)
        {
            lock (TalkCompositionHost.syncObj)
            {
                if (assembly != null)
                {
                    string key = assembly.FullName;

                    if (key != null)
                    {
                        Assembly alreadyLoaded = null;
                        if (!globalLoadedAssemblies.TryGetValue(key, out alreadyLoaded))
                        {
                            //try   (hangs up if try catch is unommented) (xamarin: only occurs if init is not executed in the main thread - even tough it is locked)
                            //{
                            globalLoadedAssemblies.Add(key, assembly);
                            //}
                            //catch (NullReferenceException nEx)
                            //{
                            //    // ignore undifinably NullReferenceException in Xamarin context
                            //}
                        }
                        else if (!alreadyLoaded.Equals(assembly))
                        {
                            // use already loaded assembly because of dynamic load problems (https://github.com/dotnet/corefx/issues/21982)
                            assembly = alreadyLoaded;
                        }
                    }

                    if (!assemblies.Contains(assembly))
                    {
                        assemblies.Add(assembly);
                        return true;    // only the first time
                    }

                }
                return false;
            }
        }

        private void AddAssemblyTree(Assembly assembly, List<Assembly> assemblies)
        {

            if (IgnoreAssemblyStartNames.Any(startN => assembly.FullName.StartsWith(startN)))
                return;

            if (AddAssembly(assembly))
            {
                //assemblies.Add(assembly);

                foreach (var assemblyName in assembly.GetReferencedAssemblies())
                {
                    if (IgnoreAssemblyStartNames.Any(startN => assemblyName.FullName.StartsWith(startN)))
                        continue;

                    if (globalLoadedAssemblies.TryGetValue(assemblyName.FullName, out assembly))   // do not load assemblies twice
                    {
                        AddAssemblyTree(assembly, assemblies);
                    }
                    else
                    {
                        Assembly loadAssembly = null;
                        try
                        {
                            loadAssembly = Assembly.Load(assemblyName);
                        }
                        catch (Exception ex)
                        {
                            // suppress nested assembly load exception
                            if (logger != null)
                                logger.Error(ex.ToString());
                            else
                                Console.WriteLine(ex.ToString());
                        }

                        if (loadAssembly != null)
                            AddAssemblyTree(loadAssembly, assemblies);
                    }
                }
            }
        }

        #endregion





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

        public T GetExport<T>()
        {
            return (T)GetExport(typeof(T));
        }

        public object GetExport(Type type)
        {
            return GetExport(type, null, null);
        }

        public object GetExport(Type type, Type injectTargetType)
        {
            return GetExport(type, injectTargetType, null);
        }

        public object GetExport(Type type, Type injectTargetType, List<Type> pendingCreateList)
        {
            object instance;
            if (!TryGetExport(type, injectTargetType, pendingCreateList, out instance))
            {
                if (assemblies.Count == 0)
                {
                    throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! No assemblies loaded. Are you missing a \"AddExecutionDirAssemblies()\" or \"AddReferencedAssemblies()\"? Share context: {name}");
                }
                else
                {
                    if (injectTargetType != null)
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! Inject Target type: {injectTargetType.FullName}; Share context: {name}");
                    else
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"; Share context: {name}");
                }
            }

            return instance;
        }

        public bool TryGetExport(Type type, Type injectTargetType, out object instance)
        {
            return TryGetExport(type, injectTargetType, null, out instance);
        }

        public bool TryGetExport(Type type, Type injectTargetType, List<Type> pendingCreateList, out object instance)
        {
            if (discoveryConditionItems != null)
            {
                DiscoveryContext ctx = new DiscoveryContext(type, injectTargetType);

                foreach (var cond in discoveryConditionItems)
                {
                    if (cond.IsMatching(ctx))
                    {
                        return cond.TargetContainer.TryGetExport(type, injectTargetType, out instance);
                    }
                }
            }

            if (this.SharedLocalInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            //if (hostSessionsSharedInstances.TryGetValue(type, out instance))
            //{
            //    return true;
            //}

            if (ParentContainer != null
                && ParentContainer is IContainerSharedByType parentShared
                && parentShared.TryGetCachedLocalExport(type, out instance))
            {
                return true;
            }

            // create new instance
            Type targetType;
            bool registerTargetInstance = true;
            if (type.IsInterface)
            {
                //var contract = currentContract;
                //if (contract != null)
                //{
                //    if (contract.TryGetSessionInstance(type, out instance))
                //    {
                //        return true;
                //    }
                //}

                if (!TryFindInterfaceImplementation(type, injectTargetType, pendingCreateList, out targetType, out registerTargetInstance))
                {
                    // not found > check if multiple import
                    if (type.GetInterface(typeof(System.Collections.IEnumerable).FullName) != null)
                    {
                        var multiImportColl = this.CollectLocalMultiImports(null, type, injectTargetType, pendingCreateList);
                        if (multiImportColl != null)
                        {
                            instance = multiImportColl;
                            return true;
                        }
                        // todo: else?
                    }
                    else
                    {
                        instance = null;
                        return false;
                    }
                }
            }
            else if (type.IsArray)
            {
                var multiImportColl = this.CollectLocalMultiImports(null, type, injectTargetType, pendingCreateList);
                if (multiImportColl != null)
                {
                    instance = multiImportColl;
                    return true;
                }
                else
                {
                    instance = null;
                    return false;
                }
            }
            else
            {
                targetType = type;
            }

            object[] outParams;
            ParameterInfo[] outParamsInfo;
            instance = TypeService.CreateInstance(targetType, DetermineConstructorImportInstance, pendingCreateList, out outParams, out outParamsInfo);
            this.CheckOutParamsSubscriptions(instance, outParams, null, type, injectTargetType);

            if (registerTargetInstance)
                this.RegisterSharedConstructorInstances(type, instance, outParams, outParamsInfo);

            return true;
        }

        internal bool TryFindInterfaceImplementation(Type interfaceType, Type injectTargetType, List<Type> pendingCreateList, out Type targetType, out bool registerTargetInstance)
        {
            if (interfaceImplementationMapping.TryGetValue(interfaceType, out var targetTypeHierachy))
            {
                targetType = targetTypeHierachy.GetNextImplementationType(injectTargetType, pendingCreateList, out registerTargetInstance);
                return true;
            }

            registerTargetInstance = true;
            if (injectTargetType != null)
            {
                // first scan inject assembly
                targetType = ScanAssembly(interfaceType, injectTargetType.Assembly);

                if (targetType != null && targetType != injectTargetType)
                    return true;
            }

            foreach (var a in this.assemblies)
            {
                targetType = ScanAssembly(interfaceType, a);

                if (targetType != null && targetType != injectTargetType)
                    return true;    // todo: choose next implementation to interface hierachy
            }

            if (ParentContainer is LocalShareContext parentLsc)
            {
                return parentLsc.TryFindInterfaceImplementation(interfaceType, injectTargetType, pendingCreateList, out targetType, out registerTargetInstance);
            }
            else
            {
                targetType = null;
                return false;
            }
        }

        private static Type ScanAssembly(Type interfaceType, Assembly assembly)
        {
            Type targetType = null;
            try
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (interfaceType.IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        targetType = t;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // ignore loading error
                Console.WriteLine($"Error iterating types for assembly: {assembly}:\n{ex}");
            }

            return targetType;
        }

        internal object DetermineConstructorImportInstance(Type type, string parameterName, Type injectTargetType, List<Type> pendingCreateList)
        {
            return GetExport(type, injectTargetType, pendingCreateList);
        }

        internal Type[] FindInterfaceImplementations(Type interfaceType, Type injectTargetType, List<Type> pendingCreateList, out bool registerTargetInstance)
        {
            bool skipSearch = false;
            if (interfaceImplementationMapping.TryGetValue(interfaceType, out var targetTypeHierachy))
            {
                if (targetTypeHierachy.TryGetImplementationTypes(injectTargetType, pendingCreateList, out var hierarchyImplementations))
                {
                    skipSearch = true;
                    registerTargetInstance = false;
                    return hierarchyImplementations;
                }
            }

            List<Type> validTypes = new List<Type>();
            if (skipSearch == false)
            {
                foreach (var a in this.assemblies)
                {
                    Type[] types = null;
                    try
                    {
                        types = a.GetTypes();
                    }
                    catch (Exception ex)
                    {
                        // ignore loading error
                        Console.WriteLine($"Error iterating types for assembly: {a}:\n{ex}");
                    }

                    if (types != null)
                    {
                        foreach (var t in types)
                        {
                            if (interfaceType.IsAssignableFrom(t) && !t.IsAbstract)
                            {
                                validTypes.Add(t);
                            }
                        }
                    }
                }
            }
            registerTargetInstance = true;
            return validTypes.ToArray();
        }

        public void AddSubContainer(ITalkContainer container)
        {
            if (!subContainers.Contains(container))
            {
                subContainers.Add(container);

                if (container.ParentContainer != null)
                {
                    if (!container.ParentContainer.Equals(this))
                    {
                        throw new InvalidOperationException($"Different sub container already registered!; Share context: {name}");
                    }
                }
                else
                {
                    container.ParentContainer = this;
                }
            }
        }


        #region Multi Shared

        internal bool IsMultiShared(Type interfaceType)
        {
            return localMultipleSharedInterfaceTypes != null && localMultipleSharedInterfaceTypes.Contains(interfaceType);
        }


        internal IEnumerable CollectLocalMultiImports(TalkCompositionHost host, Type type, Type injectTargetType, List<Type> pendingCreateList)
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
                        foreach (Type implType in this.FindInterfaceImplementations(targetInterfaceType, injectTargetType, pendingCreateList, out bool registerSharedInstances))
                        {
                            if (!createdTypes.Contains(implType) && injectTargetType != implType)
                            {
                                object[] outParams;
                                ParameterInfo[] outParamsInfo;
                                object itemInstance;
                                if (host != null)
                                    itemInstance = TypeService.CreateInstance(implType, host.DetermineConstructorImportInstance, pendingCreateList, out outParams, out outParamsInfo);
                                else
                                    itemInstance = TypeService.CreateInstance(implType, this.DetermineConstructorImportInstance, pendingCreateList, out outParams, out outParamsInfo);

                                CheckOutParamsSubscriptions(itemInstance, outParams, host, targetInterfaceType, injectTargetType);
                                targetCollection.Add(itemInstance);

                                if (registerSharedInstances)
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



        internal void CheckOutParamsSubscriptions(object instance, object[] outParams, TalkCompositionHost host, Type targetInterface, Type injectTargetType)
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
                                    throw new InvalidOperationException($"Cannot subscribe session events for {serviceDelegateType.FullName} in {injectTargetType}! No registration was found in the assigned container hosts. Are you missing a RegisterRemoteService<{serviceDelegateType.Name}>()? Share context: {name}");
                                }
                            }

                            if (host != null && host.IsSessionInstance(targetInterface, out ISession session))
                            {
                                subscription.AddDelegate(sessionDelegate, parameters, session);
                            }
                            else
                            {
                                subscription.AddDelegate(sessionDelegate, parameters, null);
                            }
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
                                    throw new InvalidOperationException($"Cannot subscribe session events for {serviceDelegateType.FullName} in {injectTargetType}! No registration was found in the assigned container hosts. Are you missing a RegisterRemoteService<{serviceDelegateType.Name}>()? Share context: {name}");
                                }
                            }

                            if (host != null && host.IsSessionInstance(targetInterface, out ISession session))
                            {
                                subscription.AddDelegate(sessionDelegate, parameters, session);
                            }
                            else
                            {
                                subscription.AddDelegate(sessionDelegate, parameters, null);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"The Action subscription method name \"{methodName}\" is not supported! Method name must end either with \"Created\" or \"Terminated\". Share context: {name}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected out parameter in \"{instance.GetType().FullName}\" constructor! Only delegate types (e.g. Action<IMyService>) are allowed. Share context: {name}");
                    }
                }
            }
        }

        private bool CheckIfSubscriptionInterfaceIsRegistered(Type serviceDelegateType)
        {
            if (IsSubscriptionRegistered(serviceDelegateType))
            {
                return true;
            }

            foreach (var subContainer in subContainers)
            {
                if (subContainer.IsSubscriptionRegistered(serviceDelegateType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSubscriptionRegistered(Type serviceDelegateType)
        {
            if (localSharedInterfaceTypes?.IndexOf(serviceDelegateType) >= 0)
            {
                return true;
            }

            if (manualManagedServiceInterfaceTypes != null
                && manualManagedServiceInterfaceTypes.Contains(serviceDelegateType))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Registers a single instance of the local implmentation for the given interface.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface service type</typeparam>
        public void RegisterLocalSharedService<InterfaceType>()
        {
            RegisterLocalSharedService(typeof(InterfaceType));
        }

        /// <summary>
        /// Registers a single instance of the local implmentation for the given interface and a specific implementation type.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface service type</typeparam>
        /// <typeparam name="ImplementationType">The implementation type</typeparam>
        public LocalSharedRegistration<InterfaceType> RegisterLocalSharedService<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            LocalSharedRegistration<InterfaceType> fluentHelper = new LocalSharedRegistration<InterfaceType>(this);

            MapInterfaceImplementationType<InterfaceType, ImplementationType>();

            RegisterLocalSharedService(typeof(InterfaceType));

            return fluentHelper;
        }

        /// <summary>
        /// Creates a single instance of the local implmentation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedService(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Given service type must be an interface! Share context: {name}", nameof(interfaceType));

            if (localSharedInterfaceTypes == null)
                localSharedInterfaceTypes = new List<Type>();

            if (localMultipleSharedInterfaceTypes != null && localMultipleSharedInterfaceTypes.Contains(interfaceType))
                throw new InvalidOperationException($"You cannot register single and mutliple shared implementation instances for the interface type: {interfaceType.FullName}; Share context: {name}");

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
                throw new ArgumentException($"Given service type must be an interface! Share context: {name}", nameof(interfaceType));

            if (localMultipleSharedInterfaceTypes == null)
                localMultipleSharedInterfaceTypes = new List<Type>();

            if (localSharedInterfaceTypes != null && localSharedInterfaceTypes.Contains(interfaceType))
                throw new InvalidOperationException($"You cannot register single and mutliple shared implementation instances for the interface type: {interfaceType.FullName}; Share context: {name}");

            localMultipleSharedInterfaceTypes.Add(interfaceType);
        }


        public void RegisterLocalSharedService<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance", $"Instance argument cannot be null! Share context: {name}");

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
                    throw new InvalidOperationException($"Only one local shared singelton service for the interface {type.FullName} is allowed! Share context: {name}");
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
                throw new InvalidOperationException($"No manual mangaged service registration for {type.FullName}; Share context: {name}");
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
                    inMemorySession = new Session(communicationService, 0, "In-Memory Session", null);
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

        public void Init(bool initSubContainers = true)
        {
            lock (TalkCompositionHost.syncObj)
            {
                if (!isInitalized)  // only inialize once
                {
                    isInitalized = true;

                    CreateLocalSharedServices();

                    if (initSubContainers)
                    {
                        foreach (var sc in subContainers)
                        {
                            sc.Init(initSubContainers);
                        }
                    }

                    if (TryGetCachedLocalExport(typeof(ILogger), out var log))
                    {
                        this.logger = (ILogger)log;
                    }
                }
            }
        }


        //private void CreateLocalSharedServices(TalkCompositionHost source)    // todo: check if subscriptions work without composition host
        private void CreateLocalSharedServices()
        {
            if (this.LocalSharedInterfaceTypes != null)
            {
                // create single instance of registered shared local service types
                foreach (Type servicetype in this.LocalSharedInterfaceTypes)
                {
                    if (this.SharedLocalInstances.ContainsKey(servicetype))
                        continue;   // service instance already craeted

                    object sharedinstance = this.GetExport(servicetype);
                    RegisterLocalSharedService(servicetype, sharedinstance);
                }

                // call in-memory state change callback subscriptions for local global instances
                foreach (var item in this.SharedLocalInstances)
                {
                    this.TryRaiseInMemorySessionCreated(item.Key, item.Value);
                }
            }
        }

        private void SetAutoNameIfNecessary(string name)
        {
            if (name is null)
            {
                this.name = "ioctalkShare" + Interlocked.Increment(ref localShareAutoNameCounter);
            }
            else
            {
                this.name = name;
            }
        }


    }
}
