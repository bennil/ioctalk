using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Json;
using CmdChat.Interface;
using System;
using System.Threading;

namespace CmdChat.Frontend
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Command Line Chat Frontend");
            Console.WriteLine("----------------------------------");

            var compositionHost = new TalkCompositionHost();
            compositionHost.AddExecutionDirAssemblies();

            compositionHost.RegisterLocalSessionService<IChatClient>();
            compositionHost.RegisterRemoteService<IChatService>();

            var tcpClientService = new TcpCommunicationController(new LegacyWireFraming(), new JsonMessageSerializer());

            compositionHost.InitGenericCommunication(tcpClientService);

            tcpClientService.InitClient("127.0.0.1", 52274);

            Thread.CurrentThread.Join();    // wait infinite
        }
    }
}
