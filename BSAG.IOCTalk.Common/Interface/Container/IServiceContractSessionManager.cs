using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    /// <summary>
    /// Session Manager interface
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session.</typeparam>
    public interface IServiceContractSessionManager<TServiceContractSession>
    {

        /// <summary>
        /// Called when service contract session instance is created.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="serviceContractSessionInstance">The service contract session instance.</param>
        void OnServiceContractSessionCreated(ISession session, TServiceContractSession serviceContractSessionInstance);

        /// <summary>
        /// Called when service contract session instance is terminated.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="serviceContractSessionInstance">The service contract session instance.</param>
        void OnServiceContractSessionTerminated(ISession session, TServiceContractSession serviceContractSessionInstance);


        /// <summary>
        /// Gets the service contract sessions.
        /// </summary>
        IList<SessionServiceContractMapping<TServiceContractSession>> ServiceContractSessions { get; }
    }
}
