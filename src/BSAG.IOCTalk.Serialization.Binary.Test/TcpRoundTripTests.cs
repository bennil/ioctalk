using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Common.Test.TestObjects;
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
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    public class TcpRoundTripTests
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        readonly ITestOutputHelper xUnitLog;

        public TcpRoundTripTests(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }


        IMyRemoteAsyncAwaitTestService currentAsyncAwaitTestServiceClientProxyInstance;


        [Fact]
        public async Task TestMethodDataTransferAsyncImplementation()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33256;
            var log = new UnitTestLogger(xUnitLog);

            TcpCommunicationController tcpClient;
            TcpCommunicationController tcpBackendService;
            MyRemoteAsyncTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost("UnitTestService");
                compositionHostService.AddAssembly(typeof(MyRemoteAsyncTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService>();


                tcpBackendService = new TcpCommunicationController(log);
                tcpBackendService.Serializer = new BinaryMessageSerializer();
                tcpBackendService.LogDataStream = true;

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (MyRemoteAsyncTestService)compositionHostService.GetExport<IMyRemoteAsyncAwaitTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("UnitTestClient");
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClient = new TcpCommunicationController(log);
                tcpClient.Serializer = new BinaryMessageSerializer();
                tcpClient.LogDataStream = true;
                tcpClient.RequestTimeoutSeconds = 15;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated_AsyncTest;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            Assert.True(await onConnectionEstablished.Task);


            var dataResponse = await currentAsyncAwaitTestServiceClientProxyInstance.GetDataAsync();

            Assert.Equal("Hello world", dataResponse);


            int expected = 23134;
            var response2 = await currentAsyncAwaitTestServiceClientProxyInstance.GetDataAsync2(expected);
            Assert.Equal(expected, response2);

            // without return value
            MyRemoteAsyncTestService.RunSomeWorkCounter = 0;
            await currentAsyncAwaitTestServiceClientProxyInstance.RunSomeWork();

            Assert.Equal(1, MyRemoteAsyncTestService.RunSomeWorkCounter);

            tcpClient.Shutdown();
            tcpBackendService.Shutdown();
        }





        private void OnCompositionHostClient_SessionCreated_AsyncTest(object contractSession, SessionEventArgs e)
        {
            currentAsyncAwaitTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();
            onConnectionEstablished.SetResult(true);
        }








        IStressTestService currentStressTestServiceClientProxyInstance;

        [Fact]
        public async Task ClientServiceStressTestBinary1()
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
                var compositionHostService = new TalkCompositionHost("UnitTestStressTestService");
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();


                tcpBackendService = new TcpCommunicationController(log);
                tcpBackendService.Serializer = new BinaryMessageSerializer();

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("UnitTestStressTestClient");
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();
                compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                tcpClient = new TcpCommunicationController(log);
                tcpClient.Serializer = new BinaryMessageSerializer();

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated_StressTest;

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

        private void OnCompositionHostClient_SessionCreated_StressTest(object contractSession, SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IStressTestService>();
            onConnectionEstablished.SetResult(true);
        }
    }
}
