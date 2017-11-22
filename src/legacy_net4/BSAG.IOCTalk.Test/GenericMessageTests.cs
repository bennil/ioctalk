using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BSAG.IOCTalk.Communication.Common;
using System.Diagnostics;
using BSAG.IOCTalk.Serialization.Json;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Test.TestObjects;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Container.MEF;

namespace BSAG.IOCTalk.Test
{
    [TestClass]
    public class GenericMessageTests
    {
        IGenericMessageSerializer serializer = new JsonMessageSerializer();

        public GenericMessageTests()
        {
            serializer.RegisterContainerHost(new DummyTestContainerHost());
        }

        [TestMethod]
        public void BasicStringSerializationTest()
        {
            var method = this.GetType().GetMethod("BasicByteSerializationTestParams");

            object[] parameters = new object[] { 3, "Test", 937.1 };

            GenericMessage reqMsg = new GenericMessage(1, method, parameters);

            string reqMsgString = serializer.SerializeToString(reqMsg, null);

            GenericMessage deserializedMsg = (GenericMessage)serializer.DeserializeFromString(reqMsgString, null);

            Assert.IsTrue(deserializedMsg.Payload is object[]);

            object[] deserializedParameters = deserializedMsg.Payload as object[];

            Assert.AreEqual(parameters[0], deserializedParameters[0]);
            Assert.AreEqual(parameters[1], deserializedParameters[1]);
            Assert.AreEqual(parameters[2], deserializedParameters[2]);
        }
        

        [TestMethod]
        public void StringSerializationPerformanceTest()
        {
            var method = this.GetType().GetMethod("BasicByteSerializationTestParams");


            Stopwatch stAll = Stopwatch.StartNew();
            Stopwatch stFirst = Stopwatch.StartNew();

            int serializeCount = 100000;
            for (int i = 0; i < serializeCount; i++)
            {
                object[] parameters = new object[] { i, "Test", 937.1 };

                GenericMessage reqMsg = new GenericMessage(i, method, parameters);

                string reqMsgString = serializer.SerializeToString(reqMsg, null);

                if (i == 0)
                {
                    stFirst.Stop();
                }
            }

            stAll.Stop();

            Debug.WriteLine("String Serialize Count: {0} - First serialize time: {1} - All serializations: {2}", serializeCount, stFirst.Elapsed, stAll.Elapsed);
        }


        public void StringAdvancedSerializationTestParams(BaseTestObject testObj1, BaseTestObject testObj2)
        {
        }

        [TestMethod]
        public void StringAdvancedByteSerializationTest()
        {
            var method = this.GetType().GetMethod("StringAdvancedSerializationTestParams");

            InvokeMethodInfo methodInfo = new InvokeMethodInfo(method);
            

            BaseTestObject testObject1 = new BaseTestObject();
            testObject1.BaseProperty = "Test1";

            BaseTestObject testObject2 = new BaseTestObject();
            testObject2.BaseProperty = "Test2";

            object[] parameters = new object[] { testObject1, testObject2 };

            GenericMessage reqMsg = new GenericMessage(1, method, parameters);

            string reqMsgString = serializer.SerializeToString(reqMsg, methodInfo);

            GenericMessage deserializedMsg = (GenericMessage)serializer.DeserializeFromString(reqMsgString, methodInfo);

            Assert.IsTrue(deserializedMsg.Payload is object[]);
            object[] deserializedParams = (object[])deserializedMsg.Payload;
            Assert.AreEqual<int>(2, deserializedParams.Length);

            Assert.IsTrue((deserializedParams[0] as BaseTestObject).BaseProperty == testObject1.BaseProperty);
            Assert.IsTrue((deserializedParams[1] as BaseTestObject).BaseProperty == testObject2.BaseProperty);
        }

        public void BasicByteSerializationTestParams(int id, string str, double dbl)
        {
        }

        [TestMethod]
        public void BasicByteSerializationTest()
        {
            var method = this.GetType().GetMethod("BasicByteSerializationTestParams");

            object[] parameters = new object[] { 3, "Test", 937.1 };

            GenericMessage reqMsg = new GenericMessage(1, method, parameters);

            byte[] reqMsgBytes = serializer.SerializeToBytes(reqMsg, null);

            GenericMessage deserializedMsg = (GenericMessage)serializer.DeserializeFromBytes(reqMsgBytes, null);

            Assert.IsTrue(deserializedMsg.Payload is object[]);

        }


       


        /// <summary>
        /// Bytes the serialization performance test.
        /// </summary>
        [TestMethod]
        public void ByteSerializationPerformanceTest()
        {
            var method = this.GetType().GetMethod("ByteSerializationPerformanceTest");


            Stopwatch stAll = Stopwatch.StartNew();
            Stopwatch stFirst = Stopwatch.StartNew();

            int serializeCount = 100000;
            for (int i = 0; i < serializeCount; i++)
            {
                object[] parameters = new object[] { i, "Test", 937.1 };

                GenericMessage reqMsg = new GenericMessage(i, method, parameters);

                byte[] bytes = serializer.SerializeToBytes(reqMsg, null);

                if (i == 0)
                {
                    stFirst.Stop();
                }
            }

            stAll.Stop();

            Debug.WriteLine("Bytes Serialize Count: {0} - First serialize time: {1} - All serializations: {2}", serializeCount, stFirst.Elapsed, stAll.Elapsed);
        }


        /// <summary>
        /// Tests the convention driven expose sub interface.
        /// </summary>
        [TestMethod]
        public void TestConventionDrivenExposeSubType()
        {
            Type interfaceType = typeof(ITestServiceInterface);
            Type sourceType = typeof(TestServiceImplementation);
            TalkContainerHostMEF<TestServiceContractSession> containerHost = new TalkContainerHostMEF<TestServiceContractSession>();
            containerHost.RegisterExposedSubInterfaceForType(interfaceType, sourceType);

            Type result = containerHost.GetExposedSubInterfaceForType(sourceType);

            Assert.AreEqual<Type>(interfaceType, result);
        }


        internal class DummyTestContainerHost : IGenericContainerHost
        {
            public DummyTestContainerHost()
            {
            }

            public object DIContainer
            {
                get { throw new NotImplementedException(); }
            }

            public void InitGenericCommunication(IGenericCommunicationService communicationService)
            {
                throw new NotImplementedException();
            }

            public object CreateSessionContractInstance(IOCTalk.Common.Interface.Session.ISession session)
            {
                throw new NotImplementedException();
            }

            public object GetInterfaceImplementationInstance(IOCTalk.Common.Interface.Session.ISession session, string interfaceType)
            {
                throw new NotImplementedException();
            }

            public Type GetInterfaceImplementationType(string interfaceType)
            {
                throw new NotImplementedException();
            }

            public IOCTalk.Common.Interface.Session.ISession GetSessionByServiceInstance(object serviceObjectInstance)
            {
                throw new NotImplementedException();
            }

            public Type GetExposedSubInterfaceForType(Type sourceType)
            {
                return null;
            }

            public void RegisterExposedSubInterfaceForType(Type interfaceType, Type sourceType)
            {                
            }
        }
    }
}
