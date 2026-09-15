using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
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

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    public class ExposeSubTypeRoundTripTestBinary
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        IExposeSubTypeRoundTripTest currentServiceClientProxyInstance;
        readonly ITestOutputHelper xUnitLog;

        public ExposeSubTypeRoundTripTestBinary(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        [Fact]
        public async Task ExposeSubTypeBaseTest1Binary()
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
                compositionHostService.AddAssembly(typeof(ExposeSubTypeRoundTripTestBinary).Assembly);
                compositionHostService.MapInterfaceImplementationType<IExposeTestBase, ExposeTestBase>();
                compositionHostService.MapInterfaceImplementationType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostService.MapInterfaceImplementationType<IExposeTestOther, ExposeTestBase>();

                compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IExposeSubTypeRoundTripTest, ExposeSubTypeRoundTripTestService>();

                compositionHostService.RegisterExposedSubInterfaceForType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostService.RegisterExposedSubInterfaceForType<IExposeTestOther, ExposeTestBase>();

                // Test2: interface > interface expose
                compositionHostService.RegisterExposedSubInterfaceForType<IExposeTest2Level1, IExposeTest2Base>();
                compositionHostService.MapInterfaceImplementationType<IExposeTest2Base, ExposeTest2Base>();
                compositionHostService.MapInterfaceImplementationType<IExposeTest2Level1, ExposeTest2Level1>();

                // Test4
                compositionHostService.MapInterfaceImplementationType<IExposeTest4Base, ExposeTest4Combined>();
                compositionHostService.MapInterfaceImplementationType<IExposeTest4Sub, ExposeTest4Combined>();

                tcpBackendService = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());

                compositionHostService.InitGenericCommunication(tcpBackendService);

                tcpBackendService.InitService(port);

                localService = (ExposeSubTypeRoundTripTestService)compositionHostService.GetExport<IExposeSubTypeRoundTripTest>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost();
                compositionHostClient.AddAssembly(typeof(ExposeSubTypeRoundTripTestBinary).Assembly);
                compositionHostClient.MapInterfaceImplementationType<IExposeTestBase, ExposeTestBase>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTestOther, ExposeTestBase>();

                compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IExposeSubTypeRoundTripTest>();
                compositionHostClient.RegisterExposedSubInterfaceForType<IExposeTestLevel1, ExposeTestLevel1>();
                compositionHostClient.RegisterExposedSubInterfaceForType<IExposeTestOther, ExposeTestBase>();

                // Test2: interface > interface expose
                compositionHostClient.RegisterExposedSubInterfaceForType<IExposeTest2Level1, IExposeTest2Base>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTest2Base, ExposeTest2Base>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTest2Level1, ExposeTest2Level1>();

                // Test3
                compositionHostClient.MapInterfaceImplementationType<IExposeTest3Base, ExposeTest3Combined>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTest3Other, ExposeTest3Combined>();

                // Test4
                compositionHostClient.MapInterfaceImplementationType<IExposeTest4Base, ExposeTest4Combined>();
                compositionHostClient.MapInterfaceImplementationType<IExposeTest4Sub, ExposeTest4Combined>();

                tcpClient = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());
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

            // collection test
            var items = currentServiceClientProxyInstance.GetExposedCollection();

            // expect 1 = base
            Assert.Equal(typeof(ExposeTestBase), items[0].GetType());
            // expect 2 = level 1
            Assert.Equal(typeof(ExposeTestLevel1), items[1].GetType());


            // check exposed derived interface
            var baseInput = new ExposeTest2Base { BaseProperty = 5 };
            var test2Result1 = (ExposeTest2Level1)currentServiceClientProxyInstance.ExposeDerivedInterfaceTest(baseInput);
            Assert.Null(test2Result1.Level1Property);

            var level1Input = new ExposeTest2Level1 { BaseProperty = 5, Level1Property = "level1" };
            var test2Result2 = (ExposeTest2Level1)currentServiceClientProxyInstance.ExposeDerivedInterfaceTest(level1Input);
            Assert.Equal(level1Input.Level1Property, test2Result2.Level1Property);
            
            // test 2 - nested
            var test2containerResult1 = currentServiceClientProxyInstance.ExposeDerivedInterfaceContainerTest(new ExposeTest2Container { NestedItem = baseInput });
            Assert.Equal(typeof(ExposeTest2Level1), test2containerResult1.NestedItem.GetType());
            Assert.Null(((ExposeTest2Level1)test2containerResult1.NestedItem).Level1Property);

            var test2containerResult2 = currentServiceClientProxyInstance.ExposeDerivedInterfaceContainerTest(new ExposeTest2Container { NestedItem = level1Input });
            Assert.Equal(level1Input.Level1Property, ((ExposeTest2Level1)test2containerResult2.NestedItem).Level1Property);


            // test 3
            var result3Items = currentServiceClientProxyInstance.GetTest3BaseItems();
            // only IExposeTest3Base surface is expected
            foreach (ExposeTest3Combined item in result3Items)
            {
                Assert.NotNull(item.SomeBasicProperty);
                Assert.Null(item.OtherTypeProperty);
            }

            // only other property is expected
            var otherItem = (ExposeTest3Combined)currentServiceClientProxyInstance.GetTest3OtherItem();
            Assert.Null(otherItem.SomeBasicProperty);
            Assert.NotNull(otherItem.OtherTypeProperty);

            // request items again
            var result3Items2 = currentServiceClientProxyInstance.GetTest3BaseItems();
            // only IExposeTest3Base surface is expected
            foreach (ExposeTest3Combined item in result3Items2)
            {
                Assert.NotNull(item.SomeBasicProperty);
                Assert.Null(item.OtherTypeProperty);
            }


            // test 4 - same concrete class as base interface first, then as derived interface (response direction)
            AssertTest4(currentServiceClientProxyInstance);


            tcpClient.Shutdown();
            tcpBackendService.Shutdown();
        }

        internal static void AssertTest4(IExposeSubTypeRoundTripTest proxy)
        {
            // response direction: base first > only base surface expected
            var base1 = (ExposeTest4Combined)proxy.GetTest4Base();
            Assert.Equal("base data", base1.BaseProperty);
            Assert.Null(base1.SubProperty);

            // derived interface must transfer the sub property although the same concrete type was cached as base before
            var sub1 = (ExposeTest4Combined)proxy.GetTest4Sub();
            Assert.Equal("base data", sub1.BaseProperty);
            Assert.Equal("sub data expected", sub1.SubProperty);

            // base again > still only base surface
            var base2 = (ExposeTest4Combined)proxy.GetTest4Base();
            Assert.Equal("base data", base2.BaseProperty);
            Assert.Null(base2.SubProperty);

            // request direction (object[] parameter items)
            var combined = new ExposeTest4Combined { BaseProperty = "base", SubProperty = "sub" };
            Assert.Null(proxy.SendTest4Base(combined));
            Assert.Equal("sub", proxy.SendTest4Sub(combined));
            Assert.Null(proxy.SendTest4Base(combined));
            Assert.Equal("sub", proxy.SendTest4Sub(combined));
        }

        private void OnCompositionHostClient_SessionCreated(object contractSession, SessionEventArgs e)
        {
            currentServiceClientProxyInstance = e.SessionContract.GetSessionInstance<IExposeSubTypeRoundTripTest>();
            onConnectionEstablished.SetResult(true);
        }
    }
}
