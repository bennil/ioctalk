using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Composition;
using CmdChat.Interface;
using System;

namespace CmdChat.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Command Line Chat Server");
            Console.WriteLine("----------------------------------");

            var compositionHost = new TalkCompositionHost();
            compositionHost.AddExecutionDirAssemblies();

            compositionHost.RegisterLocalSharedService<IChatBroker>();   // one global instance
            compositionHost.RegisterLocalSessionService<IChatService>();    // one instance for each session
            compositionHost.RegisterRemoteService<IChatClient>();           // one proxy instance for each session

            var tcpBackendService = new TcpCommunicationController();
            tcpBackendService.LogDataStream = true;

            compositionHost.InitGenericCommunication(tcpBackendService);

            tcpBackendService.InitService(52274);

            Console.WriteLine("Press return to exit");
            Console.ReadLine();

            Console.WriteLine("Exit...");
            tcpBackendService.Shutdown();
            
        }
    }
}
