using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Session - Session Contract mapping
    /// </summary>
    /// <typeparam name="TServiceContractSession">The type of the service contract session.</typeparam>
    public class SessionServiceContractMapping<TServiceContractSession>
    {
        /// <summary>
        /// Gets or sets the service contract.
        /// </summary>
        /// <value>
        /// The service contract.
        /// </value>
        public TServiceContractSession ServiceContract { get; set; }

        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        /// <value>
        /// The session.
        /// </value>
        public ISession Session { get; set; }
    }
}
