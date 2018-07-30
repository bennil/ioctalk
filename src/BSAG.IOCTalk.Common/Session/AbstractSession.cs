using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Exceptions;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Abstract implementation of the <see cref="ISession"/> interface.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-11
    /// </remarks>
    public abstract class AbstractSession : ISession
    {
        #region AbstractSession fields
        // ----------------------------------------------------------------------------------------
        // AbstractSession fields
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Session ID
        /// </summary>
        protected int sessionId;

        /// <summary>
        /// Session description
        /// </summary>
        protected string description;

        /// <summary>
        /// Related communication service
        /// </summary>
        protected IGenericCommunicationService communicationService;

        /// <summary>
        /// Is session active
        /// </summary>
        protected bool isActive;

        protected bool isInitialized;

        protected IContract contract;             

        /// <summary>
        /// Pending request dictionary
        /// </summary>
        protected ConcurrentDictionary<long, IInvokeState> pendingRequests;


        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractSession constructors
        // ----------------------------------------------------------------------------------------
        // AbstractSession constructors
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Abstract session constructor.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="description">The description.</param>
        public AbstractSession(IGenericCommunicationService communicationService, int sessionId, string description)
        {
            this.communicationService = communicationService;
            this.sessionId = sessionId;
            this.description = description;
            this.pendingRequests = new ConcurrentDictionary<long, IInvokeState>();
            this.isActive = true; // session is active on creation
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractSession properties
        // ----------------------------------------------------------------------------------------
        // AbstractSession properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the session identity.
        /// </summary>
        public virtual int SessionId
        {
            get { return sessionId; }
        }

        /// <summary>
        /// Gets the session description.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
        }

        /// <summary>
        /// Gets the assigned communication service.
        /// </summary>
        public virtual IGenericCommunicationService CommunicationService
        {
            get { return communicationService; }
        }

        /// <summary>
        /// Gets a value indicating whether this session is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsActive
        {
            get { return isActive; }
        }

        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets the pending requests.
        /// </summary>
        public IDictionary<long, IInvokeState> PendingRequests
        {
            get { return pendingRequests; }
        }

        public IContract Contract => contract;


        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractSession methods
        // ----------------------------------------------------------------------------------------
        // AbstractSession methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Closes the session.
        /// </summary>
        public virtual void Close()
        {
            isActive = false;

            if (pendingRequests.Count > 0)
            {
                foreach (var pendingInvokeState in pendingRequests.Values)
                {
                    pendingInvokeState.Exception = new OperationCanceledException("Remote connction lost - Session ID: " + sessionId);

                    var waitHandle = pendingInvokeState.WaitHandle;

                    if (waitHandle != null)
                    {
                        try
                        {
                            waitHandle.Set();
                        }
                        catch (ObjectDisposedException) 
                        {
                            /* ignore already disposed handles */
                        }
                    }
                }

                pendingRequests.Clear();
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion
        
    }
}
