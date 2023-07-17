using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Binary;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Test.Interface;
using IOCTalk.Communication.WebSocketClient;
using IOCTalk.Communication.WebSocketListener;
using System.Net;
using System.Threading;
using Xunit.Abstractions;

namespace IOCTalk.UnitTests.Websockets
{
    public class WebSocketRoundripTests
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        readonly ITestOutputHelper xUnitLog;

        public WebSocketRoundripTests(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        IMyRemoteAsyncAwaitTestService currentAsyncAwaitTestServiceClientProxyInstance;
        IStressTestService currentStressTestServiceClientProxyInstance;


        [Fact]
        public async Task TestMethodDataTransferAsyncImplementation()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            TimeSpan timeout = TimeSpan.FromSeconds(15);

            var ct = new CancellationTokenSource((int)timeout.TotalMilliseconds);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            var log = new UnitTestLogger(xUnitLog);

            WebSocketClientController websocketClient;
            WebSocketServiceController websocketBackendService;
            MyRemoteAsyncTestService2 localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost("UnitTestService");
                compositionHostService.AddAssembly(typeof(MyRemoteAsyncTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService, MyRemoteAsyncTestService2>();


                websocketBackendService = new WebSocketServiceController(new BinaryMessageSerializer());
                websocketBackendService.LogDataStream = false;

                compositionHostService.InitGenericCommunication(websocketBackendService);

                websocketBackendService.InitWebSocketListener("http://localhost:8383/");

                localService = (MyRemoteAsyncTestService2)compositionHostService.GetExport<IMyRemoteAsyncAwaitTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("UnitTestClient");
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                websocketClient = new WebSocketClientController(new BinaryMessageSerializer());
                websocketClient.LogDataStream = false;
                websocketClient.RequestTimeout = timeout;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated_AsyncTest;

                compositionHostClient.InitGenericCommunication(websocketClient);

                websocketClient.InitWebSocketClient("ws://localhost:8383/");
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


            websocketClient.Shutdown();
            websocketBackendService.Shutdown();
        }



        private void OnCompositionHostClient_SessionCreated_AsyncTest(object contractSession, SessionEventArgs e)
        {
            currentAsyncAwaitTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();
            onConnectionEstablished.SetResult(true);
        }






        [Fact]
        public async Task WebsocketClientServiceStressTest1()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33254;
            var log = new UnitTestLogger(xUnitLog);

            WebSocketClientController websocketClient;
            WebSocketServiceController websocketBackendService;

            StressTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost();
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();


                websocketBackendService = new WebSocketServiceController(new BinaryMessageSerializer());

                compositionHostService.InitGenericCommunication(websocketBackendService);

                websocketBackendService.InitWebSocketListener("http://localhost:8384/");

                localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();

                // async void is not supported within weboscket context ?
                //compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                websocketClient = new WebSocketClientController(new BinaryMessageSerializer());
                websocketClient.LogDataStream = false;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreatedStressTest;

                compositionHostClient.InitGenericCommunication(websocketClient);

                websocketClient.InitWebSocketClient("ws://localhost:8384/");
            }

            Assert.True(await onConnectionEstablished.Task);

            int number = 0;
            for (; number < 2000; number++)
            {
                currentStressTestServiceClientProxyInstance.AsyncCallTest(number);
            }

            for (; number < 4000; number++)
            {
                var result = currentStressTestServiceClientProxyInstance.SyncCallTest(number);
                Assert.Equal(number, result);
            }

            Assert.Equal(number, localService.CurrentNumber);

            string longTestData = "TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST TEST";
            for (; number < 5000; number++)
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

            websocketClient.Shutdown();
            //websocketBackendService.Shutdown();       // blocks sometimes?
        }

        private void OnCompositionHostClient_SessionCreatedStressTest(object contractSession, SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IStressTestService>();
            onConnectionEstablished.SetResult(true);
        }
    }
}