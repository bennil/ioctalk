using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Json;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Common.Test
{
    [Collection("Sequential")]
    public class TcpStressTest
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        IStressTestService currentStressTestServiceClientProxyInstance;
        readonly ITestOutputHelper xUnitLog;

        public TcpStressTest(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        [Fact]
        public async Task ClientServiceStressTest1()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33254;
            var log = new UnitTestLogger(xUnitLog);

            TcpCommunicationController tcpClient;
            TcpCommunicationController tcpBackendService;

            StressTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost();
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();


                tcpBackendService = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();
                compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                tcpClient = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            Assert.True(await onConnectionEstablished.Task);

            int number = 0;
            for (; number < 10000; number++)
            {
                currentStressTestServiceClientProxyInstance.AsyncCallTest(number);
            }

            for (; number < 20000; number++)
            {
                var result = currentStressTestServiceClientProxyInstance.SyncCallTest(number);
                Assert.Equal(number, result);
            }

            Assert.Equal(number, localService.CurrentNumber);

            string longTestData = "TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST";
            for (; number < 25000; number++)
            {
                var data = new DataTransferTest
                {
                    ID = number,
                    Name = longTestData
                };
                var result = currentStressTestServiceClientProxyInstance.ComplexCall(number, data);
                Assert.Equal(number, result);
            }

            Assert.Equal(number, localService.CurrentNumber);

            tcpClient.Shutdown();
            tcpBackendService.Shutdown();
        }

        private void OnCompositionHostClient_SessionCreated(object contractSession, Session.SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IStressTestService>();
            onConnectionEstablished.SetResult(true);
        }
    }
}
