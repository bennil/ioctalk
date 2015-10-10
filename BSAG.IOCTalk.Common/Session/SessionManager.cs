using System.Collections.Generic;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Default implementation of the IOC-Talk session manager
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-16
    /// </remarks>
    public class SessionManager<TServiceContractSession> : IServiceContractSessionManager<TServiceContractSession>
    {
        #region SessionManager fields

        // ----------------------------------------------------------------------------------------
        // SessionManager fields
        // ----------------------------------------------------------------------------------------

        private List<SessionServiceContractMapping<TServiceContractSession>> serviceContractSessions = new List<SessionServiceContractMapping<TServiceContractSession>>();

        // ----------------------------------------------------------------------------------------

        #endregion SessionManager fields

        #region SessionManager constructors

        // ----------------------------------------------------------------------------------------
        // SessionManager constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>SessionManager</c> class.
        /// </summary>
        public SessionManager()
        {
        }

        // ----------------------------------------------------------------------------------------

        #endregion SessionManager constructors

        #region SessionManager properties

        // ----------------------------------------------------------------------------------------
        // SessionManager properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the service contract sessions.
        /// </summary>
        public IList<SessionServiceContractMapping<TServiceContractSession>> ServiceContractSessions
        {
            get
            {
                return serviceContractSessions;
            }
        }

        // ----------------------------------------------------------------------------------------

        #endregion SessionManager properties

        #region SessionManager methods

        // ----------------------------------------------------------------------------------------
        // SessionManager methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Called when service contract session is created.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="serviceContractSessionInstance">The service contract session instance.</param>
        public virtual void OnServiceContractSessionCreated(ISession session, TServiceContractSession serviceContractSessionInstance)
        {
            SessionServiceContractMapping<TServiceContractSession> mapping = new SessionServiceContractMapping<TServiceContractSession>();
            mapping.Session = session;
            mapping.ServiceContract = serviceContractSessionInstance;

            serviceContractSessions.Add(mapping);
        }

        /// <summary>
        /// Called when service contract session is terminated.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="serviceContractSessionInstance">The service contract session instance.</param>
        public virtual void OnServiceContractSessionTerminated(ISession session, TServiceContractSession serviceContractSessionInstance)
        {
            for (int sessionIndex = 0; sessionIndex < serviceContractSessions.Count; )
            {
                var sessionMapping = serviceContractSessions[sessionIndex];
                var sessionItem = sessionMapping.Session;

                if (sessionItem == null
                    || sessionItem.SessionId == session.SessionId)
                {
                    serviceContractSessions.RemoveAt(sessionIndex);
                }
                else
                {
                    sessionIndex++;
                }
            }
        }

        // ----------------------------------------------------------------------------------------

        #endregion SessionManager methods

    }
}