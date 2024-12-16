using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Json;
using IOCTalk.UnitTests.Interceptor.Implementation;
using IOCTalk.UnitTests.Interceptor.Interface;
using Xunit.Abstractions;

namespace IOCTalk.UnitTests.Interceptor
{
    public class UnitTestInterceptor
    {
        readonly ITestOutputHelper xUnitLog;
        readonly UnitTestLogger logger;
        public UnitTestInterceptor(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
            this.logger = new UnitTestLogger(xUnitLog);
        }

        [Fact]
        public void TestInterceptServiceImplementation()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);

            localShareContext.RegisterLocalSharedService<IMyImportantService, MyImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceLogInterception>();

            localShareContext.Init();

            IMyImportantService myImportantService = localShareContext.GetExport<IMyImportantService>();

            // expected intercepted implementation
            Assert.IsType<MyImportantServiceLogInterception>(myImportantService);


            myImportantService.Multiply(2, 2);
        }

        [Fact]
        public void TestInterceptServiceImplementation2Encapsulations()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);

            localShareContext.RegisterLocalSharedService<IMyImportantService, MyImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceLogInterception>()
                                .InterceptWithImplementation<MyImportantServiceCache>();

            localShareContext.Init();

            MyImportantServiceCache myImportantService = (MyImportantServiceCache)localShareContext.GetExport<IMyImportantService>();

            myImportantService.Multiply(2, 2);
            myImportantService.Multiply(4, 4);
            myImportantService.Multiply(4, 4);

            // expect 1 cache hit count
            Assert.Equal(1, myImportantService.CacheHitCount);


            Assert.IsType<MyImportantServiceLogInterception>(myImportantService.NestedService);
            MyImportantServiceLogInterception logInterception = (MyImportantServiceLogInterception)myImportantService.NestedService;

            // expect actual implementation
            Assert.IsType<MyImportantServiceImplementation>(logInterception.NestedService);
        }



        [Fact]
        public void TestInterceptServiceImplementationYEncapsulations()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);

            localShareContext.RegisterLocalSharedService<IMyImportantService, MyImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceYRouter>()
                                    .AddMultiImportBranchImplementation<MyDifferentImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceLogInterception>();

            localShareContext.Init();

            IMyImportantService myImportantService = localShareContext.GetExport<IMyImportantService>();

            myImportantService.Multiply(1, 1);
            myImportantService.Multiply(2, 2);
            myImportantService.Multiply(3, 3);
            myImportantService.Multiply(4, 4);


            Assert.IsType<MyImportantServiceLogInterception>(myImportantService);
            MyImportantServiceLogInterception logInterception = (MyImportantServiceLogInterception)myImportantService;

            // expect actual implementation
            Assert.IsType<MyImportantServiceYRouter>(logInterception.NestedService);

            MyImportantServiceYRouter yRouter = (MyImportantServiceYRouter)logInterception.NestedService;

            Assert.IsType<MyImportantServiceImplementation>(yRouter.Service1);
            Assert.IsType<MyDifferentImportantServiceImplementation>(yRouter.Service2);

            MyImportantServiceImplementation service1 = (MyImportantServiceImplementation)yRouter.Service1;
            MyDifferentImportantServiceImplementation service2 = (MyDifferentImportantServiceImplementation)yRouter.Service2;

            Assert.Equal(2, service1.CallCount);
            Assert.Equal(2, service2.CallCount);
        }


        [Fact]
        public void TestInterceptServiceImplementationYEncapsulationsLastLevel()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);

            localShareContext.RegisterLocalSharedService<IMyImportantService, MyImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceLogInterception>()
                                .InterceptWithImplementation<MyImportantServiceYRouter>()
                                    .AddMultiImportBranchImplementation<MyDifferentImportantServiceImplementation>();

            localShareContext.Init();

            IMyImportantService myImportantService = localShareContext.GetExport<IMyImportantService>();

            myImportantService.Multiply(1, 1);
            myImportantService.Multiply(2, 2);
            myImportantService.Multiply(3, 3);
            myImportantService.Multiply(4, 4);


            MyImportantServiceYRouter yRouter = (MyImportantServiceYRouter)myImportantService;

            Assert.IsType<MyImportantServiceLogInterception>(yRouter.Service1);
            Assert.IsType<MyDifferentImportantServiceImplementation>(yRouter.Service2);

            MyImportantServiceLogInterception logInterception = (MyImportantServiceLogInterception)yRouter.Service1;
            MyDifferentImportantServiceImplementation service2 = (MyDifferentImportantServiceImplementation)yRouter.Service2;

            MyImportantServiceImplementation mainService = (MyImportantServiceImplementation)logInterception.NestedService;

            Assert.Equal(2, mainService.CallCount);
            Assert.Equal(2, service2.CallCount);
        }


        [Fact]
        public void TestInterceptServiceImplementationYEncapsulationsIndirectInjection()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);

            localShareContext.RegisterLocalSharedService<IOtherTestService, OtherTestService>();

            localShareContext.RegisterLocalSharedService<IMyImportantService, MyImportantServiceImplementation>()
                                .InterceptWithImplementation<MyImportantServiceLogInterception>()
                                //.InterceptWithImplementation<MyImportantServiceYRouter>()
                                .InterceptWithImplementation(typeof(MyImportantServiceYRouter))
                                    .AddMultiImportBranchImplementation<MyDifferentImportantServiceImplementation>();

            localShareContext.Init();

            IOtherTestService otherTestService = localShareContext.GetExport<IOtherTestService>();
            IMyImportantService myImportantService = otherTestService.InjectedService;

            myImportantService.Multiply(1, 1);
            myImportantService.Multiply(2, 2);
            myImportantService.Multiply(3, 3);
            myImportantService.Multiply(4, 4);


            MyImportantServiceYRouter yRouter = (MyImportantServiceYRouter)myImportantService;

            Assert.IsType<MyImportantServiceLogInterception>(yRouter.Service1);
            Assert.IsType<MyDifferentImportantServiceImplementation>(yRouter.Service2);

            MyImportantServiceLogInterception logInterception = (MyImportantServiceLogInterception)yRouter.Service1;
            MyDifferentImportantServiceImplementation service2 = (MyDifferentImportantServiceImplementation)yRouter.Service2;

            MyImportantServiceImplementation mainService = (MyImportantServiceImplementation)logInterception.NestedService;

            Assert.Equal(2, mainService.CallCount);
            Assert.Equal(2, service2.CallCount);
        }


        #region Session registration tests

        TaskCompletionSource sessionProcessedClient;
        TaskCompletionSource sessionProcessedServer;

        [Fact]
        public async Task TestInterceptServiceImplementationSession()
        {
            LocalShareContext localShareContext = new LocalShareContext(nameof(UnitTestInterceptor));
            localShareContext.RegisterLocalSharedService<ITestOutputHelper>(xUnitLog);
            localShareContext.RegisterLocalSharedService<ILogger>(logger);

            var tcpMyService = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            var compositionHostService = new TalkCompositionHost(localShareContext, "MyService");

            compositionHostService.RegisterLocalSessionService<IMyRemoteService, MyRemoteServiceImplementation>()
                    .InterceptWithImplementation<MyRemoteServiceInterceptionImplementation>();

            compositionHostService.SessionCreated += OnCompositionHostService_SessionCreated;

            var tcpMyClient = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            var compositionHostClient = new TalkCompositionHost(localShareContext, "MyClient");


            compositionHostClient.RegisterRemoteService<IMyRemoteService>()
                    .InterceptWithImplementation<MyRemoteServiceClientInterception>();

            compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreated;
            
            sessionProcessedClient = new TaskCompletionSource();
            sessionProcessedServer = new TaskCompletionSource();

            compositionHostService.InitGenericCommunication(tcpMyService);

            compositionHostClient.InitGenericCommunication(tcpMyClient);

            // bind to tcp port 14341
            tcpMyService.InitService(14341);

            tcpMyClient.InitClient("127.0.0.1", 14341);

            await sessionProcessedClient.Task.WaitAsync(TimeSpan.FromSeconds(25));
            await sessionProcessedServer.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        void OnCompositionHostClient_SessionCreated(object contractSession, BSAG.IOCTalk.Common.Session.SessionEventArgs e)
        {
            try
            {
                IMyRemoteService myRemoteServiceClient = e.SessionContract.GetSessionInstance<IMyRemoteService>();

                Assert.IsType<MyRemoteServiceClientInterception>(myRemoteServiceClient);

                string bar = "Test";
                string result = myRemoteServiceClient.Foo(bar);
                Assert.Contains(bar, result);

                var clientInterception = (MyRemoteServiceClientInterception)myRemoteServiceClient;
                Assert.Equal(1, clientInterception.CallCounter);

                sessionProcessedClient.SetResult();
            }
            catch (Exception ex)
            {
                sessionProcessedClient.SetException(ex);
            }
        }

        async void OnCompositionHostService_SessionCreated(object contractSession, BSAG.IOCTalk.Common.Session.SessionEventArgs e)
        {
            try
            {
                IMyRemoteService myRemoteServiceClient = e.SessionContract.GetSessionInstance<IMyRemoteService>();

                Assert.IsType<MyRemoteServiceInterceptionImplementation>(myRemoteServiceClient);

                // wait for client call
                await sessionProcessedClient.Task.WaitAsync(TimeSpan.FromSeconds(25));

                var serviceInterception = (MyRemoteServiceInterceptionImplementation)myRemoteServiceClient;
                Assert.Equal(1, serviceInterception.ServiceCallCount);

                sessionProcessedServer.SetResult();
            }
            catch (Exception ex)
            {
                sessionProcessedServer.SetException(ex);
            }
        }

        #endregion
    }
}