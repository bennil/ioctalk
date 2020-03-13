using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.Tcp.Config
{
    public class TcpTarget
    {
        /// <summary>
        /// Target host or IP address
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Target tcp port
        /// </summary>
        public int Port { get; set; }
    }
}
