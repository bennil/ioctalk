using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Common.Interface.Session
{
    /// <summary>
    /// Specifies a transfer session interface.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// Gets the session identity.
        /// </summary>
        int SessionId { get; }

        /// <summary>
        /// Gets the session description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the assigned communication service.
        /// </summary>
        IGenericCommunicationService CommunicationService { get; }

        /// <summary>
        /// Gets a value indicating whether this session is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        bool IsActive { get; }

        /// <summary>
        /// Gets a value indicating whether this session is initialized yet.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the session contract
        /// </summary>
        IContract Contract { get; }

        /// <summary>
        /// Gets the pending requests.
        /// </summary>
        IDictionary<long, IInvokeState> PendingRequests { get; }

        /// <summary>
        /// Closes the session.
        /// </summary>
        void Close();
    }
}
