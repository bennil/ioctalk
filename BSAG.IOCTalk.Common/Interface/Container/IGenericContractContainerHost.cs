using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Session;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    /// <summary>
    /// Specifies a dependency injection container host including a communication binding.
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session. This class must inlcude all required imports for the communication. IOCTalk will create a single instance for every session (connection).</typeparam>
    public interface IGenericContractContainerHost<TServiceContractSession> : IGenericContractContainerHost<TServiceContractSession, SessionManager<TServiceContractSession>>
        where TServiceContractSession : class
    {
    }

    /// <summary>
    /// Specifies a dependency injection container host including a communication binding.
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session. This class must inlcude all required imports for the communication. IOCTalk will create a single instance for every session (connection).</typeparam>
    /// <typeparam name="TServiceContractSessionManager">The type of the service contract session manager. The session manager must implement the <see cref="IServiceContractSessionManager"/> interface who holds a list of all active sessions including callbacks for session created/terminated.</typeparam>
    public interface IGenericContractContainerHost<TServiceContractSession, TServiceContractSessionManager> : IGenericContainerHost
        where TServiceContractSession : class
        where TServiceContractSessionManager : class, IServiceContractSessionManager<TServiceContractSession>, new()
    {
        /// <summary>
        /// Occurs when a session is created.
        /// </summary>
        event SessionEventHandler<TServiceContractSession> SessionCreated;

        /// <summary>
        /// Occurs when a session is terminated.
        /// </summary>
        event SessionEventHandler<TServiceContractSession> SessionTerminated;

        /// <summary>
        /// Gets the session manager.
        /// </summary>
        TServiceContractSessionManager SessionManager { get; }

    }
}
