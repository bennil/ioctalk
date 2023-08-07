using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Session;
using System.Linq.Expressions;
using System.Reflection;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Common.Test.TestObjects.InterfaceMapTest;
using BSAG.IOCTalk.Common.Exceptions;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Common.Test
{
    public class CompositionTest
    {
        [Fact]
        public void TestMethodInitCompositionContainer()
        {
            var t = typeof(IOCTalk.Test.Common.Service.MyTestService);

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSessionService<IMyTestService>();
            hostContainer.RegisterRemoteService<IMyRemoteTestService>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            SessionContract contract = (SessionContract)hostContainer.CreateSessionContractInstance(session);

            Assert.Equal(contract.Session, session);

            object proxyTypeObj = hostContainer.GetInterfaceImplementationInstance(session, typeof(IMyRemoteTestService).FullName);

            Assert.IsAssignableFrom<IMyRemoteTestService>(proxyTypeObj);
        }

        [Fact]
        public void TestMethodBuildDataTransferInterfaceImplementation()
        {
            var t = typeof(IOCTalk.Test.Common.Service.MyTestService);

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSessionService<IMyTestService>();
            hostContainer.RegisterRemoteService<IMyRemoteTestService>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            var result = hostContainer.CreateSessionContractInstance(session);

            Type resultType = hostContainer.GetInterfaceImplementationType(typeof(IDataTransferTest).FullName);
        }

        [Fact]
        public void TestMethodSpecialInterfaceClassMapping()
        {
            var t = typeof(IOCTalk.Test.Common.Service.MyTestService);

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSessionService<IMyTestService>();
            hostContainer.RegisterRemoteService<IMyRemoteTestService>();
            hostContainer.RegisterExposedSubInterfaceForType<IMapTestMainInterface, TestObjects.InterfaceMapTest.MapTestMain>();
            hostContainer.RegisterExposedSubInterfaceForType<IMapTestDerivedInterface, MapTestDerived>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            var result = hostContainer.CreateSessionContractInstance(session);

            Type resultTypeMain = hostContainer.GetInterfaceImplementationType(typeof(IMapTestMainInterface).FullName);
            Assert.Equal(typeof(TestObjects.InterfaceMapTest.MapTestMain), resultTypeMain);

            Type resultTypeDerived = hostContainer.GetInterfaceImplementationType(typeof(IMapTestDerivedInterface).FullName);
            Assert.Equal(typeof(MapTestDerived), resultTypeDerived);
        }



        [Fact]
        public void TestMethodMutualLocalImports()
        {
            MyLocalService.InstanceCount = 0;
            OtherLocalService.InstanceCount = 0;

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSessionService<IMyLocalService>();
            hostContainer.RegisterLocalSessionService<IOtherLocalService>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            var result = hostContainer.CreateSessionContractInstance(session);

            Assert.Equal(1, MyLocalService.InstanceCount);
            Assert.Equal(1, OtherLocalService.InstanceCount);
        }

        [Fact]
        public void TestMethodMutualLocalImportsDifferentOrderCheck()
        {
            MyLocalService.InstanceCount = 0;
            OtherLocalService.InstanceCount = 0;

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSessionService<IMyLocalService>();
            hostContainer.RegisterLocalSessionService<IOtherLocalService2>();
            hostContainer.RegisterLocalSessionService<IOtherLocalService>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            var result = hostContainer.CreateSessionContractInstance(session);

            Assert.Equal(1, MyLocalService.InstanceCount);
            Assert.Equal(1, OtherLocalService.InstanceCount);
            Assert.Equal(1, OtherLocalService2.InstanceCount);
        }

        [Fact]
        public void TestMethodMultipleLocalImports()
        {
            MyLocalService.InstanceCount = 0;
            OtherLocalService.InstanceCount = 0;

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSharedService<IMultipleLocalImportsService>();
            hostContainer.RegisterLocalSharedServices<IMultipleImplementation>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);
                        
            var multiImportsService = hostContainer.GetExport<IMultipleLocalImportsService>();

            Assert.Equal(2, multiImportsService.LocalImplementations.Length);            
        }


        [Fact]
        public void TestMethodSubscribeSessionStateChange()
        {
            AdvancedImportService.CreatedCount = 0;

            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            //hostContainer.RegisterLocalSessionService<IAdvancedSessionStateChangeService>();
            hostContainer.RegisterLocalSharedService<IAdvancedSessionStateChangeService>();
            hostContainer.RegisterRemoteService<IMyTestService>();
            hostContainer.AddReferencedAssemblies();

            hostContainer.InitGenericCommunication(dummyComm);

            Common.Session.Session session = new Common.Session.Session(dummyComm, 123, "Unit Test Dummy Session");

            var contract = hostContainer.CreateSessionContractInstance(session);

            dummyComm.RaiseSessionCreated(session, contract);

            Assert.Equal(1, AdvancedImportService.CreatedCount);

            dummyComm.RaiseSessionTerminated(session, contract);

            Assert.Equal(0, AdvancedImportService.CreatedCount);
        }


        [Fact]
        public void CircularDependencyRegognitionTest()
        {
            DummyCommunicationService dummyComm = new DummyCommunicationService();

            TalkCompositionHost hostContainer = new TalkCompositionHost();
            hostContainer.RegisterLocalSharedService<ICircularDependencyTest1>();
            hostContainer.RegisterLocalSharedServices<ICircularDependencyTest2>();
            hostContainer.AddReferencedAssemblies();

            Assert.Throws<CircularServiceReferenceException>(() => hostContainer.InitGenericCommunication(dummyComm));
        }


        [Fact]
        public void TestMethodImplementationMapping()
        {
            DummyCommunicationService dummyComm = new DummyCommunicationService();

            LocalShareContext localShareContext = new LocalShareContext();
            localShareContext.MapInterfaceImplementationType<IMyLocalService, MyLocalService>();

            localShareContext.RegisterLocalSharedService<IMyLocalService>();

            localShareContext.Init();

            var myService = localShareContext.GetExport<IMyLocalService>();

            Assert.Equal(typeof(MyLocalService), myService.GetType());
        }

        [Fact]
        public void TestMethodImplementationMapping2()
        {
            DummyCommunicationService dummyComm = new DummyCommunicationService();

            LocalShareContext localShareContext = new LocalShareContext();

            localShareContext.RegisterLocalSharedService<IMyLocalService, MyLocalService>();

            localShareContext.Init();

            var myService = localShareContext.GetExport<IMyLocalService>();

            Assert.Equal(typeof(MyLocalService), myService.GetType());
        }
    }


    public class DummyCommunicationService : IGenericCommunicationService, ILogger
    {
        public IGenericContainerHost ContainerHost => throw new NotImplementedException();

        public string SerializerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IGenericMessageSerializer Serializer => throw new NotImplementedException();

        public string LoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ILogger Logger => this;

        public bool LogDataStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DataStreamLoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IDataStreamLogger DataStreamLogger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ISession[] ClientSessions => throw new NotImplementedException();

        public CreateSessionHandler CustomCreateSessionHandler { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public InvokeThreadModel InvokeThreadModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan RequestTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<SessionEventArgs> SessionCreated;
        public event EventHandler<SessionEventArgs> SessionTerminated;



        public void Init()
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod<T>(object source, Expression<Action<T>> expression)
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod(object source, MethodInfo method, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            //throw new NotImplementedException();
            return null;
        }

        public async Task<object> InvokeMethodAsync(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
        {
            return null;
        }

        public void RegisterContainerHost(IGenericContainerHost containerHost, ILogger logger)
        {
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Warn(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Error(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Info(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);

        }

        public void RaiseSessionCreated(ISession session, IContract contract)
        {
            if (SessionCreated != null)
                SessionCreated(this, new SessionEventArgs(session, contract));
        }


        public void RaiseSessionTerminated(ISession session, IContract contract)
        {
            if (SessionTerminated != null)
                SessionTerminated(this, new SessionEventArgs(session, contract));
        }


    }
}
