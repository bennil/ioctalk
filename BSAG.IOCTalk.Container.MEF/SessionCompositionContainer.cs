using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using BSAG.IOCTalk.Common.Interface;
using System.Collections;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Session;
using System.ComponentModel.Composition.ReflectionModel;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Container.MEF.Metadata;
using BSAG.IOCTalk.Common.Interface.Logging;

namespace BSAG.IOCTalk.Container.MEF
{
    /// <summary>
    /// The SessionCompositionContainer class extends the MEF CompositionContainer to manage the instance session assignments.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-12
    /// </remarks>
    public class SessionCompositionContainer : CompositionContainer
    {
        #region SessionCompositionContainer fields
        // ----------------------------------------------------------------------------------------
        // SessionCompositionContainer fields
        // ----------------------------------------------------------------------------------------

        private Dictionary<int, SessionInstanceManager> sessionIdServiceInstanceMapping = new Dictionary<int, SessionInstanceManager>();
        private Dictionary<object, ISession> serviceInstanceSessionMapping = new Dictionary<object, ISession>();
        private Dictionary<string, Type> interfaceTypeCache;
        private Dictionary<Type, Type> interfaceTypeProxyImplCache = new Dictionary<Type, Type>();

        private ISession currentSessionContext = null;
        private object currentSessionContract = null;

