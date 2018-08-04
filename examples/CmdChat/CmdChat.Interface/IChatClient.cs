using System;
using System.Collections.Generic;
using System.Text;

namespace CmdChat.Interface
{
    public interface IChatClient
    {
        void OnNewMessage(string sourceName, IChatMsg msg);

        void OnNewClient(string name);

        void OnClientLeft(string name);
    }
}
