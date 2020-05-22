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
using System.IO;

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
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.RegisterPersistentMethod<IMyLocalService>(nameof(IMyLocalService.RandomMethod));
            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

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


        [Fact]
        public void TestPersistentComplexDataCall()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService();
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.RegisterPersistentMethod<IMyLocalService>(nameof(IMyLocalService.DataMethod));
            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            InvokeMethodInfo mInfo = new InvokeMethodInfo(typeof(IMyLocalService), nameof(IMyLocalService.DataMethod));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");

            string complexDataString = "json within method parameter string: {\"Type\":12,\"RequestId\":8,\"Target\":null,\"Name\":null,\"Payload\":{\"Name\":\"AccessViolationException\",\"TypeName\":\"System.AccessViolationException\",\"Text\":\"System.AccessViolationException: No login received"
                + Environment.NewLine
                + @"   at .GetTimeInfo() in C:\Docs\x.cs:line 109"
               + Environment.NewLine
               + "at BSAG.IOCTalk.Communication.Common.GenericCommunicationBaseService.CallMethod(ISession session, IGenericMessage message) in C:\\GenericCommunicationBaseService.cs:line 1170\",\"Message\":\"No login received\",\"BinaryData\":\"AAEAAAD/////AQAAAAAAAAAEAQAAAB9TeXN0ZW0uQWNjZXNzVmlvbGF0aW9uRXhjZXB0aW9uDAAAAAlDbGFzc05hbWUHTWVzc2FnZQREYXRhDklubmVyRXh3B0aW9uB0hlbHBVUkwQU3RhY2tUcmFjZVN0cmluZxZSZW1vdGVFRyYWNlU3RyaW5nEFJlbW90ZVN0YWNrSW5kZXgPRXhjZXB0aW9uTWV2dXN1bHQGU291cmNlDVdhdHNvbkJ1Y2tldHMBAQMDAQEBAAEAAQceU3lzdGVtLkNvbGxlY3Rpb25zLklEaWN0aW9uYXJ5EFN5c3RlbS5FeGNlcHRpb24ICAIGAgAAAB9TeXN0ZW0uQWNjZXNzVmlvbGF0aW9uRXhjZXB0aW9uBgMAAAARTm8gbG9naW4gcmVjZWl2ZWQKCgoGBAAAAI0EICAgYXQgVGVsZW1hdGljbGluay5CYWNrZW5kLkFjdGl2aXR5VHJhY2tlci5BY3Rpdml0eVRyYWNrZXJDbGllbnlltZUluZm8oKSBpbiBDOlxEb2NzXFh5cGVybGlua1xLdW5kZW5cSW50ZXJuXFRlbGVtYXRpY2xpbmtcVGVsZW1hdGljbGluay5CYWNrZW5kXFRlbGVtYXRpY2xpbmsuQmFja2VuZC5BY3Rpdml0eVRyYWNrZXJcQWN0aXZpdHlUcmFja2VyQ2xpZW50LmNzOmxpbmUgMTA5CiAgIGF0IGxhbWJkYV9tZXRob2QoQ2xvc3VyZSAsIE9iamVjdCAsIE9iamVjdFtdICkKICAgYXQgQlNBRy5JT0NUYWxrLkNvbW11bmljYXRpb24uQ29tbW9uLkdlbmVyaWNDb21tdW5pY2F0aW9uQmFzZVNlcnZpY2UuQ2FsbE1ldGhvZChJU2Vzc2lvbiBzZXNzaW9uLCBJR2VuZXJpY01lc3NhZ2UgbWVzc2FnZSkgaW4gQzpcVXNlcnNcYmVuXFNvdXJjZVxSZXBvc1xpb2N0YWxrLWdpdGh1YlxzcmNcQlNBRy5JT0NUYWxrLkNvbW11bmljYXRpb24uQ29tbW9uXEdlbmVyaWNDb21tdW5pY2F0aW9uQmFzZVNlcnZpY2UuY3M6bGluZSAxMTcwC3gAAAAAKA0AAgAYFAAAAJVRlbGVtYXRpY2xpbmsuQmFja2VuZC5BY3Rpdml0eVRyYWNrZXIKCw==\"}}\" ";

            object[] complexData = new object[] { 1, DateTime.UtcNow, complexDataString };

            persistComm.InvokeMethod(this, mInfo, session, complexData);  // 1. not connected call

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed
            Thread.Sleep(500);

            Assert.Equal(1, dummyCom.InvokeCounter);
        }

        private static void CleanupPeristentDirectory(PersistentClientCommunicationHost persistComm)
        {
            string persistPath = Path.GetFullPath(persistComm.DirectoryPath);
            if (Directory.Exists(persistPath))
            {
                Directory.Delete(persistPath, true);
            }
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
            public TimeSpan RequestTimeout { get; set; }

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