        private ExportHelper<ISession> sessionExportHelper = null;
        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionCompositionContainer constructors
        // ----------------------------------------------------------------------------------------
        // SessionCompositionContainer constructors
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCompositionContainer"/> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        public SessionCompositionContainer(ComposablePartCatalog catalog)
            : base(catalog)
        {
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionCompositionContainer properties
        // ----------------------------------------------------------------------------------------
        // SessionCompositionContainer properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether the auto generated assemblies can be debugged (assembly are saved to the local temp folder).
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [create debug enabled assembly]; otherwise, <c>false</c>.
        /// </value>
        public bool CreateDebugEnabledAssembly { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILogger Logger { get; set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionCompositionContainer methods
        // ----------------------------------------------------------------------------------------
        // SessionCompositionContainer methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a collection of all exports that match the conditions in the specified <see cref="T:System.ComponentModel.Composition.Primitives.ImportDefinition"/> object.
        /// </summary>
        /// <param name="definition">The object that defines the conditions of the <see cref="T:System.ComponentModel.Composition.Primitives.Export"/> objects to get.</param>
        /// <param name="atomicComposition">The composition transaction to use, or null to disable transactional composition.</param>
        /// <returns>
        /// A collection of all the <see cref="T:System.ComponentModel.Composition.Primitives.Export"/> objects in this <see cref="T:System.ComponentModel.Composition.Hosting.CompositionContainer"/> object that match the conditions specified by <paramref name="definition"/>.
        /// </returns>
        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            SessionInstanceManager sInstanceManager = null;
            if (currentSessionContext != null)
            {
                // get session manager
                if (!sessionIdServiceInstanceMapping.TryGetValue(currentSessionContext.SessionId, out sInstanceManager))
                {
                    sInstanceManager = new SessionInstanceManager(currentSessionContext, currentSessionContract);
                    sessionIdServiceInstanceMapping.Add(currentSessionContext.SessionId, sInstanceManager);
                }

                object existingSessionInstance = null;
                if (sInstanceManager.ContractInstanceMapping.TryGetValue(definition.ContractName, out existingSessionInstance))
                {
                    // replace with existing session instance
                    return new Export[] { new Export(definition.ContractName, delegate () { return existingSessionInstance; }) };
                }
            }

            var exportEnumerable = base.GetExportsCore(definition, atomicComposition);

            IList<Export> exports;
            if (exportEnumerable is IList<Export>)
            {
                exports = (IList<Export>)exportEnumerable;
            }
            else
            {
                exports = new List<Export>(exportEnumerable);   // create new list
            }

            if (currentSessionContext != null)
            {
                if (exports.Count == 0
                    && definition.ContractName != typeof(IGenericCommunicationService).FullName)
                {
                    // try auto generate proxy implementation
                    Type interfaceType;
                    if (TypeService.TryGetTypeByName(definition.ContractName, out interfaceType))
                    {
                        object proxyImplementationInstance = null;
                        if (!sInstanceManager.ContractInstanceMapping.TryGetValue(definition.ContractName, out proxyImplementationInstance))
                        {
                            // try get auto generated proxy implementation type from cache
                            Type proxyImplementationType;
                            if (!interfaceTypeProxyImplCache.TryGetValue(interfaceType, out proxyImplementationType))
                            {
                                // auto generate proxy
                                proxyImplementationType = TypeService.BuildProxyImplementation(interfaceType, CreateDebugEnabledAssembly);
                                interfaceTypeProxyImplCache.Add(interfaceType, proxyImplementationType);
                            }

                            // create session proxy instance
                            proxyImplementationInstance = TypeService.CreateInstance(proxyImplementationType);

                            // satisfy imports
                            this.SatisfyImportsOnce(proxyImplementationInstance);
                        }

                        // add to remote call direction metadata (do not use proxy for incomming calls)
                        Dictionary<string, object> metadata = new Dictionary<string, object>();
                        metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, interfaceType.FullName);
                        metadata.Add(ExportConstants.IsToRemoteCallDirection, true);

                        Export export = new Export(definition.ContractName, metadata, new Func<object>(() => { return proxyImplementationInstance; }));
                        exports = new List<Export>();
                        exports.Add(export);
                    }
                }

                // serve session export and register export instances
                for (int i = 0; i < exports.Count; i++)
                {
                    var export = exports[i];

                    //if (export.Definition.ContractName == typeof(ISession).FullName)
                    //{
                    //    // replace with current session instance
                    //    exports[i] = new Export(export.Definition, delegate() { return sInstanceManager.Session; });
                    //}
                    //else
                    //{
                    if (export.Metadata.Count > 1
                        && export.Metadata.Contains(new KeyValuePair<string, object>(ExportConstants.IsToRemoteCallDirection, true)))
                    {
                        // do not register remote only proxy services in contract mapping
                        // only add to session instance mapping
                        if (!serviceInstanceSessionMapping.ContainsKey(export.Value))
                        {
                            serviceInstanceSessionMapping.Add(export.Value, currentSessionContext);
                        }
                    }
                    else
                    {
                        // check if contract is already registered in this session
                        object existingSessionInstance = null;
                        if (sInstanceManager.ContractInstanceMapping.TryGetValue(export.Definition.ContractName, out existingSessionInstance))
                        {
                            // replace with existing session instance
                            exports[i] = new Export(export.Definition, delegate () { return existingSessionInstance; });
                        }
                        else
                        {
                            // register session instance mappings
                            sInstanceManager.RegisterSessionExport(export);

                            if (!serviceInstanceSessionMapping.ContainsKey(export.Value))
                            {
                                serviceInstanceSessionMapping.Add(export.Value, currentSessionContext);
                            }
                        }
                    }
                    //}
                }
            }


            return exports;
        }


        /// <summary>
        /// Gets the interface implementation instance for the given session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        public virtual object GetInterfaceImplementationInstance(ISession session, string interfaceType)
        {
            object result = null;

            SessionInstanceManager sInstanceMgr;
            if (sessionIdServiceInstanceMapping.TryGetValue(session.SessionId, out sInstanceMgr))
            {
                result = sInstanceMgr.GetInterfaceImplementationInstance(interfaceType);

                if (result == null)
                {
                    // try find parts after session creation
                    Type implementationType = GetInterfaceImplementationType(interfaceType, true);
                    try
                    {
                        if (implementationType != null)
                        {
                            // create not known service implementation instance
                            object newServiceImplInstance = TypeService.CreateInstance(implementationType);
                            sInstanceMgr.RegisterSessionExport(interfaceType, newServiceImplInstance);
                            result = newServiceImplInstance;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(string.Format("Unable to create interface implementation instance of type \"{0}\" - Interface type: \"{1}\" - Details: {2}", implementationType.FullName, interfaceType, ex.ToString()));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the interface implementation type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="isServiceType">if set to <c>true</c> [is service type].</param>
        /// <returns></returns>
        public Type GetInterfaceImplementationType(string interfaceType, bool isServiceType)
        {
            if (interfaceTypeCache == null)
                interfaceTypeCache = new Dictionary<string, Type>();

            Type implementationType = null;
            if (!interfaceTypeCache.TryGetValue(interfaceType, out implementationType))
            {
                foreach (var part in this.Catalog.Parts)
                {
                    foreach (var exportDef in part.ExportDefinitions)
                    {
                        if (exportDef.ContractName == interfaceType)
                        {
                            object exportTypeIdentity;
                            if (exportDef.Metadata.TryGetValue("ExportTypeIdentity", out exportTypeIdentity))
                            {
                                implementationType = ReflectionModelServices.GetPartType(part).Value;

                                interfaceTypeCache.Add(interfaceType, implementationType);
                                break;
                            }
                        }
                    }

                    if (implementationType != null)
                    {
                        break;
                    }
                }
            }

            if (implementationType != null)
            {
                return implementationType;
            }
            else if (isServiceType)
            {
                throw new Exception("Service implemenation for the interface \"" + interfaceType + "\" not found! Provide a implementation with an export attribute.");
            }
            else
            {
                // No implementation found
                // Try auto generate type
                Type autoGeneratedType = TypeService.BuildInterfaceImplementationType(interfaceType, CreateDebugEnabledAssembly);
                if (autoGeneratedType != null)
                {
                    interfaceTypeCache[interfaceType] = autoGeneratedType;
                    return autoGeneratedType;
                }
                else
                {
                    throw new Exception("Unable to auto generate a class implemenation for the interface \"" + interfaceType + "\"! Provide a implementation with an export attribute.");
                }
            }
        }




        /// <summary>
        /// Gets the root service contract session object.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public virtual object GetServiceContractSession(ISession session)
        {
            object result = null;

            SessionInstanceManager sInstanceMgr;
            if (sessionIdServiceInstanceMapping.TryGetValue(session.SessionId, out sInstanceMgr))
            {
                result = sInstanceMgr.ServiceContractSession;
            }

            return result;
        }


        /// <summary>
        /// Gets the session by the given dependency injection service instance.
        /// </summary>
        /// <param name="serviceObjectInstance">The service object instance.</param>
        /// <returns></returns>
        public ISession GetSessionByInstance(object serviceObjectInstance)
        {
            ISession session = null;
            serviceInstanceSessionMapping.TryGetValue(serviceObjectInstance, out session);

            return session;
        }

        /// <summary>
        /// Registers the session.
        /// </summary>
        /// <param name="session">The session.</param>
        internal void RegisterSession(ISession session)
        {
            this.currentSessionContext = session;

            if (sessionExportHelper == null)
            {
                // create an export for the session object
                sessionExportHelper = new ExportHelper<ISession>(session);
                this.ComposeExportedValue<ExportHelper<ISession>>(sessionExportHelper);
                this.ComposeExportedValue<ISession>(session);
            }

            SessionInstanceManager sInstanceManager;
            if (!sessionIdServiceInstanceMapping.TryGetValue(currentSessionContext.SessionId, out sInstanceManager))
            {
                sInstanceManager = new SessionInstanceManager(currentSessionContext, currentSessionContract);
                sessionIdServiceInstanceMapping.Add(currentSessionContext.SessionId, sInstanceManager);
            }

            sInstanceManager.RegisterSessionExport(typeof(ISession).FullName, session);
            sInstanceManager.RegisterSessionExport(typeof(IGenericCommunicationService).FullName, session.CommunicationService);
        }

        /// <summary>
        /// Sets the container session context.
        /// </summary>
        /// <param name="sessionContract">The session contract.</param>
        internal void SetContainerSessionContext(object sessionContract)
        {
            this.currentSessionContract = sessionContract;

            SessionInstanceManager sInstanceManager;
            if (sessionIdServiceInstanceMapping.TryGetValue(currentSessionContext.SessionId, out sInstanceManager))
            {
                if (sInstanceManager.ServiceContractSession == null)
                {
                    sInstanceManager.ServiceContractSession = sessionContract;
                }
            }
            else
            {
                // add session manager
                sInstanceManager = new SessionInstanceManager(currentSessionContext, currentSessionContract);
                sessionIdServiceInstanceMapping.Add(currentSessionContext.SessionId, sInstanceManager);
            }
        }

        /// <summary>
        /// Resets the container session context.
        /// </summary>
        internal void ResetContainerSessionContext()
        {
            this.currentSessionContext = null;
            this.currentSessionContract = null;
        }


        internal void ReleaseSessionInstances(ISession session)
        {
            SessionInstanceManager instanceManager;
            if (sessionIdServiceInstanceMapping.TryGetValue(session.SessionId, out instanceManager))
            {
                SessionInstanceManager.CheckSessionStateTerminatedCall(session, instanceManager.ServiceContractSession);
                foreach (var item in instanceManager.ContractInstanceMapping.Values)
                {
                    // remove object session mappings
                    serviceInstanceSessionMapping.Remove(item);

                    if (instanceManager.ServiceContractSession != item)
                        SessionInstanceManager.CheckSessionStateTerminatedCall(session, item);
                }

                instanceManager.ReleaseSessionInstances();
                sessionIdServiceInstanceMapping.Remove(session.SessionId);
            }
        }



        // ----------------------------------------------------------------------------------------
        #endregion





    }

}
