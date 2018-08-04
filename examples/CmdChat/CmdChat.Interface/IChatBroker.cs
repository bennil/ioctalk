using System;
using System.Collections.Generic;
using System.Text;

namespace CmdChat.Interface
{
    public interface IChatBroker
    {
        void BroadcastMessage(string sourceUser, IChatMsg message);

        void NotifyNewClient(IChatClient client, string username);
    }
}
