using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Composition.Condition;
using BSAG.IOCTalk.Composition.Fluent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BSAG.IOCTalk.Composition
{
    public class TalkCompositionHost : IGenericContainerHost, ITalkContainer //IGenericContractContainerHost<TServiceContractSession> //, IXmlConfig
                                                                             //where TServiceContractSession : class
    {
        #region TalkCompositionHost fields
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost fields
        // ----------------------------------------------------------------------------------------
        private static readonly string[] IgnoreAssemblyStartNames = new string[] { "System", "Microsoft.", "netstandard", "Mono.", "mscorlib", "api-ms-", "hostfxr", "mscor", "hostfxr", "clrcompression", "clretwrc", "clrjit", "coreclr", "dbgshim", "hostpolicy", "sos", "ucrtbase", "PresentationFramework", "WindowsBase", "PresentationCore", "sni.dll" };

        private SessionManagerNeu sessionManager = new SessionManagerNeu();
        //private SessionExportDescriptorProvider sessionExportProvider = new SessionExportDescriptorProvider();
        private List<Assembly> assemblies = new List<Assembly>();
        private List<IDiscoveryCondition> discoveryConditionItems;
        private static Dictionary<string, Assembly> globalLoadedAssemblies = new Dictionary<string, Assembly>();

        private List<Type> localSessionServiceInterfaceTypes = new List<Type>();

        private Type[] localSessionServiceInterfaceTypesResolved;
        private List<Type> remoteServiceInterfaceTypes = new List<Type>();
        private List<Type> remoteServiceProxyTypes = new List<Type>();
        private Type[] remoteServiceInterfaceTypesResolved;


        private Dictionary<Type, Type> interfaceTypeProxyImplCache = new Dictionary<Type, Type>();
        private Dictionary<string, Type> interfaceTypeCache;
        private Dictionary<int, SessionContract> sessionIdContractMapping = new Dictionary<int, SessionContract>();
        private LocalShareContext localShare;
        private Dictionary<Type, object> hostSessionsSharedInstances = new Dictionary<Type, object>();
        private static object syncObj = new object();
        private Dictionary<Type, Type> exposedSubInterfaceTypeMapping;
        private Dictionary<Type, Type> exposedSubInterfaceTypeMappingInterfToClass;
        private Dictionary<Type, List<string>> asyncMethods = null;
        private ILogger logger;
        private ISession currentSession;
        private IContract currentContract;


        // ----------------------------------------------------------------------------------------
        #endregion


        #region TalkCompositionHost constructors
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost constructors
        // ----------------------------------------------------------------------------------------

        public TalkCompositionHost()
        {
            // create own local share for remote connection
            localShare = new LocalShareContext();
            localShare.AssignHost(this);
        }

        public TalkCompositionHost(LocalShareContext localShareContext)
        {
            this.localShare = localShareContext;
            localShare.AssignHost(this);
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkCompositionHost properties
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost properties
        // ----------------------------------------------------------------------------------------

        //public TServiceContractSessionManager SessionManager => sessionManager;

        public object DIContainer => this;

        public SessionManagerNeu SessionManager => sessionManager;

        public Type[] LocalServiceInterfaceTypes
        {
            get { return localSessionServiceInterfaceTypesResolved; }
        }

        /// <summary>
        /// Interface mapping to export a single session instance in multiple interfaces (TargetAlsoImplements)
        /// </summary>
        internal Dictionary<Type, Type> LocalSessionServiceTypeMappings { get; set; }
        
        public Type[] RemoteServiceInterfaceTypes
        {
            get { return remoteServiceInterfaceTypesResolved; }
        }

        public IGenericCommunicationService CommunicationService { get; private set; }
        
        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkCompositionHost events
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when [session created].
        /// </summary>
        public event SessionEventHandler<object> SessionCreated;

        /// <summary>
        /// Occurs when [session terminated].
        /// </summary>
        public event SessionEventHandler<object> SessionTerminated;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkCompositionHost methods
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost methods
        // ----------------------------------------------------------------------------------------

        public RemoteServiceRegistration<InterfaceType> RegisterRemoteService<InterfaceType>()
            where InterfaceType : class
        {
            RegisterRemoteService(typeof(InterfaceType));

            return new RemoteServiceRegistration<InterfaceType>(this);
        }

        public void RegisterRemoteService(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given service type must be an interface!", nameof(interfaceType));

            if (localSessionServiceInterfaceTypesResolved != null)
                throw new Exception($"{GetType().FullName} is already initialized.");

            lock (syncObj)
            {
                // try get auto generated proxy implementation type from cache
                Type proxyImplementationType;
                if (!interfaceTypeProxyImplCache.TryGetValue(interfaceType, out proxyImplementationType))
                {
                    if (!TryFindInterfaceImplementation(interfaceType, null, out proxyImplementationType))
                    {
                        // auto generate proxy
                        proxyImplementationType = TypeService.BuildProxyImplementation(interfaceType); // "[System.Composition.Import]");
                        interfaceTypeProxyImplCache.Add(interfaceType, proxyImplementationType);
                    }

                    AddAssembly(proxyImplementationType.Assembly);
                }

                remoteServiceInterfaceTypes.Add(interfaceType);
                remoteServiceProxyTypes.Add(proxyImplementationType);
            }
        }

        public LocalSessionRegistration<InterfaceType> RegisterLocalSessionService<InterfaceType>()
        {
            LocalSessionRegistration<InterfaceType> localReg = new LocalSessionRegistration<InterfaceType>(this);
            RegisterLocalSessionService(typeof(InterfaceType));

            return localReg;
        }

        public void RegisterLocalSessionService(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given service type must be an interface!", nameof(interfaceType));

            if (remoteServiceInterfaceTypesResolved != null)
                throw new Exception($"{GetType().FullName} is already initialized.");

            localSessionServiceInterfaceTypes.Add(interfaceType);
        }

        public void RegisterHostSessionsSharedInstance<InterfaceType>(InterfaceType instance)
        {
            RegisterHostSessionsSharedInstance(typeof(InterfaceType), instance);
        }

        public void RegisterHostSessionsSharedInstance(Type interfaceType, object instance)
        {
            hostSessionsSharedInstances.Add(interfaceType, instance);
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
            localShare.RegisterLocalSharedService(interfaceType);
        }

        /// <summary>
        /// Creates a instance for every local implementation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedServices<T>()
        {
            localShare.RegisterLocalSharedServices<T>();
        }

        /// <summary>
        /// Creates a instance for every local implementation of the given interface.
        /// </summary>
        /// <param name="interfaceType"></param>
        public void RegisterLocalSharedServices(Type interfaceType)
        {
            localShare.RegisterLocalSharedServices(interfaceType);
        }

        public void RegisterLocalSharedService<T>(T instance)
        {
            localShare.RegisterLocalSharedService<T>(instance);
        }

        public void RegisterLocalSharedService(Type type, object instance)
        {
            localShare.RegisterLocalSharedService(type, instance);
        }

        public void InitGenericCommunication(IGenericCommunicationService communicationService)
        {
            this.InitGenericCommunication(communicationService, true);
        }

        public void InitGenericCommunication(IGenericCommunicationService communicationService, bool createSharedInstances)
        {
            if (remoteServiceInterfaceTypesResolved != null)
            {
                throw new Exception($"{GetType().FullName} is already initialized.");
            }

            this.CommunicationService = communicationService;

            this.remoteServiceInterfaceTypesResolved = remoteServiceInterfaceTypes.ToArray();
            this.localSessionServiceInterfaceTypesResolved = localSessionServiceInterfaceTypes.ToArray();

            if (communicationService != null)   // todo: check if good solution for local only
            {
                communicationService.SessionCreated += OnCommunicationService_SessionCreated;
                communicationService.SessionTerminated += OnCommunicationService_SessionTerminated;

                RegisterHostSessionsSharedInstance(communicationService);

                // init dependency injection

                // register communication host for response processing
                communicationService.RegisterContainerHost(this);

                // export ILogger
                RegisterLocalSharedService<ILogger>(communicationService.Logger);
                this.logger = communicationService.Logger;
            }

            if (createSharedInstances)
                localShare.CreateLocalSharedServices(this);
        }



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
                        assembly = Assembly.LoadFile(aFile.FullName);
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
            lock (syncObj)
            {
                if (discoveryConditionItems == null)
                    discoveryConditionItems = new List<IDiscoveryCondition>();

                if (!discoveryConditionItems.Contains(discoveryCondition))
                    discoveryConditionItems.Add(discoveryCondition);
            }
        }

        public bool AddAssembly(Assembly assembly)
        {
            lock (syncObj)
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

        public IContract CreateSessionContractInstance(ISession session)
        {
            SessionContract sessionContract = null;
            lock (syncObj)
            {
                try
                {
                    currentSession = session;

                    object[] remoteServiceInstances = new object[this.remoteServiceInterfaceTypes.Count];
                    object[] localServiceInstances = new object[this.localSessionServiceInterfaceTypes.Count];
                    sessionContract = new SessionContract(this, session, localServiceInstances, remoteServiceInstances);
                    currentContract = sessionContract;

                    for (int i = 0; i < this.remoteServiceInterfaceTypes.Count; i++)
                    {
                        // get instance of service proxy implementation (remote call)
                        Type remoteProxyType = this.remoteServiceProxyTypes[i];

                        try
                        {
                            remoteServiceInstances[i] = this.GetExport(remoteProxyType);
                        }
                        catch (Exception exportException)
                        {
                            throw new TypeLoadException($"Unable to export instance of remote proxy type {remoteProxyType}!", exportException);
                        }
                    }

                    for (int i = 0; i < this.localSessionServiceInterfaceTypes.Count; i++)
                    {
                        // get instance of local service implementation
                        Type localType = this.localSessionServiceInterfaceTypes[i];

                        try
                        {
                            localServiceInstances[i] = this.GetExport(localType);
                        }
                        catch (Exception exportException)
                        {
                            throw new TypeLoadException($"Unable to export instance of local type {localType}!", exportException);
                        }
                    }

                    sessionIdContractMapping.Add(session.SessionId, sessionContract);
                }
                finally
                {
                    currentSession = null;
                    currentContract = null;
                }
            }

            return sessionContract;
        }


        private void OnCommunicationService_SessionCreated(object sender, SessionEventArgs e)
        {
            try
            {
                CheckSessionCreatedActions(e.Session, e.SessionContract);

                SessionCreated?.Invoke(e.SessionContract, e);
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Error(ex.ToString());
                else
                    Console.WriteLine(ex.ToString());
            }
        }

        private void OnCommunicationService_SessionTerminated(object sender, SessionEventArgs e)
        {
            try
            {
                CheckSessionTerminatedActions(e.Session, e.SessionContract);

                SessionTerminated?.Invoke(e.SessionContract, e);
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Error(ex.ToString());
                else
                    Console.WriteLine(ex.ToString());
            }
        }



        /// <summary>
        /// Gets the type of the exposed sub interface.
        /// </summary>
        /// <param name="sourceType">Type of the concrete source.</param>
        /// <returns>
        /// Returns null if no sub interface is exposed
        /// </returns>
        public Type GetExposedSubInterfaceForType(Type sourceType)
        {
            Type exposedInterfaceType = null;
            if (exposedSubInterfaceTypeMapping != null)
            {
                exposedSubInterfaceTypeMapping.TryGetValue(sourceType, out exposedInterfaceType);
            }
            return exposedInterfaceType;
        }

        /// <summary>
        /// Registers the type mapping for an exposed sub interface.
        /// </summary>
        /// <typeparam name="InterfaceType">Type of the exposed derived interface.</typeparam>
        /// <typeparam name="SourceType">Type of the concrete source class.</typeparam>
        public void RegisterExposedSubInterfaceForType<InterfaceType, SourceType>()
        {
            RegisterExposedSubInterfaceForType(typeof(InterfaceType), typeof(SourceType));
        }


        /// <summary>
        /// Registers the type mapping for an exposed sub interface.
        /// </summary>
        /// <param name="interfaceType">Type of the exposed interface.</param>
        /// <param name="sourceType">Type of the concrete source class.</param>
        public void RegisterExposedSubInterfaceForType(Type interfaceType, Type sourceType)
        {
            if (exposedSubInterfaceTypeMapping == null)
            {
                exposedSubInterfaceTypeMapping = new Dictionary<Type, Type>();
                exposedSubInterfaceTypeMappingInterfToClass = new Dictionary<Type, Type>();
            }

            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException(string.Format("The exposed sub type \"{0}\" must be an interface!", interfaceType.FullName));
            }

            exposedSubInterfaceTypeMapping[sourceType] = interfaceType;

            if (sourceType.IsClass)
            {
                exposedSubInterfaceTypeMappingInterfToClass[interfaceType] = sourceType;
            }
        }


        public object GetInterfaceImplementationInstance(ISession session, string interfaceType)
        {
            object result = null;

            SessionContract sessionContract;
            if (sessionIdContractMapping.TryGetValue(session.SessionId, out sessionContract))
            {
                result = sessionContract.GetInterfaceImplementationInstance(interfaceType);
            }

            return result;
        }

        public Type GetInterfaceImplementationType(string interfaceType)
        {
            if (interfaceTypeCache == null)
                interfaceTypeCache = new Dictionary<string, Type>();

            Type implementationType = null;
            if (!interfaceTypeCache.TryGetValue(interfaceType, out implementationType))
            {
                Type interfType;
                if (TypeService.TryGetTypeByName(interfaceType, out interfType))
                {
                    // check if a special sub type mapping resolver is registered
                    if (exposedSubInterfaceTypeMappingInterfToClass != null)
                    {
                        Type exposedType;
                        if (exposedSubInterfaceTypeMappingInterfToClass.TryGetValue(interfType, out exposedType))
                        {
                            return exposedType;
                        }
                    }

                    // check if a local export is present for the type
                    object instance;
                    if (TryGetExport(interfType, out instance))
                    {
                        return instance.GetType();
                    }
                    else
                    {
                        // No implementation found
                        // Try auto generate type
                        Type autoGeneratedType = TypeService.BuildInterfaceImplementationType(interfaceType);
                        if (autoGeneratedType != null)
                        {
                            interfaceTypeCache[interfaceType] = autoGeneratedType;
                            return autoGeneratedType;
                        }
                        else
                        {
                            throw new Exception("Unable to auto generate a class implemenation for the interface \"" + interfaceType + "\"! Provide a local implementation.");
                        }
                    }
                }
                else
                {
                    throw new TypeLoadException($"Coult not load type {interfaceType}!");
                }
            }

            return implementationType;
        }

        public void RegisterCustomInterfaceMapping<InterfaceType, ImplementationType>()
        {
            if (interfaceTypeCache == null)
                interfaceTypeCache = new Dictionary<string, Type>();

            Type interfaceType = typeof(InterfaceType);

            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given InterfaceType is not an interface!");

            interfaceTypeCache.Add(interfaceType.FullName, typeof(ImplementationType));
        }

        public ISession GetSessionByServiceInstance(object serviceObjectInstance)
        {
            throw new NotSupportedException("Not supported anymore in .net core implementation - use session contract mapping instead");
        }


        public T GetExport<T>()
        {
            return (T)GetExport(typeof(T));
        }

        public object GetExport(Type type)
        {
            return GetExport(type, null);
        }

        public object GetExport(Type type, Type injectTargetType)
        {
            object instance;
            if (!TryGetExport(type, injectTargetType, out instance))
            {
                if (assemblies.Count == 0)
                {
                    throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! No assemblies loaded. Are you missing a \"AddExecutionDirAssemblies()\" or \"AddReferencedAssemblies()\"?");
                }
                else
                {
                    if (injectTargetType != null)
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! Inject Target type: {injectTargetType.FullName}");
                    else
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"");
                }
            }

            return instance;
        }

        public bool TryGetExport(Type type, out object instance)
        {
            return TryGetExport(type, null, out instance);
        }

        public bool TryGetExport(Type type, Type injectTargetType, out object instance)
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

            if (localShare.SharedLocalInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            if (hostSessionsSharedInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            Type targetType;
            if (type.IsInterface)
            {
                var contract = currentContract;
                if (contract != null)
                {
                    if (contract.TryGetSessionInstance(type, out instance))
                    {
                        return true;
                    }
                }

                if (!TryFindInterfaceImplementation(type, injectTargetType, out targetType))
                {
                    // not found > check if multiple import
                    if (type.GetInterface(typeof(System.Collections.IEnumerable).FullName) != null)
                    {
                        var multiImportColl = localShare.CollectLocalMultiImports(this, type, injectTargetType);
                        if (multiImportColl != null)
                        {
                            instance = multiImportColl;
                            return true;
                        }
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
                var multiImportColl = this.localShare.CollectLocalMultiImports(this, type, injectTargetType);
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
            instance = TypeService.CreateInstance(targetType, DetermineConstructorImportInstance, out outParams, out outParamsInfo);
            localShare.CheckOutParamsSubscriptions(instance, outParams);

            localShare.RegisterSharedConstructorInstances(type, instance, outParams, outParamsInfo);

            return true;
        }

        internal object DetermineConstructorImportInstance(Type type, string parameterName, Type injectTargetType)
        {
            if (string.Compare(parameterName, "sessionId", true) == 0)
            {
                // return current session id
                return currentSession.SessionId;
            }
            else
            {
                return GetExport(type, injectTargetType);
            }
        }

        private bool TryFindInterfaceImplementation(Type interfaceType, Type injectTargetType, out Type targetType)
        {
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

            targetType = null;
            return false;
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

        internal IEnumerable<Type> FindInterfaceImplementations(Type interfaceType, Type injectTargetType)
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
                            yield return t;
                        }
                    }
                }
            }
        }




        private void CheckSessionCreatedActions(ISession session, IContract sessionContract)
        {
            for (int i = 0; i < localSessionServiceInterfaceTypesResolved.Length; i++)
            {
                Type interfType = localSessionServiceInterfaceTypesResolved[i];
                SessionChangeSubscription subscr;
                if (localShare.sessionCreatedSubscriptions.TryGetValue(interfType, out subscr))
                {
                    object serviceInstance = sessionContract.LocalServices[i];
                    subscr.Invoke(serviceInstance, sessionContract, session);
                }
            }

            // check session created subscriptions
            for (int i = 0; i < this.remoteServiceInterfaceTypes.Count; i++)
            {
                Type interfType = remoteServiceInterfaceTypes[i];
                SessionChangeSubscription subscr;
                if (localShare.sessionCreatedSubscriptions.TryGetValue(interfType, out subscr))
                {
                    object serviceInstance = sessionContract.RemoteServices[i];
                    subscr.Invoke(serviceInstance, sessionContract, session);
                }
            }
        }

        private void CheckSessionTerminatedActions(ISession session, IContract sessionContract)
        {
            if (sessionContract != null)
            {
                // check session terminated subscriptions
                for (int i = 0; i < this.localSessionServiceInterfaceTypesResolved.Length; i++)
                {
                    Type interfType = localSessionServiceInterfaceTypesResolved[i];
                    object serviceInstance = sessionContract.LocalServices[i];

                    SessionChangeSubscription subscr;
                    if (localShare.sessionTerminatedSubscriptions.TryGetValue(interfType, out subscr))
                    {
                        subscr.Invoke(serviceInstance, sessionContract, session);

                        // subscription is not needed anymore (local session based service instance is terminated)
                        subscr.RemoveDelegate(serviceInstance);
                        if (localShare.sessionCreatedSubscriptions.TryGetValue(interfType, out subscr))
                        {
                            subscr.RemoveDelegate(serviceInstance);
                        }
                    }

                    if (serviceInstance is IDisposable disposableService)
                    {
                        // call IDispoable implementation
                        disposableService.Dispose();
                    }
                }

                for (int i = 0; i < this.remoteServiceInterfaceTypesResolved.Length; i++)
                {
                    Type interfType = remoteServiceInterfaceTypesResolved[i];
                    object serviceInstance = sessionContract.RemoteServices[i];

                    SessionChangeSubscription subscr;
                    if (localShare.sessionTerminatedSubscriptions.TryGetValue(interfType, out subscr))
                    {
                        subscr.Invoke(serviceInstance, sessionContract, session);
                    }

                    if (serviceInstance is IDisposable disposableService)
                    {
                        // call IDispoable implementation
                        disposableService.Dispose();
                    }
                }
            }
        }

        public void RegisterManualManagedService(Type interfaceType)
        {
            localShare.RegisterManualManagedService(interfaceType);
        }

        public void RegisterManualManagedService<T>()
        {
            RegisterManualManagedService(typeof(T));
        }

        public void RaiseManualServiceCreated<T>(T serviceInstance)
        {
            localShare.RaiseManualServiceCreated<T>(serviceInstance);
        }

        public void RegisterAsyncMethod<InterfaceType>(string methodName)
        {
            lock (syncObj)
            {
                if (asyncMethods == null)
                    asyncMethods = new Dictionary<Type, List<string>>();

                //todo: handle / include command parameters

                List<string> methods;
                if (!asyncMethods.TryGetValue(typeof(InterfaceType), out methods))
                {
                    methods = new List<string>();
                    asyncMethods.Add(typeof(InterfaceType), methods);
                }

                if (!methods.Contains(methodName))
                    methods.Add(methodName);
            }
        }

        public bool IsAsyncRemoteInvoke(Type type, string methodName)
        {
            if (asyncMethods == null)
                return false;

            List<string> methods;
            if (asyncMethods.TryGetValue(type, out methods))
            {
                return methods.Contains(methodName);
            }

            return false;
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }
}
