using BSAG.IOCTalk.Composition;
using IOCTalk.UnitTests.Interceptor.Implementation;
using IOCTalk.UnitTests.Interceptor.Interface;
using Xunit.Abstractions;

namespace IOCTalk.UnitTests.Interceptor
{
    public class UnitTestInterceptor
    {
        readonly ITestOutputHelper xUnitLog;

        public UnitTestInterceptor(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
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
    }
}