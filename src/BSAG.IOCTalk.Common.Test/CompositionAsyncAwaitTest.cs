using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Reflection;
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
        TaskCompletionSource<bool> onConnectionEstablished;
        IMyRemoteAsyncAwaitTestService currentAsyncAwaitTestServiceClientProxyInstance;
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
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

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

                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService>();


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

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClient = new TcpCommunicationController(log);
                tcpClient.LogDataStream = true;
                tcpClient.RequestTimeoutSeconds = 15;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

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





        private void OnCompositionHostClient_SessionCreated(object contractSession, Session.SessionEventArgs e)
        {
            currentAsyncAwaitTestServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();
            onConnectionEstablished.SetResult(true);
        }
    }
}
