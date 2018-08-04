using CmdChat.Interface;
using System;

namespace CmdChat.Server.Implementation
{
    public class ChatService : IChatService
    {
        private string userName;
        private IChatBroker chatBroker;
        private IChatClient sourceClient;

        public ChatService(IChatBroker localChatBroker, IChatClient remoteClient)
        {
            this.chatBroker = localChatBroker;
            this.sourceClient = remoteClient;
        }

        public void Login(string name)
        {
            this.userName = name;

            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException("You must specify a name");

            chatBroker.NotifyNewClient(sourceClient, userName);
        }

        public void Logout()
        {
            // todo
        }

        public void BroadcastMessage(IChatMsg message)
        {
            if (string.IsNullOrEmpty(userName))
                throw new UnauthorizedAccessException("Please login first!");

            chatBroker.BroadcastMessage(userName, message);
        }


    }
}
