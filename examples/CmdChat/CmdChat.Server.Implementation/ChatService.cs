using CmdChat.Interface;
using System;

namespace CmdChat.Server.Implementation
{
    public class ChatService : IChatService, IDisposable
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

            Console.WriteLine($"New user login: {userName}");

            chatBroker.NotifyNewClient(sourceClient, userName);
        }

        public void Logout()
        {
            Console.WriteLine($"User \"{userName}\" left");
        }

        public void BroadcastMessage(IChatMsg message)
        {
            if (string.IsNullOrEmpty(userName))
                throw new UnauthorizedAccessException("Please login first!");

            chatBroker.BroadcastMessage(userName, message);
        }

        public void Dispose()
        {
            Logout();
        }
    }
}
