using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Session event handler
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session.</typeparam>
    /// <param name="contractSession">The contract session.</param>
    /// <param name="e">The <see cref="BSAG.IOCTalk.Common.Session.SessionEventArgs"/> instance containing the event data.</param>
    public delegate void SessionEventHandler<TServiceContractSession>(TServiceContractSession contractSession, SessionEventArgs e);
    
}
