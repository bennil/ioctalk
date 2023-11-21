using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Test.Common.Service.Expose;
using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Common.Test
{
    public class ExposeSubTypeRoundTripTest
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        IExposeSubTypeRoundTripTest currentServiceClientProxyInstance;
        readonly ITestOutputHelper xUnitLog;

        public ExposeSubTypeRoundTripTest(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        [Fact]
        public async Task ExposeSubTypeBaseTest1()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 31251;
            var log = new UnitTestLogger(xUnitLog);

            TcpCommunicationController tcpClient;
            TcpCommunicationController tcpBackendService;

            ExposeSubTypeRoundTripTestService localService;
            {
                // init service
                var compositionHostService = new TalkCompositionHost();
                compositionHostService.AddAssembly(typeof(ExposeSubTypeRoundTripTest).Assembly);
                compositionHostService.MapInterfaceImplementationType<IExposeTestBase, ExposeTestBase>();
                compositionHostService.MapInterfaceImplementationType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostService.MapInterfaceImplementationType<IExposeTestOther, ExposeTestBase>();

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IExposeSubTypeRoundTripTest, ExposeSubTypeRoundTripTestService>();

                compositionHostService.RegisterExposedSubInterfaceForType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostService.RegisterExposedSubInterfaceForType<IExposeTestOther, ExposeTestBase>();

                tcpBackendService = new TcpCommunicationController(log);

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (ExposeSubTypeRoundTripTestService)compositionHostService.GetExport<IExposeSubTypeRoundTripTest>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                compositionHostClient.AddAssembly(typeof(ExposeSubTypeRoundTripTest).Assembly);
                compositionHostClient.MapInterfaceImplementationType<IExposeTestBase, ExposeTestBase>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTestOther, ExposeTestBase>();

                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IExposeSubTypeRoundTripTest>();
                compositionHostClient.RegisterExposedSubInterfaceForType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostClient.RegisterExposedSubInterfaceForType<IExposeTestOther, ExposeTestBase>();

                tcpClient = new TcpCommunicationController(log);
                tcpClient.LogDataStream = true;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            

            Assert.True(await onConnectionEstablished.Task);


            var firstSend = new ExposeTestLevel1 { TestId = 1, TestLevel1 = "input" };
            var result1 = currentServiceClientProxyInstance.TestExposeTypeMain(firstSend);

            Assert.Equal(firstSend.GetType(), result1.GetType());
            Assert.Equal(firstSend.TestId, result1.TestId);

            var otherSend = new ExposeTestLevel1 { TestId = 2, OtherTypeProperty = 2 };
            var result2 = currentServiceClientProxyInstance.TestExposeTypeOther(otherSend);

            Assert.Equal(typeof(ExposeTestBase), result2.GetType());
            Assert.Equal(otherSend.OtherTypeProperty, result2.OtherTypeProperty);

            tcpClient.Shutdown();
            tcpBackendService.Shutdown();
        }

        private void OnCompositionHostClient_SessionCreated(object contractSession, Session.SessionEventArgs e)
        {
            currentServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IExposeSubTypeRoundTripTest>();
            onConnectionEstablished.SetResult(true);
        }
    }
}
