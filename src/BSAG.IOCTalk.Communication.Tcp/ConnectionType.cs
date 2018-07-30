using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Communication.Tcp
{
    /// <summary>
    /// Defines the connection type (client/service)
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Client connection
        /// </summary>
        Client = 1,

        /// <summary>
        /// Service listener
        /// </summary>
        Service = 2,
    }
}
