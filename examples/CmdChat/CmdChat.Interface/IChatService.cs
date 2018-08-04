using System;
using System.Collections.Generic;
using System.Text;

namespace CmdChat.Interface
{
    public interface IChatService
    {
        void Login(string name);

        void Logout();

        void BroadcastMessage(IChatMsg message);
    }
}
