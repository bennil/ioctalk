using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BSAG.IOCTalk.Test.TestObjects;
using BSAG.IOCTalk.Serialization.Json;
using BSAG.IOCTalk.Test.Common;
using System.ComponentModel.Composition;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Test.Common.Service.MEF;
using BSAG.IOCTalk.Common.Reflection;
using System.IO;

namespace BSAG.IOCTalk.Test
{
    [TestClass]
    public class TypeServiceTest
    {
        [TestMethod]
        public void TestMethodCreateInheritInterfaceImplementation()
        {
            object createdType = TypeService.BuildInterfaceImplementationType(typeof(ITestInterfaceExtended).FullName, false);
            Assert.IsTrue(createdType != null && createdType is Type);

            Type type = (Type)createdType;

            Assert.IsTrue(type.GetInterface(typeof(ITestInterfaceExtended).FullName) != null);
        }

        [TestMethod]
        public void TestMethodCreateIEnumerableImplementation()
        {
            TestCollectionInstanceCreation(typeof(IEnumerable<string>));
            TestCollectionInstanceCreation(typeof(IEnumerable<ITestInterfaceBase>));
            TestCollectionInstanceCreation(typeof(IEnumerableTest));
        }

        private static void TestCollectionInstanceCreation(Type interfaceType)
        {
            object createdType = TypeService.BuildInterfaceImplementationType(interfaceType.FullName, true);
            Assert.IsTrue(createdType != null && createdType is Type);

            Type type = (Type)createdType;

            Assert.IsTrue(interfaceType.IsAssignableFrom(type));
        }


        [TestMethod]
        public void TestMethodGetGenericType()
        {
            Type type;
            TypeService.TryGetTypeByName(typeof(IEnumerable<ITestInterfaceBase>).FullName, out type);
            Assert.IsNotNull(type);
        }


        [TestMethod]
        public void TestMethodGetGenericTypeReadableName()
        {

            string readableGenericName = "System.Collections.Generic.IEnumerable<BSAG.IOCTalk.Test.TestObjects.ITestInterfaceBase>";

            Type type;
            TypeService.TryGetTypeByName(readableGenericName, out type);
            Assert.IsNotNull(type);

            string readableGenericName2 = "System.Collections.Generic.IDictionary<System.String, BSAG.IOCTalk.Test.TestObjects.ITestInterfaceBase>";

            TypeService.TryGetTypeByName(readableGenericName2, out type);
            Assert.IsNotNull(type);
        }



        [TestMethod]
        public void TestMethodGetExplicitInterfaceMethodImplementation()
        {
            InvokeMethodInfo invokeInfo = new InvokeMethodInfo(typeof(ITestServiceInterface), "StartService", new Type[] { typeof(int) }, typeof(TestServiceImplementation));

            Assert.IsNotNull(invokeInfo.ImplementationMethod);

            // with full qualified method name
            InvokeMethodInfo invokeInfo2 = new InvokeMethodInfo(typeof(ITestServiceInterface), invokeInfo.QualifiedMethodName, null, typeof(TestServiceImplementation));

            Assert.IsNotNull(invokeInfo2.ImplementationMethod);

        }

        

        [TestMethod]
        public void TestMethodCreateProxyImplementation()
        {
            SimpleProxyImplementationTest();

            AdvancedProxyImplementation();
        }

        private static void AdvancedProxyImplementation()
        {
            Type result = TypeService.BuildProxyImplementation(typeof(IPerformanceMonitorService), true);

            IPerformanceMonitorService instance = (IPerformanceMonitorService)Activator.CreateInstance(result);

            var piCommSerivce = instance.GetType().GetProperty("CommunicationService");

            DummyCommunicationService dummyCommService = new DummyCommunicationService();

            piCommSerivce.SetValue(instance, dummyCommService, null);

            TimeSpan timeSpan = new TimeSpan(23532534);
            List<string> testColl = new List<string>();
            testColl.Add("item 1");
            string testOutput;
            instance.SubscribeCpuUsageNotification(timeSpan, out testOutput, testColl);
        }


        private static void SimpleProxyImplementationTest()
        {
            Type result = TypeService.BuildProxyImplementation(typeof(IPerformanceMonitorClientNotification), false);

            IPerformanceMonitorClientNotification instance = (IPerformanceMonitorClientNotification)Activator.CreateInstance(result);

            var piCommSerivce = instance.GetType().GetProperty("CommunicationService");

            DummyCommunicationService dummyCommService = new DummyCommunicationService();

            piCommSerivce.SetValue(instance, dummyCommService, null);

            PerformanceData data = new PerformanceData();
            data.Type = MeasureType.Cpu;
            data.Unity = "test";
            data.Value = 2;
            instance.OnPerformanceData(data);
        }


        public class DummyCommunicationService : GenericCommunicationBaseService
        {
            public override object InvokeMethod(object source, System.Reflection.MethodInfo method, object[] parameters)
            {
                Assert.IsNotNull(method);

                return Activator.CreateInstance(method.ReturnType);
            }

            public override object InvokeMethod(object source, IOCTalk.Common.Interface.Reflection.IInvokeMethodInfo invokeInfo, object[] parameters)
            {
                Assert.IsNotNull(invokeInfo);

                if (invokeInfo.InterfaceMethod.ReturnType != typeof(void))
                {
                    if (invokeInfo.InterfaceMethod.ReturnType == typeof(IPerfSubscribeResponse))
                    {
                        return new PerfSubscribeResponse();
                    }
                    else
                    {
                        return Activator.CreateInstance(invokeInfo.InterfaceMethod.ReturnType);
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
