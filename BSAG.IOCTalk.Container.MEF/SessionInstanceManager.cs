using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Primitives;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Container.MEF
{
    /// <summary>
    /// The SessionInstanceManager holds dependency injection instances for an active session.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-12
    /// </remarks>
    public class SessionInstanceManager
    {
        #region SessionInstanceManager fields
        // ----------------------------------------------------------------------------------------
        // SessionInstanceManager fields
        // ----------------------------------------------------------------------------------------

        private Dictionary<string, object> contractNameInstanceMapping = new Dictionary<string, object>();

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionInstanceManager constructors
        // ----------------------------------------------------------------------------------------
        // SessionInstanceManager constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>SessionInstanceManager</c> class.
        /// </summary>
        public SessionInstanceManager(ISession session, object serviceContractSession)
        {
            this.Session = session;
            this.ServiceContractSession = serviceContractSession;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionInstanceManager properties
        // ----------------------------------------------------------------------------------------
        // SessionInstanceManager properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the session.
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// Gets the service contract session.
        /// </summary>
        public object ServiceContractSession { get; private set; }


        /// <summary>
        /// Gets the contract instance mapping.
        /// </summary>
        internal Dictionary<string, object> ContractInstanceMapping
        {
            get
            {
                return contractNameInstanceMapping;
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionInstanceManager methods
        // ----------------------------------------------------------------------------------------
        // SessionInstanceManager methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Registers the session export.
        /// </summary>
        /// <param name="export">The export.</param>
        public void RegisterSessionExport(Export export)
        {
            RegisterSessionExport(export.Definition.ContractName, export.Value);
        }

        /// <summary>
        /// Registers the session export.
        /// </summary>
        /// <param name="contractName">Name of the contract.</param>
        /// <param name="instance">The instance.</param>
        public void RegisterSessionExport(string contractName, object instance)
        {
            object existingObject;
            if (contractNameInstanceMapping.TryGetValue(contractName, out existingObject))
            {
                if (!instance.Equals(existingObject))
                {
                    // multiple import instances (NonShared) for the same object are found
                    // remote calls will be only directed to the first instance
                    CheckSessionStateCreatedCall(Session, instance);
                }
                // instance already mapped
            }
            else
            {
                // create new instance
                contractNameInstanceMapping.Add(contractName, instance);

                CheckSessionStateCreatedCall(Session, instance);
            }
        }


        /// <summary>
        /// Gets the interface implementation instance.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        public object GetInterfaceImplementationInstance(string interfaceType)
        {
            object result = null;
            contractNameInstanceMapping.TryGetValue(interfaceType, out result);
            return result;
        }


        /// <summary>
        /// Releases the session instances.
        /// </summary>
        internal void ReleaseSessionInstances()
        {
            ServiceContractSession = null;
            contractNameInstanceMapping.Clear();
        }

        /// <summary>
        /// Checks the session state created call.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="instance">The instance.</param>
        internal static void CheckSessionStateCreatedCall(ISession session, object instance)
        {
            if (instance is ISessionStateChanged)
            {
                var sessionStateChanged = instance as ISessionStateChanged;
                sessionStateChanged.OnSessionCreated(session);
            }
        }


        /// <summary>
        /// Checks the session state terminated call.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="instance">The instance.</param>
        internal static void CheckSessionStateTerminatedCall(ISession session, object instance)
        {
            if (instance is ISessionStateChanged)
            {
                var sessionStateChanged = instance as ISessionStateChanged;
                sessionStateChanged.OnSessionTerminated(session);
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
