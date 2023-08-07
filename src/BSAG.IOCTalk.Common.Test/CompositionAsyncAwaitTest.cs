using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Composition;
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

    /// <summary>
    /// Tests verify the remote proxy generation of async/await methods
    /// ValueTask is not supported yet because of current .net Standard 2.0 dependency
    /// </summary>
    public class CompositionAsyncAwaitTest
    {
        TaskCompletionSource<bool> onConnectionEstablishedClient;
        TaskCompletionSource<bool> onConnectionEstablishedService;
        IMyRemoteAsyncAwaitTestService currentAsyncAwaitTestServiceClientProxyInstance;
        IMyRemoteAsyncAwaitTestClient currentAsyncAwaitClientProxyInstance;
        readonly ITestOutputHelper xUnitLog;

        public CompositionAsyncAwaitTest(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }


        [Fact]
        public void TestCreateProxyImplementationAsyncAwaitMethods()
        {
            Type result = TypeService.BuildProxyImplementation(typeof(IMyRemoteAsyncAwaitTestService));

            IMyRemoteAsyncAwaitTestService instance = (IMyRemoteAsyncAwaitTestService)Activator.CreateInstance(result, new object[2]);

            Assert.NotNull(instance);
        }


        [Fact]
        public async Task TestMethodBuildDataTransferInterfaceAsyncImplementation()
        {
            var infra = await InitClientServiceTest();

            var complexTest = await currentAsyncAwaitTestServiceClientProxyInstance.GetObjectDataAsync();
            Assert.Equal(5, complexTest.ID);

            var dataResponse = await currentAsyncAwaitTestServiceClientProxyInstance.GetDataAsync();

            Assert.Equal("Hello world", dataResponse);


            int expected = 23134;
            var response2 = await currentAsyncAwaitTestServiceClientProxyInstance.GetDataAsync2(expected);
            Assert.Equal(expected, response2);

            // without return value
            MyRemoteAsyncTestService.RunSomeWorkCounter = 0;
            await currentAsyncAwaitTestServiceClientProxyInstance.RunSomeWork();

            Assert.Equal(1, MyRemoteAsyncTestService.RunSomeWorkCounter);

            infra.TcpClient.Shutdown();
            infra.TcpBackendService.Shutdown();
        }


        [Fact]
        public async Task TestMethodDataTransferInterfaceClientOtherSideAsyncImplementationOrder1()
        {
            var infra = await InitClientServiceTest();

            var rGetObjectDataID10Async = await currentAsyncAwaitClientProxyInstance.GetObjectDataID10Async();
            Assert.Equal(10, rGetObjectDataID10Async.ID);

            var rGetObjectData = await currentAsyncAwaitClientProxyInstance.GetObjectData();
            Assert.Equal("test", rGetObjectData.MainProperty);

            var rGetObjectData2 = await currentAsyncAwaitClientProxyInstance.GetObjectData2();
            Assert.Equal("test", rGetObjectData2.MainProperty);

            infra.TcpClient.Shutdown();
            infra.TcpBackendService.Shutdown();
        }


        [Fact]
        public async Task TestMethodDataTransferInterfaceClientOtherSideAsyncImplementationOrder2()
        {
            var infra = await InitClientServiceTest();

            var rGetObjectData = await currentAsyncAwaitClientProxyInstance.GetObjectData();
            Assert.Equal("test", rGetObjectData.MainProperty);

            var rGetObjectDataID10Async = await currentAsyncAwaitClientProxyInstance.GetObjectDataID10Async();
            Assert.Equal(10, rGetObjectDataID10Async.ID);

            var rGetObjectData2 = await currentAsyncAwaitClientProxyInstance.GetObjectData2();
            Assert.Equal("test", rGetObjectData2.MainProperty);

            infra.TcpClient.Shutdown();
            infra.TcpBackendService.Shutdown();
        }


        [Fact]
        public async Task TestMethodDataTransferInterfaceClientOtherSideAsyncImplementationOrder3()
        {
            var infra = await InitClientServiceTest();

            var rGetObjectData2 = await currentAsyncAwaitClientProxyInstance.GetObjectData2();
            Assert.Equal("test", rGetObjectData2.MainProperty);

            var rGetObjectData = await currentAsyncAwaitClientProxyInstance.GetObjectData();
            Assert.Equal("test", rGetObjectData.MainProperty);

            var rGetObjectDataID10Async = await currentAsyncAwaitClientProxyInstance.GetObjectDataID10Async();
            Assert.Equal(10, rGetObjectDataID10Async.ID);

            infra.TcpClient.Shutdown();
            infra.TcpBackendService.Shutdown();
        }


        async Task<(TcpCommunicationController TcpClient, TcpCommunicationController TcpBackendService)> InitClientServiceTest()
        {
            onConnectionEstablishedClient = new TaskCompletionSource<bool>();
            onConnectionEstablishedService = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablishedClient.TrySetCanceled(), useSynchronizationContext: false);
            ct.Token.Register(() => onConnectionEstablishedService.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33255;
            var log = new UnitTestLogger(xUnitLog);

            TcpCommunicationController tcpClient;
            TcpCommunicationController tcpBackendService;
            MyRemoteAsyncTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost();
                compositionHostService.AddAssembly(typeof(MyRemoteAsyncTestService).Assembly);

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService, MyRemoteAsyncTestService>();
                compositionHostService.RegisterRemoteService<IMyRemoteAsyncAwaitTestClient>(true);
                compositionHostService.SessionCreated += OnCompositionHostService_SessionCreated;

                tcpBackendService = new TcpCommunicationController(log);
                tcpBackendService.LogDataStream = true;

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (MyRemoteAsyncTestService)compositionHostService.GetExport<IMyRemoteAsyncAwaitTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                //compositionHostClient.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>(true);
                compositionHostClient.RegisterLocalSessionService<IMyRemoteAsyncAwaitTestClient, MyRemoteAsyncTestClient>();
                

                tcpClient = new TcpCommunicationController(log);
                tcpClient.LogDataStream = true;
                tcpClient.RequestTimeoutSeconds = 15;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            Assert.True(await onConnectionEstablishedClient.Task);
            Assert.True(await onConnectionEstablishedService.Task);

            return (tcpClient, tcpBackendService);
        }

        private void OnCompositionHostService_SessionCreated(object contractSession, Session.SessionEventArgs e)
        {
            currentAsyncAwaitClientProxyInstance = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestClient>();
            onConnectionEstablishedService.SetResult(true);
        }

        private void OnCompositionHostClient_SessionCreated(object contractSession, Session.SessionEventArgs e)
        {
            currentAsyncAwaitTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();
            onConnectionEstablishedClient.SetResult(true);
        }
    }
}
