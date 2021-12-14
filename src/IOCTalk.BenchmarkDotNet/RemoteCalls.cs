using BenchmarkDotNet.Attributes;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCTalk.BenchmarkDotNet
{
    //[RPlotExporter]
    [AllStatisticsColumn]
    [MemoryDiagnoser]
    public class RemoteCalls
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        IStressTestService currentStressTestServiceClientProxyInstance;
        IMyRemoteAsyncAwaitTestService myRemoteAsyncAwaitTestService;

        TcpCommunicationController tcpClient;
        TcpCommunicationController tcpBackendService;

        [GlobalSetup]
        public async Task Setup()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33255;


            StressTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost();
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                //compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();
                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService>();


                tcpBackendService = new TcpCommunicationController();

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                //compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();
                compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClient = new TcpCommunicationController();

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            await onConnectionEstablished.Task;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            tcpClient?.Shutdown();
            tcpBackendService?.Shutdown();
        }

        private void OnCompositionHostClient_SessionCreated(object contractSession, SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IStressTestService>();
            myRemoteAsyncAwaitTestService = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();
            onConnectionEstablished.SetResult(true);
        }


        [Benchmark(Baseline = true)]
        public void SimpleCallClientToService() => currentStressTestServiceClientProxyInstance.SimpleCall();

        [Benchmark]
        public async Task SimpleCallAsyncAwaitClientToService() => await myRemoteAsyncAwaitTestService.SimpleCall();

    }
}
