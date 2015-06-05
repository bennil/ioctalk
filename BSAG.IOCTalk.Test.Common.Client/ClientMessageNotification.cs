using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using BSAG.IOCTalk.Common.Interface;

namespace BSAG.IOCTalk.Test.Common.Client.MEF
{
    [Export(typeof(IClientMessageNotification))]
    public class ClientMessageNotification : IClientMessageNotification
    {
        public ClientMessageNotification()
        {
        }

        public void NotifyMessage(string message)
        {
            Console.WriteLine("New server message from {0}: \"{1}\"", null, message);
        }

    }
}
