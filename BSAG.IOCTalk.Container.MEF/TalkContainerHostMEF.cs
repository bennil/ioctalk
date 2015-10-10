using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Config;
using System.Xml.Linq;

namespace BSAG.IOCTalk.Container.MEF
{
    /// <summary>
    /// The <see cref="TalkContainerHostMEF"/> implements the IOCTalk <see cref="IGenericContainerHost"/> interface using MEF as dependency injection container.
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session. This class must inlcude all required imports for the communication. IOCTalk will create a single instance for every session (connection).</typeparam>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 09.07.2013
    /// </remarks>
    [Export(typeof(IGenericContainerHost))]
    public class TalkContainerHostMEF<TServiceContractSession> : TalkContainerHostMEF<TServiceContractSession, SessionManager<TServiceContractSession>>, IGenericContractContainerHost<TServiceContractSession>
        where TServiceContractSession : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TalkContainerHostMEF&lt;TServiceContractSession&gt;"/> class.
        /// </summary>
        public TalkContainerHostMEF()
        {
        }
    }

    /// <summary>
    /// The <see cref="TalkContainerHostMEF"/> implements the IOCTalk <see cref="IGenericContainerHost"/> interface using MEF as dependency injection container.
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session. This class must inlcude all required imports for the communication. IOCTalk will create a single instance for every session (connection).</typeparam>
    /// <typeparam name="TServiceContractSessionManager">The type of the service contract session manager. The session manager must implement the <see cref="IServiceContractSessionManager"/> interface who holds a list of all active sessions including callbacks for session created/terminated.</typeparam>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 09.07.2013
    /// </remarks>
    [Export(typeof(IGenericContainerHost))]
    public class TalkContainerHostMEF<TServiceContractSession, TServiceContractSessionManager> : IGenericContractContainerHost<TServiceContractSession, TServiceContractSessionManager>, IXmlConfig
        where TServiceContractSession : class, new()
        where TServiceContractSessionManager : class, IServiceContractSessionManager<TServiceContractSession>, new()
    {
        #region TalkContainerHostMEF fields
        // ----------------------------------------------------------------------------------------
        // TalkContainerHostMEF fields
        // ----------------------------------------------------------------------------------------

        private Dictionary<string, Export> interfaceImplementationCache = null;
        private SessionCompositionContainer container = null;
        private Type serviceContractSessionType;
        private TServiceContractSessionManager sessionManager = null;
        private Dictionary<Type, Type> exposedSubInterfaceTypeMapping;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkContainerHostMEF constructors
        // ----------------------------------------------------------------------------------------
        // TalkContainerHostMEF constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>TalkContainerHostMEF</c> class.
        /// </summary>
        public TalkContainerHostMEF()
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkContainerHostMEF events
        // ----------------------------------------------------------------------------------------
        // TalkContainerHostMEF properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when [session created].
        /// </summary>
        public event SessionEventHandler<TServiceContractSession> SessionCreated;

        /// <summary>
        /// Occurs when [session terminated].
        /// </summary>
        public event SessionEventHandler<TServiceContractSession> SessionTerminated;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkContainerHostMEF properties
        // ----------------------------------------------------------------------------------------
        // TalkContainerHostMEF properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the dependency injection container instance.
        /// </summary>
        public object DIContainer
        {
            get { return container; }
        }

        /// <summary>
        /// Gets the session manager.
        /// </summary>
        public TServiceContractSessionManager SessionManager
        {
            get { return sessionManager; }
        }

        /// <summary>
        /// Gets or sets the xml configuration.
        /// </summary>
        public XDocument Config { get; set; }
        
        // ----------------------------------------------------------------------------------------
        #endregion

        #region TalkContainerHostMEF methods
        // ----------------------------------------------------------------------------------------
        // TalkContainerHostMEF methods
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Initalizes the generic communication.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        public void InitGenericCommunication(IGenericCommunicationService communicationService)
        {
            if (interfaceImplementationCache != null)
            {
                throw new Exception("TalkContainerHostMEF is already initialized.");
            }

            bool createDebugEnabledAssembly;
            List<ComposablePartCatalog> catalogs = new List<ComposablePartCatalog>();
            LoadXmlConfig(catalogs, out createDebugEnabledAssembly);

            if (catalogs.Count == 0)
            {
                // add default if not configured
                catalogs.Add(new DirectoryCatalog("."));
            }

            var catalog = new AggregateCatalog(catalogs.ToArray());
            SessionCompositionContainer container = new SessionCompositionContainer(catalog);
            container.CreateDebugEnabledAssembly = createDebugEnabledAssembly;

            InitGenericCommunication(communicationService, container);
        }


        /// <summary>
        /// Initalizes the generic communication.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        /// <param name="catalog">The MEF catalog.</param>
        public void InitGenericCommunication(IGenericCommunicationService communicationService, ComposablePartCatalog catalog)
        {
            if (interfaceImplementationCache != null)
            {
                throw new Exception("TalkContainerHostMEF is already initialized.");
            }

            SessionCompositionContainer container = new SessionCompositionContainer(catalog);

            InitGenericCommunication(communicationService, container);
        }

        /// <summary>
        /// Initalizes the generic communication with the libraries matching the specified search patterns.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        /// <param name="directorySearchPatterns">The directory search patterns. See System.IO.Directory.GetFiles(searchPattern) reference.</param>
        public void InitGenericCommunication(IGenericCommunicationService communicationService, string[] directorySearchPatterns)
        {
            if (interfaceImplementationCache != null)
            {
                throw new Exception("TalkContainerHostMEF is already initialized.");
            }

            var catalog = new AggregateCatalog();

            if (directorySearchPatterns != null)
            {
                foreach (var dirSearchPattern in directorySearchPatterns)
                {
                    catalog.Catalogs.Add(new DirectoryCatalog(".", dirSearchPattern));
                }
            }

            SessionCompositionContainer container = new SessionCompositionContainer(catalog);

            InitGenericCommunication(communicationService, container);
        }

        /// <summary>
        /// Initalizes the generic communication.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        /// <param name="container">The MEF container.</param>
        public void InitGenericCommunication(IGenericCommunicationService communicationService, SessionCompositionContainer container)
        {
            if (interfaceImplementationCache != null)
            {
                throw new Exception("TalkContainerHostMEF is already initialized.");
            }

            this.container = container;
            this.interfaceImplementationCache = new Dictionary<string, Export>();
            this.serviceContractSessionType = typeof(TServiceContractSession);
            this.sessionManager = new TServiceContractSessionManager();

            // Inject single instances
            container.ComposeExportedValue<IGenericCommunicationService>(communicationService);
            container.ComposeExportedValue<TServiceContractSessionManager>(sessionManager);

            // register communication host for response processing
            communicationService.RegisterContainerHost(this);

            // export ILogger
            this.container.Logger = communicationService.Logger;
            container.ComposeExportedValue<ILogger>(communicationService.Logger);

            communicationService.SessionCreated += new EventHandler<SessionEventArgs>(OnCommunicationService_SessionCreated);
            communicationService.SessionTerminated += new EventHandler<SessionEventArgs>(OnCommunicationService_SessionTerminated);
        }


        private void OnCommunicationService_SessionCreated(object sender, SessionEventArgs e)
        {
            OnSessionCreated(e.Session);

            TServiceContractSession contractSession = (TServiceContractSession)e.SessionContract;

            if (contractSession != null)
            {
                sessionManager.OnServiceContractSessionCreated(e.Session, contractSession);

                if (SessionCreated != null)
                {
                    SessionCreated(contractSession, e);
                }

                SessionInstanceManager.CheckSessionStateCreatedCall(e.Session, contractSession);
            }
        }

        private void OnCommunicationService_SessionTerminated(object sender, SessionEventArgs e)
        {
            // remove session service instances
            TServiceContractSession contractSession = (TServiceContractSession)container.GetServiceContractSession(e.Session);

            sessionManager.OnServiceContractSessionTerminated(e.Session, contractSession);

            container.ReleaseSessionInstances(e.Session);

            OnSessionTerminated(e.Session);

            if (SessionTerminated != null
                && contractSession != null)
            {
                SessionTerminated(contractSession, e);
            }
        }

        /// <summary>
        /// Called when [session created].
        /// </summary>
        /// <param name="session"></param>
        public virtual void OnSessionCreated(ISession session)
        {
        }

        /// <summary>
        /// Called when the session is terminated.
        /// </summary>
        /// <param name="session"></param>
        public virtual void OnSessionTerminated(ISession session)
        {
        }


        /// <summary>
        /// Creates the session contract instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public virtual object CreateSessionContractInstance(ISession session)
        {
            TServiceContractSession sessionContract = (TServiceContractSession)Activator.CreateInstance(serviceContractSessionType);

            // set container session context
            container.SetContainerSessionContext(session, sessionContract);

            // compose parts
            container.ComposeParts(sessionContract);

            // reset container session context
            container.ResetContainerSessionContext();

            return sessionContract;
        }



        /// <summary>
        /// Gets the interface implementation instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        public virtual object GetInterfaceImplementationInstance(ISession session, string interfaceType)
        {
            if (interfaceImplementationCache == null)
            {
                throw new Exception("TalkContainerHostMEF must be initialized.");
            }

            return container.GetInterfaceImplementationInstance(session, interfaceType);
        }


        /// <summary>
        /// Gets the interface implementation type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        public Type GetInterfaceImplementationType(string interfaceType)
        {
            return container.GetInterfaceImplementationType(interfaceType, false);
        }


        /// <summary>
        /// Gets the session by the given service import instance.
        /// </summary>
        /// <param name="serviceObjectInstance">The service object instance.</param>
        /// <returns></returns>
        public virtual ISession GetSessionByServiceInstance(object serviceObjectInstance)
        {
            return container.GetSessionByInstance(serviceObjectInstance);
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
        /// <param name="interfaceType">Type of the exposed interface.</param>
        /// <param name="sourceType">Type of the concrete source.</param>
        public void RegisterExposedSubInterfaceForType(Type interfaceType, Type sourceType)
        {
            if (exposedSubInterfaceTypeMapping == null)
                exposedSubInterfaceTypeMapping = new Dictionary<Type, Type>();

            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException(string.Format("The exposed sub type \"{0}\" must be an interface!", interfaceType.FullName));
            }

            exposedSubInterfaceTypeMapping[sourceType] = interfaceType;            
        }

        private void LoadXmlConfig(List<ComposablePartCatalog> catalogs, out bool createDebugEnabledAssembly)
        {
            createDebugEnabledAssembly = false;

            if (Config != null)
            {
                var container = Config.Element("Container");
                if (container != null)
                {
                    // catalogs
                    var catalogsXml = container.Element("Catalogs");
                    if (catalogsXml != null)
                    {
                        foreach (var catalogXml in catalogsXml.Elements())
                        {
                            if (catalogXml.Name.LocalName == "DirectoryCatalog")
                            {
                                var pathXml = catalogXml.Attribute("path");
                                var searchPatternXml = catalogXml.Attribute("searchPattern");

                                if (searchPatternXml != null)
                                {
                                    catalogs.Add(new DirectoryCatalog(pathXml.Value, searchPatternXml.Value));
                                }
                                else
                                {
                                    catalogs.Add(new DirectoryCatalog(pathXml.Value));
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("Catalog type not supported for xml configuration!");
                            }
                        }
                    }

                    // debug setting
                    var createDebugEnabledAssemblyXml = container.Element("CreateDebugEnabledAssembly");
                    if (createDebugEnabledAssemblyXml != null)
                    {
                        createDebugEnabledAssembly = bool.Parse(createDebugEnabledAssemblyXml.Value);
                    }
                }
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion


       
    }

}
