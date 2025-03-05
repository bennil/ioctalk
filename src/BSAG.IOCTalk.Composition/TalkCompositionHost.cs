using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Composition.Condition;
using BSAG.IOCTalk.Composition.Fluent;
using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace BSAG.IOCTalk.Composition
{
    public class TalkCompositionHost : IGenericContainerHost, ITalkContainer //IGenericContractContainerHost<TServiceContractSession> //, IXmlConfig
                                                                             //where TServiceContractSession : class
    {
        #region TalkCompositionHost fields
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost fields
        // ----------------------------------------------------------------------------------------

        private List<IDiscoveryCondition> discoveryConditionItems;  // todo: check if needed in Host or only in LocalShareContext

        private List<Type> localSessionServiceInterfaceTypes = new List<Type>();

        private Type[] localSessionServiceInterfaceTypesResolved;
        private List<TypeHierachy> remoteServiceInterfaceTypes = new List<TypeHierachy>();
        private List<Type> remoteServiceProxyTypes = new List<Type>();
        private Type[] remoteServiceInterfaceTypesResolved;


        private Dictionary<Type, Type> interfaceTypeProxyImplCache = new Dictionary<Type, Type>();
        private Dictionary<string, Type> interfaceTypeCache;
        private Dictionary<Type, TypeHierachy> interfaceImplementationMapping = new Dictionary<Type, TypeHierachy>();

        private Dictionary<int, SessionContract> sessionIdContractMapping = new Dictionary<int, SessionContract>();
        private LocalShareContext localShare;
        private Dictionary<Type, object> hostSessionsSharedInstances = new Dictionary<Type, object>();
        internal static object syncObj = new object();
        private Dictionary<Type, Type> exposedSubInterfaceTypeMapping;
        private Dictionary<Type, Type> exposedSubInterfaceTypeMappingInterfToClass;
        private Dictionary<Type, List<string>> asyncVoidMethods = null;
        private ILogger logger;
        private ISession currentSession;
        private IContract currentContract;
        private bool isInitalized = false;


        private static int compositionHostAutoNameCounter = 0;
        private string name;

        // ----------------------------------------------------------------------------------------
        #endregion


        #region TalkCompositionHost constructors
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost constructors
        // ----------------------------------------------------------------------------------------

        public TalkCompositionHost(string name = null)
        {
            // create own local share for remote connection
            localShare = new LocalShareContext();
            localShare.AddSubContainer(this);

            SetAutoNameIfNecessary(name);
        }



        public TalkCompositionHost(LocalShareContext localShareContext, string name = null)
        {
            this.localShare = localShareContext;
            localShare.AddSubContainer(this);

            SetAutoNameIfNecessary(name);
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkCompositionHost properties
        // ----------------------------------------------------------------------------------------
        // TalkCompositionHost properties
        // ----------------------------------------------------------------------------------------

        //public TServiceContractSessionManager SessionManager => sessionManager;

        public object DIContainer => this;


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

        public IGenericCommunicationService CommunicationService { get; set; }

        public ITalkContainer ParentContainer
        {
            get { return localShare; }
            set { throw new NotSupportedException("parent container set only by constructor!"); }
        }

        public string Name => name;

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

        public RemoteServiceRegistration<InterfaceType> RegisterRemoteService<InterfaceType>(bool forceProxyAutoCreation = false)
            where InterfaceType : class
        {
            RegisterRemoteService(typeof(InterfaceType), forceProxyAutoCreation);

            return new RemoteServiceRegistration<InterfaceType>(this);
        }

        /// <summary>
        /// Registers a remote service for the given interface
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="forceProxyAutoCreation">Forces proxy auto creation for interface type. No local implementation lookup.</param>
        public void RegisterRemoteService(Type interfaceType, bool forceProxyAutoCreation = false)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Given service type must be an interface! Host: " + name, nameof(interfaceType));

            if (localSessionServiceInterfaceTypesResolved != null)
                throw new Exception($"{GetType().FullName} is already initialized. Host: {name}");

            lock (syncObj)
            {
                // try get auto generated proxy implementation type from cache
                Type proxyImplementationType;
                if (!interfaceTypeProxyImplCache.TryGetValue(interfaceType, out proxyImplementationType))
                {
                    if (forceProxyAutoCreation || !TryFindInterfaceImplementation(interfaceType, null, null, out proxyImplementationType, out _))
                    {
                        // auto generate proxy
                        proxyImplementationType = TypeService.BuildProxyImplementation(interfaceType); // "[System.Composition.Import]");
                        interfaceTypeProxyImplCache.Add(interfaceType, proxyImplementationType);
                    }

                    localShare.AddAssembly(proxyImplementationType.Assembly);
                }

                var interfaceTypeHierachy = MapInterfaceImplementationTypeInternal(interfaceType, proxyImplementationType);

                remoteServiceInterfaceTypes.Add(interfaceTypeHierachy);
                remoteServiceProxyTypes.Add(proxyImplementationType);

            }
        }

        public LocalSessionRegistration<InterfaceType> RegisterLocalSessionService<InterfaceType>()
        {
            LocalSessionRegistration<InterfaceType> localReg = new LocalSessionRegistration<InterfaceType>(this);
            RegisterLocalSessionService(typeof(InterfaceType));

            return localReg;
        }

        public LocalSessionRegistration<InterfaceType> RegisterLocalSessionService<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            MapInterfaceImplementationType<InterfaceType, ImplementationType>();

            return RegisterLocalSessionService<InterfaceType>();
        }

        public void RegisterLocalSessionService(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Given service type must be an interface! Host: {name}", nameof(interfaceType));

            if (remoteServiceInterfaceTypesResolved != null)
                throw new Exception($"{GetType().FullName} is already initialized. Host: {name}");

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
        /// Registers a single instance of the local implmentation for the given interface and a specific implementation type.
        /// </summary>
        /// <typeparam name="InterfaceType">The interface service type</typeparam>
        /// <typeparam name="ImplementationType">The implementation type</typeparam>
        public void RegisterLocalSharedService<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            localShare.RegisterLocalSharedService<InterfaceType, ImplementationType>();
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

        public void Init(bool initSubContainers = false)
        {
            lock (TalkCompositionHost.syncObj)
            {
                if (!isInitalized)  // only inialize once
                {
                    if (CommunicationService == null)
                        throw new NullReferenceException($"{nameof(CommunicationService)} must be provided! Host: {name}");

                    InitGenericCommunication(CommunicationService, true, initSubContainers);
                }
            }
        }

        public void InitGenericCommunication(IGenericCommunicationService communicationService)
        {
            this.InitGenericCommunication(communicationService, true, false);
        }

        public void InitGenericCommunication(IGenericCommunicationService communicationService, bool initShareContext, bool initSubContainers)
        {
            lock (TalkCompositionHost.syncObj)
            {
                if (!isInitalized)  // only inialize once
                {
                    isInitalized = true;
                    this.CommunicationService = communicationService;

                    this.remoteServiceInterfaceTypesResolved = remoteServiceInterfaceTypes.Select(rsi => rsi.InterfaceType).ToArray();
                    this.localSessionServiceInterfaceTypesResolved = localSessionServiceInterfaceTypes.ToArray();

                    communicationService.SessionCreated += OnCommunicationService_SessionCreated;
                    communicationService.SessionTerminated += OnCommunicationService_SessionTerminated;

                    RegisterHostSessionsSharedInstance(communicationService);

                    // init dependency injection
                    // check if logger is already added to share context
                    bool exportNewLogger = false;
                    if (localShare.TryGetCachedLocalExport(typeof(ILogger), out object loggerObj))
                    {
                        this.logger = (ILogger)loggerObj;
                    }
                    else
                    {
                        exportNewLogger = true;
                    }

                    // register communication host for response processing
                    communicationService.RegisterContainerHost(this, logger);

                    if (exportNewLogger)
                    {
                        RegisterLocalSharedService<ILogger>(communicationService.Logger);
                        this.logger = communicationService.Logger;
                    }

                    if (initShareContext)
                        localShare.Init(initSubContainers);
                }
            }
        }

        public void AddReferencedAssemblies()
        {
            localShare.AddReferencedAssemblies();
        }
        public void AddExecutionDirAssemblies()
        {
            localShare.AddExecutionDirAssemblies();
        }
        public void AddLoadedAssemblies()
        {
            localShare.AddLoadedAssemblies();
        }

        public bool AddAssembly(Assembly assembly)
        {
            return localShare.AddAssembly(assembly);
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
                        var remoteProxyOrInterceptedType = remoteServiceInterfaceTypes[i].GetNextImplementationType(sessionContract.GetType(), null, out var _);
                        // get instance of service proxy implementation (remote call)
                        //Type remoteProxyType = this.remoteServiceProxyTypes[i];

                        try
                        {
                            remoteServiceInstances[i] = this.GetExport(remoteProxyOrInterceptedType);
                        }
                        catch (Exception exportException)
                        {
                            throw new TypeLoadException($"Unable to get export instance of remote proxy/intercepted type {remoteProxyOrInterceptedType}! Host: {name}", exportException);
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
                            throw new TypeLoadException($"Unable to export instance of local type {localType}! Host: {name}", exportException);
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

                sessionIdContractMapping.Remove(e.Session.SessionId);
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
                throw new InvalidOperationException($"The exposed sub type \"{interfaceType.FullName}\" must be an interface! Host: {name}");
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
                    if (TryFindInterfaceImplementation(interfType, null, null, out Type implType, out _))
                    {
                        interfaceTypeCache[interfaceType] = implType;   // add string > implementation type mapping
                        return implType;
                    }
                    else if (TryGetExport(interfType, out instance))
                    {
                        var exportType = instance.GetType();
                        interfaceTypeCache[interfaceType] = exportType;
                        return exportType;
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
                            throw new Exception("Unable to auto generate a class implemenation for the interface \"" + interfaceType + "\"! Provide a local implementation. Host: " + name);
                        }
                    }
                }
                else
                {
                    throw new TypeLoadException($"Coult not load type {interfaceType}! Host: {name}");
                }
            }

            return implementationType;
        }

        [Obsolete("Use MapInterfaceImplementationType instead")]
        public void RegisterCustomInterfaceMapping<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            //if (interfaceTypeCache == null)
            //    interfaceTypeCache = new Dictionary<string, Type>();

            //Type interfaceType = typeof(InterfaceType);

            //if (!interfaceType.IsInterface)
            //    throw new ArgumentException("Given InterfaceType is not an interface!");

            //interfaceTypeCache.Add(interfaceType.FullName, typeof(ImplementationType));

            MapInterfaceImplementationType<InterfaceType, ImplementationType>();
        }

        public ISession GetSessionByServiceInstance(object serviceObjectInstance)
        {
            throw new NotSupportedException($"Not supported anymore in .net core implementation - use session contract mapping instead; Host: {name}");
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
                if (localShare.Assemblies.Count == 0)
                {
                    throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! No assemblies loaded. Are you missing a \"AddExecutionDirAssemblies()\" or \"AddReferencedAssemblies()\"? Host: {name}");
                }
                else
                {
                    if (injectTargetType != null)
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"! Inject Target type: {injectTargetType.FullName}; Host: {name}");
                    else
                        throw new TypeAccessException($"Unable to find a local implementation of \"{type.FullName}\"; Host: {name}");
                }
            }

            return instance;
        }

        public bool TryGetExport(Type type, out object instance)
        {
            return TryGetExport(type, null, null, out instance);
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

            if (hostSessionsSharedInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            if (localShare.SharedLocalInstances.TryGetValue(type, out instance))
            {
                return true;
            }

            bool registerTargetInstance = true;
            Type targetType;
            var contract = currentContract;
            if (type.IsInterface)
            {
                if (contract != null)
                {
                    if (contract.TryGetSessionInstance(type, out instance))
                    {
                        return true;
                    }
                }

                if (TryFindInterfaceImplementation(type, injectTargetType, pendingCreateList, out targetType, out registerTargetInstance) == false)
                {
                    // not found > check if multiple import
                    if (type.GetInterface(typeof(System.Collections.IEnumerable).FullName) != null)
                    {
                        var multiImportColl = localShare.CollectLocalMultiImports(this, type, injectTargetType, pendingCreateList);
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
                var multiImportColl = this.localShare.CollectLocalMultiImports(this, type, injectTargetType, pendingCreateList);
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
            localShare.CheckOutParamsSubscriptions(instance, outParams, this, type, injectTargetType);

            if (registerTargetInstance)
                localShare.RegisterSharedConstructorInstances(type, instance, outParams, outParamsInfo);

            if (contract != null)
            {
                // update contract service cache here as well to be not dependent on the registration order in nested cases
                int foundIndexAlternative;
                if (this.localSessionServiceInterfaceTypesResolved.Contains(type))
                {
                    int cachedAtIndex = Array.IndexOf<Type>(this.localSessionServiceInterfaceTypesResolved, type);
                    contract.LocalServices[cachedAtIndex] = instance;
                }
                else if (LocalSessionServiceTypeMappings != null
                        && LocalSessionServiceTypeMappings.TryGetValue(type, out var alternativeLocalSourceType)
                        && (foundIndexAlternative = Array.IndexOf<Type>(LocalServiceInterfaceTypes, alternativeLocalSourceType)) >= 0)
                {
                    // TargetAlsoImplements registration
                    if (localSessionServiceInterfaceTypesResolved[foundIndexAlternative].IsAssignableFrom(instance.GetType()))
                    {
                        contract.LocalServices[foundIndexAlternative] = instance;
                    }
                    else
                    {
                        logger.Warn($"Could not set TargetAlsoImplements interface instance \"{type}\" to local service interface instance \"{localSessionServiceInterfaceTypesResolved[foundIndexAlternative].FullName}\" because is not assignable.");
                    }
                }
            }

            //todo: parent container handling

            return true;
        }

        internal object DetermineConstructorImportInstance(Type type, string parameterName, Type injectTargetType, List<Type> pendingCreateList)
        {
            if (string.Compare(parameterName, "sessionId", true) == 0)
            {
                // return current session id
                if (currentSession is null)
                    throw new NullReferenceException($"Unable to inject \"{parameterName}\" to {injectTargetType.FullName} because no session in current context is available! Type: {type}; Host: {name}");

                return currentSession.SessionId;
            }
            else
            {
                var targetInstance = GetExport(type, injectTargetType, pendingCreateList);
                return targetInstance;
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
                Type interfType = remoteServiceInterfaceTypes[i].InterfaceType;
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
                        subscr.RemoveDelegate(serviceInstance, session);
                    }

                    // remove created subscription
                    if (localShare.sessionCreatedSubscriptions.TryGetValue(interfType, out subscr))
                    {
                        subscr.RemoveDelegate(serviceInstance, session);
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

                    // remove created subscriptions as well
                    if (localShare.sessionCreatedSubscriptions.TryGetValue(interfType, out subscr))
                    {
                        // remove only session related subscriptions (local session instance subscriptions)
                        subscr.RemoveDelegate(serviceInstance, session);
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

        /// <summary>
        /// Ioctalk will call the remote method without awaiting the response. The method will return immediately without blocking. This can be a great performance gain for mass remote calls.
        /// To avoid flooding the receiver underlying communication implements a control flow (IsAsyncVoidSendCurrentlyPossible) to issue a sync call if the receiver needs more time to process.
        /// This is only valid on methods with return type "void".
        /// Async void calls do not propagate back thrown exceptions. Exceptions will only occur on receiver side (see error logging).
        /// </summary>
        /// <typeparam name="InterfaceType">The interface type</typeparam>
        /// <param name="methodName">The void method name</param>
        public void RegisterAsyncVoidMethod<InterfaceType>(string methodName)
        {
            lock (syncObj)
            {
                if (asyncVoidMethods == null)
                    asyncVoidMethods = new Dictionary<Type, List<string>>();


                List<string> methods;
                if (!asyncVoidMethods.TryGetValue(typeof(InterfaceType), out methods))
                {
                    methods = new List<string>();
                    asyncVoidMethods.Add(typeof(InterfaceType), methods);
                }

                if (!methods.Contains(methodName))
                    methods.Add(methodName);
            }
        }

        public bool IsAsyncVoidRemoteInvoke(Type type, string methodName)
        {
            if (asyncVoidMethods == null)
                return false;

            List<string> methods;
            if (asyncVoidMethods.TryGetValue(type, out methods))
            {
                return methods.Contains(methodName);
            }

            return false;
        }


        /// <summary>
        /// Determines if the given interface (instance) is only valid during session lifetime (one instance per session).
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        internal bool IsSessionInstance(Type interfaceType, out ISession session)
        {
            if (currentSession != null)
            {
                // only if instance is acquired during session creation
                bool isLocalSessionInstance = localSessionServiceInterfaceTypesResolved.Contains(interfaceType);

                bool isRemoteSessionInstance = remoteServiceInterfaceTypesResolved.Contains(interfaceType);

                if (isLocalSessionInstance || isRemoteSessionInstance)
                {
                    session = currentSession;
                    return true;
                }
            }
            session = null;
            return false;
        }

        public bool IsSubscriptionRegistered(Type serviceDelegateType)
        {
            if (this.remoteServiceInterfaceTypes.Where(rsi => rsi.InterfaceType.Equals(serviceDelegateType)).Any())
            {
                return true;
            }

            if (this.localSessionServiceInterfaceTypes.Contains(serviceDelegateType))
            {
                return true;
            }

            return false;
        }

        private void SetAutoNameIfNecessary(string name)
        {
            if (name is null)
            {
                this.name = "ioctalkHost" + Interlocked.Increment(ref compositionHostAutoNameCounter);
            }
            else
            {
                this.name = name;
            }
        }



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
        public void MapInterfaceImplementationType<InterfaceType, ImplementationType>()
            where ImplementationType : class, InterfaceType
        {
            var typeHierachy = MapInterfaceImplementationTypeInternal(typeof(InterfaceType), typeof(ImplementationType));

            //return new MapInterfaceImplementationType<InterfaceType>(this, typeHierachy);
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
                throw new ArgumentException($"Interface type expected. Actual: {interfaceType.FullName}; Host: {name}", nameof(interfaceType));

            if (implementationType.IsClass == false)
                throw new ArgumentException($"Class type expected. Actual: {interfaceType.FullName}; Host: {name}", nameof(implementationType));

            var typeHierachy = new TypeHierachy(interfaceType, implementationType);

            interfaceImplementationMapping[interfaceType] = typeHierachy;

            return typeHierachy;
        }

        internal TypeHierachy GetInterfaceImplementationTypeHierachy(Type interfaceType)
        {
            return interfaceImplementationMapping[interfaceType];
        }


        internal bool TryFindInterfaceImplementation(Type interfaceType, Type injectTargetType, List<Type> pendingCreateList, out Type targetType, out bool registerTargetInstance)
        {
            if (interfaceImplementationMapping.TryGetValue(interfaceType, out var targetTypeHierachy))
            {
                targetType = targetTypeHierachy.GetNextImplementationType(injectTargetType, pendingCreateList, out registerTargetInstance);
                registerTargetInstance = true;      // ?
                return true;
            }
            else
                return localShare.TryFindInterfaceImplementation(interfaceType, injectTargetType, pendingCreateList, out targetType, out registerTargetInstance);  // redirect to parent container
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }
}
