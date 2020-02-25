using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.Tcp.Config
{
    public class TcpConfiguration
    {
        public ConnectionType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public bool LogDataStream { get; set; }
    }
}
