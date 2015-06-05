using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Session event args
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-11
    /// </remarks>
    public class SessionEventArgs : EventArgs
    {
        #region SessionEventArgs fields
        // ----------------------------------------------------------------------------------------
        // SessionEventArgs fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionEventArgs constructors
        // ----------------------------------------------------------------------------------------
        // SessionEventArgs constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>SessionEventArgs</c> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public SessionEventArgs(ISession session)
            : this(session, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <c>SessionEventArgs</c> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="sessionContract">The session contract object.</param>
        public SessionEventArgs(ISession session, object sessionContract)
        {
            this.Session = session;
            this.SessionContract = sessionContract;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionEventArgs properties
        // ----------------------------------------------------------------------------------------
        // SessionEventArgs properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <value>
        /// The session.
        /// </value>
        public ISession Session { get; private set; }


        /// <summary>
        /// Gets the session contract object.
        /// </summary>
        /// <value>
        /// The session contract.
        /// </value>
        public object SessionContract { get; private set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SessionEventArgs methods
        // ----------------------------------------------------------------------------------------
        // SessionEventArgs methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
