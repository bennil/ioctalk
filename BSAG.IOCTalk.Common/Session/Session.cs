﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

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
        public Session(IGenericCommunicationService communicationService, int sessionId, string description)
            : base(communicationService, sessionId, description)
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

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
