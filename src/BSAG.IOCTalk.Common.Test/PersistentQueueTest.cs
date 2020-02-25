using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Communication.PersistentQueue;
using System.Threading;

namespace BSAG.IOCTalk.Common.Test
{
    public class PersistentQueueTest
    {
        [Fact]
        public void TestPersistentCall()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();
            
            PersistentTestCommService dummyCom = new PersistentTestCommService();
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.RegisterPersistentMethod<IMyLocalService>(nameof(IMyLocalService.RandomMethod));
            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            InvokeMethodInfo mInfo = new InvokeMethodInfo(typeof(IMyLocalService), nameof(IMyLocalService.RandomMethod));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            persistComm.InvokeMethod(this, mInfo, session, new object[0]);  // 1. not connected call
            persistComm.InvokeMethod(this, mInfo, session, new object[0]);  // 2. not connected call

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed
            Thread.Sleep(500);

            Assert.Equal(2, dummyCom.InvokeCounter);
        }


        public class PersistentTestCommService : IGenericCommunicationService, ILogger
        {
            private IGenericContainerHost containerHost;
            public IGenericContainerHost ContainerHost => containerHost;

            public string SerializerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            private IGenericMessageSerializer serializer = new BSAG.IOCTalk.Serialization.Json.JsonMessageSerializer();
            public IGenericMessageSerializer Serializer => serializer;

            public string LoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ILogger Logger => this;

            public bool LogDataStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string DataStreamLoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IDataStreamLogger DataStreamLogger => throw new NotImplementedException();

            public ISession[] ClientSessions => throw new NotImplementedException();

            public InvokeThreadModel InvokeThreadModel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public event EventHandler<SessionEventArgs> SessionCreated;
            public event EventHandler<SessionEventArgs> SessionTerminated;

            public void Init()
            {
            }

            public void RaiseConnectionCreated()
            {
                if (SessionCreated != null)
                    SessionCreated(this, new SessionEventArgs(new Session.Session(this, 0, "new unit test session"), null));
            }

            public bool RaiseConnectionLost { get; set; }

            public int InvokeCounter { get; set; }

            public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
            {
                if (RaiseConnectionLost)
                {
                    throw new OperationCanceledException("Session lost unit test dummy");
                }
                else
                {
                    InvokeCounter++;
                    return null;
                }
            }

            public void RegisterContainerHost(IGenericContainerHost containerHost, ILogger logger)
            {
                this.containerHost = containerHost;
                this.Serializer.RegisterContainerHost(containerHost);
            }

            public void Shutdown()
            {
                throw new NotImplementedException();
            }

            void ILogger.Debug(string message)
            {
            }

            void ILogger.Info(string message)
            {
            }

            void ILogger.Warn(string message)
            {
            }

            void ILogger.Error(string message)
            {
                throw new Exception(message);
            }
        }
    }
}
