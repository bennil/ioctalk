using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Session
{
    /// <summary>
    /// Specifies session state changed callback interface.
    /// </summary>
    public interface ISessionStateChanged
    {
        /// <summary>
        /// Called when [session created].
        /// </summary>
        void OnSessionCreated(ISession session);
        
        /// <summary>
        /// Called when the session is terminated.
        /// </summary>
        void OnSessionTerminated(ISession session);
    }
}
