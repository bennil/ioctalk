using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Json;
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

                // Test4
                compositionHostService.MapInterfaceImplementationType<IExposeTest4Base, ExposeTest4Combined>();
                compositionHostService.MapInterfaceImplementationType<IExposeTest4Sub, ExposeTest4Combined>();

                tcpBackendService = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());

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

                // Test4
                compositionHostClient.MapInterfaceImplementationType<IExposeTest4Base, ExposeTest4Combined>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTest4Sub, ExposeTest4Combined>();

                tcpClient = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
                tcpClient.LogDataStream = true;

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;

                compositionHostClient.InitGenericCommunication(tcpClient);

                tcpClient.InitClient(IPAddress.Loopback.ToString(), port);
            }

            

            Assert.True(await onConnectionEstablished.Task);


            //var firstSend = new ExposeTestLevel1 { TestId = 1, TestLevel1 = "input" };
            //var result1 = currentServiceClientProxyInstance.TestExposeTypeMain(firstSend);

            //Assert.Equal(firstSend.GetType(), result1.GetType());
            //Assert.Equal(firstSend.TestId, result1.TestId);

            var otherSend = new ExposeTestLevel1 { TestId = 2, OtherTypeProperty = 2 };
            var result2 = currentServiceClientProxyInstance.TestExposeTypeOther(otherSend);

            //Assert.Equal(typeof(ExposeTestBase), result2.GetType());
            //Assert.Equal(otherSend.OtherTypeProperty, result2.OtherTypeProperty);


            // test 4 - same concrete class as base interface first, then as derived interface
            {
                // response direction: base first > only base surface expected
                var base1 = (ExposeTest4Combined)currentServiceClientProxyInstance.GetTest4Base();
                Assert.Equal("base data", base1.BaseProperty);
                Assert.Null(base1.SubProperty);

                // derived interface must transfer the sub property although the same concrete type was cached as base before
                var sub1 = (ExposeTest4Combined)currentServiceClientProxyInstance.GetTest4Sub();
                Assert.Equal("base data", sub1.BaseProperty);
                Assert.Equal("sub data expected", sub1.SubProperty);

                // base again > still only base surface
                var base2 = (ExposeTest4Combined)currentServiceClientProxyInstance.GetTest4Base();
                Assert.Equal("base data", base2.BaseProperty);
                Assert.Null(base2.SubProperty);

                // request direction (object[] parameter items)
                var combined = new ExposeTest4Combined { BaseProperty = "base", SubProperty = "sub" };
                Assert.Null(currentServiceClientProxyInstance.SendTest4Base(combined));
                Assert.Equal("sub", currentServiceClientProxyInstance.SendTest4Sub(combined));
                Assert.Null(currentServiceClientProxyInstance.SendTest4Base(combined));
                Assert.Equal("sub", currentServiceClientProxyInstance.SendTest4Sub(combined));
            }

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
