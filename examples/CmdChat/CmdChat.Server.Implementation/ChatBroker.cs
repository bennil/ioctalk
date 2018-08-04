using CmdChat.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CmdChat.Server.Implementation
{
    public class ChatBroker : IChatBroker
    {
        private List<IChatClient> clients = new List<IChatClient>();

        public ChatBroker(out Action<IChatClient> onChatClientCreated, out Action<IChatClient> onChatClientTerminated)
        {
            onChatClientCreated = OnChatClientCreated;
            onChatClientTerminated = OnChatClientTerminated;
        }

        private void OnChatClientCreated(IChatClient chatClient)
        {
            clients.Add(chatClient);
        }

        private void OnChatClientTerminated(IChatClient chatClient)
        {
            clients.Remove(chatClient);
        }

        public void BroadcastMessage(string sourceUser, IChatMsg message)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    clients[i].OnNewMessage(sourceUser, message);
                }
                catch (OperationCanceledException)
                {
                    // connection close during send
                }
                catch (TimeoutException)
                {
                    // no response in time
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void NotifyNewClient(IChatClient sourceClient, string name)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] != sourceClient)  // do not notify source
                    clients[i].OnNewClient(name);
            }
        }
    }
}
