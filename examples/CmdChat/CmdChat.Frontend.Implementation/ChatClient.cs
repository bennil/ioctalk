using CmdChat.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CmdChat.Frontend.Implementation
{
    public class ChatClient : IChatClient
    {
        private IChatService chatService;
        private string username;

        public ChatClient(IChatService chatService)
        {
            this.chatService = chatService;

            Task.Run(new Action(StartChat));
        }

        private void StartChat()
        {
            try
            {
                WriteColorLine(ConsoleColor.White, "Please enter your username:");

                username = Console.ReadLine();

                chatService.Login(username);

                WriteColorLine(ConsoleColor.White, "You are logged in - start writing");

                string message;
                do
                {
                    message = Console.ReadLine();
                    chatService.BroadcastMessage(new ChatMsg() { Text = message });

                } while (message != "exit");

                chatService.Logout();
                WriteColorLine(ConsoleColor.DarkRed, "Logout");
            }
            catch (Exception ex)
            {
                WriteColorLine(ConsoleColor.Red, ex.ToString());
            }
        }


        public void OnNewClient(string name)
        {
            WriteColorLine(ConsoleColor.DarkYellow, $"New user \"{name}\" joined");
        }

        public void OnClientLeft(string name)
        {
            WriteColorLine(ConsoleColor.DarkYellow, $"User \"{name}\" left the chat");
        }

        public void OnNewMessage(string sourceName, IChatMsg msg)
        {
            WriteColorLine(sourceName == username ? ConsoleColor.White : ConsoleColor.Green, $"{sourceName}:   {msg.Text}");
        }


        private void WriteColorLine(ConsoleColor color, string line)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = oldColor;
        }

    }
}
