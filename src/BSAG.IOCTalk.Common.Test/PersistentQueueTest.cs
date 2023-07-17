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
using BSAG.IOCTalk.Communication.PersistentQueue.Transaction;
using System.Threading.Tasks;
using Xunit.Abstractions;
using BSAG.IOCTalk.Composition;

namespace BSAG.IOCTalk.Common.Test
{
    public class PersistentQueueTest
    {
        private readonly ITestOutputHelper xUnitLog;

        public PersistentQueueTest(ITestOutputHelper output)
        {
            this.xUnitLog = output;
        }

        [Fact]
        public void TestPersistentCall()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
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

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
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



        [Fact]
        public void TestPersistentTransactionContextValue()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.ResendDelay = TimeSpan.Zero;

            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn });  // transaction commit

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed
            Thread.Sleep(500);

            Assert.Equal(3, dummyCom.InvokeCounter);

            Assert.Equal(2, dummyCom.ReceivedParameterList.Count);
            foreach (var itemArr in dummyCom.ReceivedParameterList)
            {
                Assert.Equal(dummyCom.TransactionTestGuid, itemArr[0]);
            }
        }


        [Fact]
        public void TestPersistentTransactionContext_LooseConnDuringSend()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;


            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            // loose connection
            persistComm.RealUnderlyingSession.Close();

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn });  // transaction commit

            // raise connection again
            var firstOnlineCallGuid = dummyCom.TransactionTestGuid;
            dummyCom.TransactionTestGuid = Guid.NewGuid(); // set new Guid
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed
            Thread.Sleep(500);

            // 8 calls = 3 online transaction till first conn lost + 5 calls on complete transaction resend
            Assert.Equal(8, dummyCom.InvokeCounter);

            // expect 6 parameter valus (8 - 2 x Start method)
            Assert.Equal(6, dummyCom.ReceivedParameterList.Count);
            for (int i = 0; i < dummyCom.ReceivedParameterList.Count; i++)
            {
                var itemArr = dummyCom.ReceivedParameterList[i];

                if (i <= 1)
                {
                    Assert.Equal(firstOnlineCallGuid, itemArr[0]);
                }
                else
                {
                    // expect new guid for complete transaction resend
                    Assert.Equal(dummyCom.TransactionTestGuid, itemArr[0]);
                }
            }
        }




        [Fact]
        public void TestPersistentTransactionContext_RestoreConnectionAfterStartTransaction()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            //persistComm.DirectoryPath = @"." + Path.DirectorySeparatorChar + "IOCTalk-PendingMessageStore-RestoreConnectionAfterStartTransaction";
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;


            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            dummyCom.RaiseConnectionLost = false;

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call

            // create connection after transaction start
            dummyCom.RaiseConnectionCreated();

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 


            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn });  // transaction commit

            // wait until local pending transaction file messages are resend
            Thread.Sleep(500);

            // 5 resend calls
            Assert.Equal(5, dummyCom.InvokeCounter);

            // expect 4 parameter values item
            Assert.Equal(4, dummyCom.ReceivedParameterList.Count);
            for (int i = 0; i < dummyCom.ReceivedParameterList.Count; i++)
            {
                var itemArr = dummyCom.ReceivedParameterList[i];

                // expect new guid for complete transaction resend
                Assert.Equal(dummyCom.TransactionTestGuid, itemArr[0]);
            }
        }



        [Fact]
        public void TestPersistentTransactionContext_TransactionConnectionFlickering()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            //persistComm.DirectoryPath = @"." + Path.DirectorySeparatorChar + "IOCTalk-PendingMessageStore-RestoreConnectionAfterStartTransaction";
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;


            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            dummyCom.RaiseConnectionLost = false;

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            // raise connection before first transaction call
            dummyCom.RaiseConnectionCreated();

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            // loose connection
            persistComm.RealUnderlyingSession.Close();

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            // raise connection again
            // no further online calls expected (only resend)
            dummyCom.RaiseConnectionCreated();

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn });  // transaction commit

            // wait until local pending transaction file messages are resend
            Thread.Sleep(500);

            // 9 calls expected (2 online calls + 7 resend transaction calls)
            Assert.Equal(9, dummyCom.InvokeCounter);
        }



        [Fact]
        public void TestPersistentTransactionContext_CompleteOnlineTransactionCalls_NoResendExpected()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;


            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn });  // transaction commit

            // loose connection
            persistComm.RealUnderlyingSession.Close();

            //Thread.Sleep(100);

            // raise connection again
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed
            Thread.Sleep(500);

            // 5 calls = 5 online transaction calls till first conn lost. No resend expected because commit mehtod was successfully executed during active connection.
            Assert.Equal(5, dummyCom.InvokeCounter);

            // expect 4 parameter values (- start method)
            Assert.Equal(4, dummyCom.ReceivedParameterList.Count);
            Assert.Equal(dummyCom.TransactionTestGuid, dummyCom.ReceivedParameterList[0][0]);
        }




        [Fact]
        public void TestPersistentTransactionContext_FunctionalExceptionOnComplete()
        {
            IOCTalk.Composition.TalkCompositionHost talkCompositionHost = new Composition.TalkCompositionHost();

            PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
            dummyCom.RaiseConnectionLost = true;

            PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
            persistComm.ResendDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionDelay = TimeSpan.Zero;
            persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;


            TransactionDefinition trxDef = new TransactionDefinition("Test Transaction");

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.StartTransactionTest))
                        .RegisterTransactionBegin(trxDef)
                        .RegisterResendAction(new TrxResendActionUseReturnValue("testSessionId"));

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.PushTrxData))
                        .RegisterTransaction(trxDef);

            persistComm.RegisterPersistentMethod<ITrxTestService>(nameof(ITrxTestService.CompleteTransactionTest))
                        .RegisterTransactionCommit(trxDef);

            persistComm.RegisterContainerHost(talkCompositionHost, null);
            persistComm.Init();

            CleanupPeristentDirectory(persistComm);

            dummyCom.RaiseConnectionLost = false;
            dummyCom.RaiseConnectionCreated();

            InvokeMethodInfo mInfoBeginTrx = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.StartTransactionTest));
            InvokeMethodInfo mInfoTrxData = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.PushTrxData));
            InvokeMethodInfo mInfoTrxCommit = new InvokeMethodInfo(typeof(ITrxTestService), nameof(ITrxTestService.CompleteTransactionTest));

            ISession session = new BSAG.IOCTalk.Common.Session.Session(dummyCom, 1, "Unit Test Session");
            Guid startTrxReturn = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn });  // Push transaction data 

            dummyCom.RaiseFunctionalException = true;

            Assert.Throws<InvalidOperationException>(() => persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn }));  // transaction commit

            // close connection to release file
            persistComm.RealUnderlyingSession.Close();

            // raise connection again
            var firstOnlineCallGuid = dummyCom.TransactionTestGuid;
            dummyCom.TransactionTestGuid = Guid.NewGuid(); // set new Guid
            dummyCom.RaiseConnectionCreated();

            // wait until local pending messages are processed (no messages are expected)
            Thread.Sleep(500);

            // 4 calls because last online call threw a function exception resulting in a transaction abort
            Assert.Equal(4, dummyCom.InvokeCounter);

            // begin new transaction without interruption
            dummyCom.RaiseFunctionalException = false;
            Guid startTrxReturn2 = (Guid)persistComm.InvokeMethod(this, mInfoBeginTrx, session, new object[0]);  // Start transaction call
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn2 });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn2 });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxData, session, new object[] { startTrxReturn2 });  // Push transaction data 
            persistComm.InvokeMethod(this, mInfoTrxCommit, session, new object[] { startTrxReturn2 });

            Assert.Equal(9, dummyCom.InvokeCounter);

            // release resend session
            persistComm.RealUnderlyingSession.Close();
        }



        //[Fact]
        //public async Task ResendTempTest()
        //{
        //    PersistentTestCommService dummyCom = new PersistentTestCommService();
        //    dummyCom.RaiseConnectionLost = false;

        //    PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
        //    persistComm.ResendDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;

        //    BSAG.IOCTalk.Common.Session.Session dummySession = new Session.Session(null, 0, "UnitTest Dummy Session");

        //    await persistComm.ResendFile(@"C:\temp\MessageStore-20201012_143957_8823.pend", dummySession);
        //}


        //[Fact]
        //public async Task ResendTempTest2()
        //{
        //    PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
        //    dummyCom.RaiseConnectionLost = false;

        //    PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
        //    persistComm.ResendDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;

        //    BSAG.IOCTalk.Common.Session.Session dummySession = new Session.Session(null, 0, "UnitTest Dummy Session");

        //    await persistComm.ResendFile(@"C:\temp\ioctalkPendingMsgProblem2021-02-23-DID1027\MessageStore-Trx_Upload Trx-20210217_084628_3970.pend", dummySession);
        //}


        //[Fact]
        //public async Task ResendTempTest3()
        //{
        //    PersistentTestCommService dummyCom = new PersistentTestCommService(xUnitLog);
        //    dummyCom.RaiseConnectionLost = false;

        //    PersistentClientCommunicationHost persistComm = new PersistentClientCommunicationHost(dummyCom);
        //    persistComm.ResendDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionDelay = TimeSpan.Zero;
        //    persistComm.ResendSuspensionGracePeriod = TimeSpan.Zero;

        //    BSAG.IOCTalk.Common.Session.Session dummySession = new Session.Session(null, 0, "UnitTest Dummy Session");

        //    TalkCompositionHost dummyHost = new TalkCompositionHost("UnitTestDummyHost");
        //    dummyCom.RegisterContainerHost(dummyHost, null);

        //    await persistComm.ResendFile(@"C:\temp\LieferscheinPushProblem_2023-07-11\MessageStore-Trx_Upload Doc Trx-20230711_152955_9462.pend", dummySession);
        //}


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

            private ITestOutputHelper xUnitLogger;

            IGenericMessageSerializer serializer;

            public PersistentTestCommService(ITestOutputHelper xUnitLogger)
            {
                this.xUnitLogger = xUnitLogger;
                serializer = new BSAG.IOCTalk.Serialization.Json.JsonMessageSerializer();
            }

            public string SerializerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IGenericMessageSerializer Serializer => serializer;

            public string LoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ILogger Logger => this;

            public bool LogDataStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string DataStreamLoggerTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public IDataStreamLogger DataStreamLogger { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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


            public bool RaiseFunctionalException { get; set; }

            public int InvokeCounter { get; set; }
            public TimeSpan RequestTimeout { get; set; }

            public Guid TransactionTestGuid { get; set; } = Guid.NewGuid();

            public List<object[]> ReceivedParameterList { get; set; } = new List<object[]>();

            public object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
            {
                if (RaiseConnectionLost)
                {
                    throw new OperationCanceledException("Session lost unit test dummy");
                }
                else if (RaiseFunctionalException)
                {
                    throw new InvalidOperationException("Dummy functional exception");
                }
                else
                {
                    InvokeCounter++;

                    if (parameters.Length > 0)
                    {
                        ReceivedParameterList.Add(parameters);
                    }

                    if (invokeInfo.InterfaceMethod.ReturnType.Equals(typeof(Guid)))
                    {
                        return TransactionTestGuid;
                    }
                    else
                    {
                        return null;
                    }
                }
            }


            public Task<object> InvokeMethodAsync(object source, IInvokeMethodInfo invokeInfo, ISession session, object[] parameters)
            {
                throw new NotImplementedException();
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
                xUnitLogger.WriteLine("DEBUG: " + message);
            }

            void ILogger.Info(string message)
            {
                xUnitLogger.WriteLine("INFO: " + message);
            }

            void ILogger.Warn(string message)
            {
                xUnitLogger.WriteLine("WARN: " + message);
            }

            void ILogger.Error(string message)
            {
                xUnitLogger.WriteLine("ERROR: " + message);
                throw new Exception(message);
            }

        }
    }
}
