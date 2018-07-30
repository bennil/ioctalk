using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BSAG.IOCTalk.Test.TestObjects;
using BSAG.IOCTalk.Common.Reflection;
using System.IO;
using Xunit;
using BSAG.IOCTalk.Test.Common;

namespace BSAG.IOCTalk.Common.Test
{
    public class TypeServiceTest
    {
        [Fact]
        public void TestMethodCreateInheritInterfaceImplementation()
        {
            object createdType = TypeService.BuildInterfaceImplementationType(typeof(ITestInterfaceExtended).FullName);
            Assert.True(createdType != null && createdType is Type);

            Type type = (Type)createdType;

            Assert.True(type.GetInterface(typeof(ITestInterfaceExtended).FullName) != null);
        }

        [Fact]
        public void TestMethodCreateIEnumerableImplementation()
        {
            TestCollectionInstanceCreation(typeof(IEnumerable<string>));
            TestCollectionInstanceCreation(typeof(IEnumerable<ITestInterfaceBase>));
            TestCollectionInstanceCreation(typeof(IEnumerableTest));
        }

        private static void TestCollectionInstanceCreation(Type interfaceType)
        {
            object createdType = TypeService.BuildInterfaceImplementationType(interfaceType.FullName);
            Assert.True(createdType != null && createdType is Type);

            Type type = (Type)createdType;

            Assert.True(interfaceType.IsAssignableFrom(type));
        }


        [Fact]
        public void TestMethodGetGenericType()
        {
            Type type;
            TypeService.TryGetTypeByName(typeof(IEnumerable<ITestInterfaceBase>).FullName, out type);
            Assert.True(type != null);
        }


        [Fact]
        public void TestMethodGetGenericTypeReadableName()
        {

            string readableGenericName = "System.Collections.Generic.IEnumerable<BSAG.IOCTalk.Test.TestObjects.ITestInterfaceBase>";

            Type type;
            TypeService.TryGetTypeByName(readableGenericName, out type);
            Assert.True(type != null);

            string readableGenericName2 = "System.Collections.Generic.IDictionary<System.String, BSAG.IOCTalk.Test.TestObjects.ITestInterfaceBase>";

            TypeService.TryGetTypeByName(readableGenericName2, out type);
            Assert.True(type != null);
        }



        [Fact]
        public void TestMethodGetExplicitInterfaceMethodImplementation()
        {
            InvokeMethodInfo invokeInfo = new InvokeMethodInfo(typeof(ITestServiceInterface), "StartService", new Type[] { typeof(int) }, typeof(TestServiceImplementation));

            Assert.True(invokeInfo.ImplementationMethod != null);

            // with full qualified method name
            InvokeMethodInfo invokeInfo2 = new InvokeMethodInfo(typeof(ITestServiceInterface), invokeInfo.QualifiedMethodName, null, typeof(TestServiceImplementation));

            Assert.True(invokeInfo2.ImplementationMethod != null);

        }



        [Fact]
        public void TestMethodCreateProxyImplementation()
        {
            SimpleProxyImplementationTest();

            //AdvancedProxyImplementation();
        }

        //private static void AdvancedProxyImplementation()
        //{
        //    Type result = TypeService.BuildProxyImplementation(typeof(IPerformanceMonitorService), true);

        //    IPerformanceMonitorService instance = (IPerformanceMonitorService)Activator.CreateInstance(result);

        //    var piCommSerivce = instance.GetType().GetProperty("CommunicationService");

        //    DummyCommunicationService dummyCommService = new DummyCommunicationService();

        //    piCommSerivce.SetValue(instance, dummyCommService, null);

        //    TimeSpan timeSpan = new TimeSpan(23532534);
        //    List<string> testColl = new List<string>();
        //    testColl.Add("item 1");
        //    string testOutput;
        //    instance.SubscribeCpuUsageNotification(timeSpan, out testOutput, testColl);
        //}


        private static void SimpleProxyImplementationTest()
        {
            Type result = TypeService.BuildProxyImplementation(typeof(IPerformanceMonitorClientNotification));

            IPerformanceMonitorClientNotification instance = (IPerformanceMonitorClientNotification)Activator.CreateInstance(result, new object[2]);

            //var piCommSerivce = instance.GetType().GetProperty("CommunicationService");

            //DummyCommunicationService dummyCommService = new DummyCommunicationService();

            //piCommSerivce.SetValue(instance, dummyCommService, null);

            //PerformanceData data = new PerformanceData();
            //data.Type = MeasureType.Cpu;
            //data.Unity = "test";
            //data.Value = 2;
            //instance.OnPerformanceData(data);
        }


        //public class DummyCommunicationService : GenericCommunicationBaseService
        //{
        //    public override object InvokeMethod(object source, System.Reflection.MethodInfo method, object[] parameters)
        //    {
        //        Assert.IsNotNull(method);

        //        return Activator.CreateInstance(method.ReturnType);
        //    }

        //    public override object InvokeMethod(object source, IOCTalk.Common.Interface.Reflection.IInvokeMethodInfo invokeInfo, object[] parameters)
        //    {
        //        Assert.IsNotNull(invokeInfo);

        //        if (invokeInfo.InterfaceMethod.ReturnType != typeof(void))
        //        {
        //            if (invokeInfo.InterfaceMethod.ReturnType == typeof(IPerfSubscribeResponse))
        //            {
        //                return new PerfSubscribeResponse();
        //            }
        //            else
        //            {
        //                return Activator.CreateInstance(invokeInfo.InterfaceMethod.ReturnType);
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //}
    }
}
