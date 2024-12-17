using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Simple default session implementation.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-11
    /// </remarks>
    public class Session : AbstractSession
    {
        #region Session fields
        // ----------------------------------------------------------------------------------------
        // Session fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region Session constructors
        // ----------------------------------------------------------------------------------------
        // Session constructors
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance of the <c>Session</c> class.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="description">The description.</param>
        public Session(IGenericCommunicationService communicationService, int sessionId, string description, object underlyingCommunicationObject, Action forceCloseCallback = null)
            : base(communicationService, sessionId, description, forceCloseCallback, underlyingCommunicationObject)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region Session properties
        // ----------------------------------------------------------------------------------------
        // Session properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region Session methods
        // ----------------------------------------------------------------------------------------
        // Session methods
        // ----------------------------------------------------------------------------------------

        public void OnSessionInitalized(IContract contract)
        {
            this.contract = contract;
            isInitialized = true;
        }

        public override string ToString()
        {
            return $"{SessionId} {Description}";
        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
