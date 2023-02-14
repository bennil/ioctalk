using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
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
    public class TcpRoundTripNetTcpTests
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        readonly ITestOutputHelper xUnitLog;

        public TcpRoundTripNetTcpTests(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }


        IMyRemoteAsyncAwaitTestService currentAsyncAwaitTestServiceClientProxyInstance;


        [Fact]
        public async Task TestMethodDataTransferAsyncImplementation()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            TimeSpan timeout = TimeSpan.FromSeconds(15);

            var ct = new CancellationTokenSource((int)timeout.TotalMilliseconds);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33257;
            var log = new UnitTestLogger(xUnitLog);

            BSAG.IOCTalk.Communication.NetTcp.TcpCommunicationController tcpClient;
            BSAG.IOCTalk.Communication.NetTcp.TcpCommunicationController tcpBackendService;
            MyRemoteAsyncTestService2 localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost("UnitTestService");
                compositionHostService.AddAssembly(typeof(MyRemoteAsyncTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService, MyRemoteAsyncTestService2>();

                
                tcpBackendService = new BSAG.IOCTalk.Communication.NetTcp.TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());
                tcpBackendService.LogDataStream = false;

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (MyRemoteAsyncTestService2)compositionHostService.GetExport<IMyRemoteAsyncAwaitTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("UnitTestClient");
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClient = new BSAG.IOCTalk.Communication.NetTcp.TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());
                tcpClient.LogDataStream = false;
                tcpClient.RequestTimeout = timeout;

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
            MyRemoteAsyncTestService2.RunSomeWorkCounter = 0;
            await currentAsyncAwaitTestServiceClientProxyInstance.RunSomeWork();

            Assert.Equal(1, MyRemoteAsyncTestService2.RunSomeWorkCounter);

            // with async array return
            int expectedArrayLength = 10;
            var response3 = await currentAsyncAwaitTestServiceClientProxyInstance.GetDataAsync3(expectedArrayLength);
            Assert.Equal(expectedArrayLength, response3.Length);


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

            int port = 33258;
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


                tcpBackendService = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());

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

                tcpClient = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());

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
