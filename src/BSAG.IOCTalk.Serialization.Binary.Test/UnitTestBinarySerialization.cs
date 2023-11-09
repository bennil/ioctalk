using System;
using BSAG.IOCTalk.Test.TestObjects;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Test.TestObjects.NoProperties;
using System.Collections.Generic;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Reflection;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;
using BSAG.IOCTalk.Test.TestObjects.ChangedLayout;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Serialization.Binary.Test.Model;
using Xunit;
using System.Linq;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Test.Interface;

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    public class UnitTestBinarySerialization
    {
        [Fact]
        public void TestMethodSimpleSerialize()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            BaseTestObject simpleObj = new BaseTestObject();
            simpleObj.BaseProperty = "Hallo";
            simpleObj.TestId = 53435422;

            byte[] result = serializer.Serialize(simpleObj, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            BaseTestObject resultObject = (BaseTestObject)serializer.Deserialize(result, deserializationContext);

            Assert.Equal(simpleObj.BaseProperty, resultObject.BaseProperty);
        }

        [Fact]
        public void TestMethodSimpleSerializeWithNewSerializer()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            BaseTestObject simpleObj = new BaseTestObject();
            simpleObj.BaseProperty = "Hallo";
            simpleObj.TestId = 53435422;

            byte[] result = serializer.Serialize(simpleObj, null);

            ClearGlobalTypeCache();

            BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());

            SerializationContext deserializationContext = new SerializationContext(serializer2, true, null);
            BaseTestObject resultObject = (BaseTestObject)serializer2.Deserialize(result, deserializationContext);

            Assert.Equal(simpleObj.BaseProperty, resultObject.BaseProperty);
        }

        private static void ClearGlobalTypeCache()
        {
            // clear global cache
            Dictionary<Type, IValueItem> globalCache1 = (Dictionary<Type, IValueItem>)typeof(BinarySerializer).GetField("globalStructureMapping", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Dictionary<uint, IValueItem> globalCache2 = (Dictionary<uint, IValueItem>)typeof(BinarySerializer).GetField("globalStructureMappingById", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            List<IObjectType> removeItems = new List<IObjectType>();
            foreach (var item in globalCache1.Values)
            {
                if (item is IObjectType)
                {
                    IObjectType structure = (IObjectType)item;
                    removeItems.Add(structure);
                }
            }

            foreach (var rItem in removeItems)
            {
                globalCache1.Remove(rItem.RuntimeType);
                globalCache2.Remove(rItem.TypeId);
            }
        }

        [Fact]
        public void TestMethodBasicSerialization()
        {
            try
            {
                BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver()); //UnknownTypeResolver, SpecialTypeResolver);


                TestObject testObj = new TestObject();
                testObj.ID = 4;
                testObj.Description = "Test objekt x";
                testObj.SubObject = new SubObject() { SubId = 2, SubDescr = "Sub Description" };
                testObj.TestIntList = new List<int>(new int[] { 2, 6, 3, 8, 2 });
                testObj.ObjectArray = new object[] { 4, "Test String with \"escape\" chars ", 263.12, new TimeSpan(2, 2, 2), null, new SubObject() { SubId = 3, SubDescr = "Array Object" } };
                testObj.BooleanValue = true;
                testObj.NullableBooleanValue = null;
                testObj.NullableBooleanValue2 = true;
                testObj.TimeSpanValue = new TimeSpan(1, 1, 1);
                testObj.DateTimeValue = new DateTime(2014, 07, 14, 18, 1, 5, 201);
                testObj.BaseProperty = "Base Test";
                testObj.CharValue = 'b';
                testObj.GuidValue = Guid.NewGuid();
                testObj.DecimalValue = 1242.46m;
                testObj.DecimalValueNullable = null;
                testObj.DecimalValueNullable2 = 346353.23m;
                testObj.IntValueNullable = null;
                testObj.IntValueNullable2 = -45435323;

                var dataBytes = serializer.Serialize(testObj, null);

                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                TestObject deserializedTestObj = (TestObject)serializer.Deserialize(dataBytes, deserializationContext);

                Assert.Equal<int>(testObj.ID, deserializedTestObj.ID);
                Assert.Equal(testObj.Description, deserializedTestObj.Description);

                Assert.Equal<int>(testObj.SubObject.SubId, deserializedTestObj.SubObject.SubId);
                Assert.Equal(testObj.SubObject.SubDescr, deserializedTestObj.SubObject.SubDescr);

                // check list
                for (int i = 0; i < testObj.TestIntList.Count; i++)
                {
                    Assert.Equal<int>(testObj.TestIntList[i], deserializedTestObj.TestIntList[i]);
                }

                // check object array
                for (int i = 0; i < testObj.ObjectArray.Length; i++)
                {
                    if (testObj.ObjectArray[i] is SubObject)
                    {
                        Assert.Equal(((SubObject)testObj.ObjectArray[i]).SubId, ((SubObject)deserializedTestObj.ObjectArray[i]).SubId);
                        Assert.Equal(((SubObject)testObj.ObjectArray[i]).SubDescr, ((SubObject)deserializedTestObj.ObjectArray[i]).SubDescr);
                    }
                    else
                    {
                        Assert.Equal(testObj.ObjectArray[i], deserializedTestObj.ObjectArray[i]);
                    }
                }

                Assert.Equal<bool>(testObj.BooleanValue, deserializedTestObj.BooleanValue);
                Assert.Equal<TimeSpan>(testObj.TimeSpanValue, deserializedTestObj.TimeSpanValue);
                Assert.Equal<DateTime>(testObj.DateTimeValue, deserializedTestObj.DateTimeValue);

                Assert.Equal(testObj.BaseProperty, deserializedTestObj.BaseProperty);

                Assert.Equal<char>(testObj.CharValue, deserializedTestObj.CharValue);
                Assert.Equal<char>(testObj.EmptyCharValue, deserializedTestObj.EmptyCharValue);
                Assert.Equal<Guid>(testObj.GuidValue, deserializedTestObj.GuidValue);

                Assert.Equal<bool?>(testObj.NullableBooleanValue, deserializedTestObj.NullableBooleanValue);
                Assert.Equal<bool?>(testObj.NullableBooleanValue2, deserializedTestObj.NullableBooleanValue2);

                Assert.Equal<decimal>(testObj.DecimalValue, deserializedTestObj.DecimalValue);
                Assert.Equal<decimal?>(testObj.DecimalValueNullable, deserializedTestObj.DecimalValueNullable);
                Assert.Equal<decimal?>(testObj.DecimalValueNullable2, deserializedTestObj.DecimalValueNullable2);

                Assert.Equal<int?>(testObj.IntValueNullable, deserializedTestObj.IntValueNullable);
                Assert.Equal<int?>(testObj.IntValueNullable2, deserializedTestObj.IntValueNullable2);
            }
            catch (Exception ex)
            {
                throw;  // because of visual studio unittest bug
            }
        }


        [Fact]
        public void TestMethodInterfaceSerialization()
        {
            try
            {
                BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

                TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
                testObj1.TestBaseProperty = "TEST";
                testObj1.AdditionalProperty = "Some data";
                testObj1.DeepTestProperty1 = "Inherited interface property 1";
                testObj1.DeepTestProperty2 = "Inherited interface property 2";

                InterfRefObject interfRef = new InterfRefObject();
                interfRef.BaseObject = testObj1;

                var dataBytes = serializer.Serialize(interfRef, null);

                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                InterfRefObject deserializedObj = (InterfRefObject)serializer.Deserialize(dataBytes, deserializationContext);

                Assert.Equal(interfRef.BaseObject.TestBaseProperty, deserializedObj.BaseObject.TestBaseProperty);

                TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedObj.BaseObject;
                Assert.Null(implObj.AdditionalProperty);

                Assert.Equal(testObj1.DeepTestProperty1, implObj.DeepTestProperty1);
                Assert.Equal(testObj1.DeepTestProperty2, implObj.DeepTestProperty2);
            }
            catch (Exception ex)
            {
                throw;  // because of visual studio unittest bug
            }
        }


        /// <summary>
        /// Tests the method interface inheritance serialization.
        /// Expects typed meta serialization attribute because of interface inheritance serialization.
        /// </summary>
        [Fact]
        public void TestMethodInterfaceInheritanceSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
            testObj1.TestBaseProperty = "TEST";
            testObj1.AdditionalProperty = "Some data";
            testObj1.DeepTestProperty1 = "Inherited interface property 1";
            testObj1.DeepTestProperty2 = "Inherited interface property 2";

            InterfRefObject interfRef = new InterfRefObject();
            interfRef.BaseObjectInstance = testObj1;

            var bytes = serializer.Serialize(interfRef, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            InterfRefObject deserializedObj = (InterfRefObject)serializer.Deserialize(bytes, deserializationContext);

            Assert.Equal(((ITestInterfaceBase)interfRef.BaseObjectInstance).TestBaseProperty, ((ITestInterfaceBase)deserializedObj.BaseObjectInstance).TestBaseProperty);

            TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedObj.BaseObjectInstance;
            Assert.Null(implObj.AdditionalProperty);

            Assert.Equal(testObj1.DeepTestProperty1, implObj.DeepTestProperty1);
            Assert.Equal(testObj1.DeepTestProperty2, implObj.DeepTestProperty2);
            Assert.Equal(testObj1.TestBaseProperty, implObj.TestBaseProperty);
            Assert.Null(implObj.AdditionalProperty);
        }


        /// <summary>
        /// Tests the method interface inheritance array serialization.
        /// </summary>
        [Fact]
        public void TestMethodInterfaceInheritanceArraySerialization()
        {
            try
            {
                BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

                TestInterfaceImpl1Collections collHolder = new TestInterfaceImpl1Collections();

                TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
                testObj1.TestBaseProperty = "TEST";
                testObj1.AdditionalProperty = "Some data";
                testObj1.DeepTestProperty1 = "Inherited interface property 1";
                testObj1.DeepTestProperty2 = "Inherited interface property 2";

                TestInterfaceImpl1 testObj2 = new TestInterfaceImpl1();
                testObj2.TestBaseProperty = "TEST 2";
                testObj2.AdditionalProperty = "Some data 2";
                testObj2.DeepTestProperty1 = "Inherited interface property 1 - 2";
                testObj2.DeepTestProperty2 = "Inherited interface property 2 - 2";

                collHolder.Array = new ITestInterfaceBase[] { testObj1 };

                collHolder.List = new List<ITestInterfaceBase>();
                collHolder.List.Add(testObj1);
                collHolder.List.Add(testObj2);

                collHolder.ObjectArray = new object[] { testObj1, testObj2 };

                // own collection
                collHolder.OwnCollection = new OwnCollection();
                collHolder.OwnCollection.Add("Own Testdata 1");
                collHolder.OwnCollection.Add("Own Testdata 2");

                var data = serializer.Serialize(collHolder, null);

                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                TestInterfaceImpl1Collections deserializedObj = (TestInterfaceImpl1Collections)serializer.Deserialize(data, deserializationContext);

                Assert.Equal<int>(collHolder.Array.Length, deserializedObj.Array.Length);
                for (int i = 0; i < collHolder.Array.Length; i++)
                {
                    ITestInterfaceBase orginalItem = collHolder.Array[i];
                    ITestInterfaceBase deserializedItem = deserializedObj.Array[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }

                for (int i = 0; i < collHolder.List.Count; i++)
                {
                    ITestInterfaceBase orginalItem = collHolder.List[i];
                    ITestInterfaceBase deserializedItem = deserializedObj.List[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }

                for (int i = 0; i < collHolder.ObjectArray.Length; i++)
                {
                    ITestInterfaceBase orginalItem = (ITestInterfaceBase)collHolder.ObjectArray[i];
                    ITestInterfaceBase deserializedItem = (ITestInterfaceBase)deserializedObj.ObjectArray[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }

                // check own collection implementation
                var deserializedEnumerator = deserializedObj.OwnCollection.GetEnumerator();
                foreach (var item in collHolder.OwnCollection)
                {
                    deserializedEnumerator.MoveNext();

                    Assert.True(item.Equals(deserializedEnumerator.Current));
                }
            }
            catch (Exception ex)
            {
                throw;  // because of visual studio unittest bug
            }
        }

        /// <summary>
        /// Tests the method interface inheritance array serialization.
        /// </summary>
        [Fact]
        public void TestMethodInterfaceInheritanceArraySerializationWithNewSerializer()
        {
            try
            {
                BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

                TestInterfaceImpl1Collections collHolder = new TestInterfaceImpl1Collections();

                TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
                testObj1.TestBaseProperty = "TEST";
                testObj1.AdditionalProperty = "Some data";
                testObj1.DeepTestProperty1 = "Inherited interface property 1";
                testObj1.DeepTestProperty2 = "Inherited interface property 2";

                TestInterfaceImpl1 testObj2 = new TestInterfaceImpl1();
                testObj2.TestBaseProperty = "TEST 2";
                testObj2.AdditionalProperty = "Some data 2";
                testObj2.DeepTestProperty1 = "Inherited interface property 1 - 2";
                testObj2.DeepTestProperty2 = "Inherited interface property 2 - 2";

                collHolder.Array = new ITestInterfaceBase[] { testObj1 };

                collHolder.List = new List<ITestInterfaceBase>();
                collHolder.List.Add(testObj1);
                collHolder.List.Add(testObj2);

                collHolder.ObjectArray = new object[] { testObj1, testObj2 };

                decimal anyObjVal = 342534.56m;
                object[] anyObjectArr = new object[] { 2, "hallo", new object[] { 7, "nested", new TestObject() {
                    DecimalValue = anyObjVal
                } } };
                collHolder.AnyObjectArray = anyObjectArr;

                // own collection
                collHolder.OwnCollection = new OwnCollection();
                collHolder.OwnCollection.Add("Own Testdata 1");
                collHolder.OwnCollection.Add("Own Testdata 2");

                var data = serializer.Serialize(collHolder, null);

                ClearGlobalTypeCache();

                BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());

                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                TestInterfaceImpl1Collections deserializedObj = (TestInterfaceImpl1Collections)serializer2.Deserialize(data, deserializationContext);

                Assert.Equal<int>(collHolder.Array.Length, deserializedObj.Array.Length);
                for (int i = 0; i < collHolder.Array.Length; i++)
                {
                    ITestInterfaceBase orginalItem = collHolder.Array[i];
                    ITestInterfaceBase deserializedItem = deserializedObj.Array[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }

                for (int i = 0; i < collHolder.List.Count; i++)
                {
                    ITestInterfaceBase orginalItem = collHolder.List[i];
                    ITestInterfaceBase deserializedItem = deserializedObj.List[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }

                for (int i = 0; i < collHolder.ObjectArray.Length; i++)
                {
                    ITestInterfaceBase orginalItem = (ITestInterfaceBase)collHolder.ObjectArray[i];
                    ITestInterfaceBase deserializedItem = (ITestInterfaceBase)deserializedObj.ObjectArray[i];

                    CheckCollectionItem(orginalItem, deserializedItem, true);
                }


                // check own collection implementation
                var deserializedEnumerator = deserializedObj.OwnCollection.GetEnumerator();
                foreach (var item in collHolder.OwnCollection)
                {
                    deserializedEnumerator.MoveNext();

                    Assert.True(item.Equals(deserializedEnumerator.Current));
                }

                object anyItemObj = ((object[])((object[])collHolder.AnyObjectArray)[2])[2];
                TestObject anyItem = anyItemObj as TestObject;

                Assert.Equal(anyObjVal, anyItem.DecimalValue);

            }
            catch (Exception ex)
            {
                throw;  // because of visual studio unittest bug
            }
        }

        private static void CheckCollectionItem(ITestInterfaceBase orginalItem, ITestInterfaceBase deserializedItem, bool checkAddProp)
        {
            Assert.Equal(orginalItem.TestBaseProperty, deserializedItem.TestBaseProperty);
            Assert.Equal(orginalItem.DeepTestProperty1, deserializedItem.DeepTestProperty1);
            Assert.Equal(orginalItem.DeepTestProperty2, deserializedItem.DeepTestProperty2);

            TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedItem;
            if (checkAddProp)
                Assert.Null(implObj.AdditionalProperty);
        }


        [Fact]
        public void TestMethodObjectArrayInObjectArraySerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            TestObject testObj = new TestObject();
            testObj.ObjectArray = new object[] { 1, 10, 100005, new object[] { "Nested", "Array", 1 } };
            testObj.AnyObject = testObj.ObjectArray;

            var data = serializer.Serialize(testObj, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            TestObject deserializedObj = (TestObject)serializer.Deserialize(data, deserializationContext);

            {
                Assert.Equal<int>(testObj.ObjectArray.Length, deserializedObj.ObjectArray.Length);
                object[] sourceArr = (object[])testObj.ObjectArray[3];
                object[] targetArr = (object[])deserializedObj.ObjectArray[3];

                for (int i = 0; i < sourceArr.Length; i++)
                {
                    Assert.Equal(sourceArr[i], targetArr[i]);
                }
            }

            // any object check
            {
                Assert.Equal<int>(((object[])testObj.AnyObject).Length, ((object[])deserializedObj.AnyObject).Length);
                object[] sourceArr = (object[])((object[])testObj.AnyObject)[3];
                object[] targetArr = (object[])((object[])deserializedObj.AnyObject)[3];

                for (int i = 0; i < sourceArr.Length; i++)
                {
                    Assert.Equal(sourceArr[i], targetArr[i]);
                }
            }
        }


        [Fact]
        public void TestMethodRootObjectArraySerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            object[] array = new object[] { 1, 10, 100005, new object[] { "Nested", "Array", 1 } };
            object objArray = array;

            var data = serializer.Serialize(objArray, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            var deserializedArr = (object[])serializer.Deserialize(data, deserializationContext);

            Assert.Equal(array[0], deserializedArr[0]);
            Assert.Equal(array[1], deserializedArr[1]);
            Assert.Equal(array[2], deserializedArr[2]);

            object[] nestedArray = (object[])array[3];
            object[] deserializedNestedArray = (object[])deserializedArr[3];

            for (int i = 0; i < nestedArray.Length; i++)
            {
                Assert.Equal(nestedArray[i], deserializedNestedArray[i]);
            }
        }



        [Fact]
        public void TestMethodRootObjectArraySerializationWithNewSerializer()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            object[] array = new object[] { 1, 10, 100005, new object[] { "Nested", "Array", 1 } };
            object objArray = array;

            var data = serializer.Serialize(objArray, null);

            ClearGlobalTypeCache();

            BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            var deserializedArr = (object[])serializer.Deserialize(data, deserializationContext);

            Assert.Equal(array[0], deserializedArr[0]);
            Assert.Equal(array[1], deserializedArr[1]);
            Assert.Equal(array[2], deserializedArr[2]);

            object[] nestedArray = (object[])array[3];
            object[] deserializedNestedArray = (object[])deserializedArr[3];

            for (int i = 0; i < nestedArray.Length; i++)
            {
                Assert.Equal(nestedArray[i], deserializedNestedArray[i]);
            }
        }


        [Fact]
        public void TestMethodRootByteArraySerializationWithNewSerializer()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            byte[] array = new byte[] { 1, 10, 5, 8, 240 };
            object objArray = array;

            var data = serializer.Serialize(objArray, null);

            ClearGlobalTypeCache();

            BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            var deserializedArr = (byte[])serializer.Deserialize(data, deserializationContext);

            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(array[i], deserializedArr[i]);
            }
        }


        [Fact]
        public void TestMethodRootObjectListSerializationWithNewSerializer()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            List<SubObject> list = new List<SubObject>(new SubObject[] { new SubObject() { SubId = 7, SubDescr = "Test" }, new SubObject() { SubId = 8, SubDescr = "Test 2" } });
            object objList = list;

            var data = serializer.Serialize(objList, null);

            ClearGlobalTypeCache();

            BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            var deserializedList = (IList<SubObject>)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(list[0].SubId, deserializedList[0].SubId);
            Assert.Equal(list[0].SubDescr, deserializedList[0].SubDescr);

            Assert.Equal(list[1].SubId, deserializedList[1].SubId);
            Assert.Equal(list[1].SubDescr, deserializedList[1].SubDescr);
        }



        [Fact]
        public void TestMethodEmptyObjectArraySerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            TestObject testObj = new TestObject();
            testObj.ObjectArray = new object[] { 1, 10, 100005, new object[0] };
            testObj.AnyObject = testObj.ObjectArray;

            var data = serializer.Serialize(testObj, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            TestObject deserializedObj = (TestObject)serializer.Deserialize(data, deserializationContext);

            {
                Assert.Equal<int>(testObj.ObjectArray.Length, deserializedObj.ObjectArray.Length);
                object[] targetArr = (object[])deserializedObj.ObjectArray[3];

                Assert.Empty(targetArr);
            }

            // any object check
            {
                Assert.Equal<int>(((object[])testObj.AnyObject).Length, ((object[])deserializedObj.AnyObject).Length);
                object[] targetArr = (object[])((object[])deserializedObj.AnyObject)[3];

                Assert.Empty(targetArr);
            }
        }

        [Fact]
        public void TestMethodEnumSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            EnumTestObject testObject = new EnumTestObject();
            testObject.Type = IOCTalk.Common.Interface.Communication.MessageType.MethodInvokeRequest;
            testObject.InObjectEnum = TypeCode.Int16;

            var data = serializer.Serialize(testObject, null);

            BinarySerializer.ClearGlobalStructureCache();

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            EnumTestObject deserializedObj = (EnumTestObject)serializer.Deserialize(data, deserializationContext);

            Assert.Equal<MessageType>(testObject.Type, deserializedObj.Type);
            Assert.Equal<TypeCode>((TypeCode)testObject.InObjectEnum, (TypeCode)deserializedObj.InObjectEnum);
        }

        [Fact]
        public void TestMethodSpecialDerivedEnumSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            EnumSpecialTestObject testObject = new EnumSpecialTestObject();
            testObject.ByteEnum = EnumDerrivedFromByte.TestByte1;
            testObject.ShortEnum = EnumDerrivedFromShort.TestShort2;

            var data = serializer.Serialize(testObject, null);

            BinarySerializer.ClearGlobalStructureCache();

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            EnumSpecialTestObject deserializedObj = (EnumSpecialTestObject)serializer.Deserialize(data, deserializationContext);

            Assert.Equal<EnumDerrivedFromByte>(testObject.ByteEnum, deserializedObj.ByteEnum);
            Assert.Equal<EnumDerrivedFromShort>(testObject.ShortEnum, deserializedObj.ShortEnum);
        }

        [Fact]
        public void TestMethodMessageSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            EnumTestObject testObject = new EnumTestObject();
            testObject.Type = IOCTalk.Common.Interface.Communication.MessageType.MethodInvokeRequest;
            testObject.InObjectEnum = TypeCode.Double;
            GenericMessage msg = new GenericMessage(1, testObject);

            var data = serializer.Serialize(msg, null);

            BinarySerializer.ClearGlobalStructureCache();

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            GenericMessage deserializedMsg = (GenericMessage)serializer.Deserialize(data, deserializationContext);

            Assert.Equal<MessageType>(msg.Type, deserializedMsg.Type);
            Assert.Equal(((EnumTestObject)msg.Payload).InObjectEnum, ((EnumTestObject)deserializedMsg.Payload).InObjectEnum);
        }

        [Fact]
        public void TestMethodInterfaceWithoutSetPropertiesSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            NoPropertiesTestHolder noPropertiesHolder = new NoPropertiesTestHolder();
            noPropertiesHolder.NoPropertiesObject = new TestImplementationWithoutSetProperties();
            noPropertiesHolder.Dummy = "Test324545fd43";

            var data = serializer.Serialize(noPropertiesHolder, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            NoPropertiesTestHolder deserializedObj = (NoPropertiesTestHolder)serializer.Deserialize(data, deserializationContext);

            Assert.NotNull(deserializedObj.NoPropertiesObject);
            Assert.Equal(noPropertiesHolder.Dummy, deserializedObj.Dummy);
        }



        [Fact]
        public void TestMethodSimpleMessageSerialize()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            Communication.Common.GenericMessage msg = new Communication.Common.GenericMessage();
            msg.Name = "Test";
            byte[] result = serializer.Serialize(msg, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            IGenericMessage resultMsg = (IGenericMessage)serializer.Deserialize(result, deserializationContext);

            Assert.Equal(msg.Name, resultMsg.Name);
        }


        /// <summary>
        /// Tolerant binary read test: PropertyExtended type contains a extended Name property
        /// </summary>
        [Fact]
        public void TestMethodBinaryLayoutModification_ExtendedProperty()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            int exptectedId = 5;

            // Binary meta and payload data of PropertyExtended instance without Name property
            byte[] data = new byte[] { 176, 149, 221, 227, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 60, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 69, 120, 116, 101, 110, 100, 101, 100, 1, 0, 3, 0, 3, 0, 0, 0, 2, 73, 68, 0, 1, 5, 0, 0, 0 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyExtended resultObject = (PropertyExtended)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(exptectedId, resultObject.ID);

            // 2. Check caching
            PropertyExtended resultObject2 = (PropertyExtended)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(exptectedId, resultObject2.ID);
        }

        /// <summary>
        /// Tolerant binary read test: PropertyRemoved type does not contain the Name property anymore
        /// </summary>
        [Fact]
        public void TestMethodBinaryLayoutModification_RemovedProperty()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            int exptectedId = 7;

            #region byte generation code
            // Name property must be uncommented in PropertyRemoved
            //PropertyRemoved removedPropertyObj = new PropertyRemoved();
            //removedPropertyObj.ID = exptectedId;
            //removedPropertyObj.Name = "test";

            //byte[] result = serializer.Serialize(removedPropertyObj, null);
            //string resultBytesStr = string.Join(",", result);
            #endregion

            // Binary meta and payload data of PropertyExtended instance without Name property
            byte[] data = new byte[] { 180, 32, 156, 29, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 59, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 82, 101, 109, 111, 118, 101, 100, 2, 0, 3, 0, 3, 0, 0, 0, 2, 73, 68, 0, 2, 0, 2, 0, 0, 0, 4, 78, 97, 109, 101, 1, 1, 7, 0, 0, 0, 3, 4, 116, 101, 115, 116 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyRemoved resultObject = (PropertyRemoved)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(exptectedId, resultObject.ID);
        }




        [Fact]
        public void TestMethodBinaryLayoutModification_AdvancedModification()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            bool expUnchanged1 = true;
            string expUnchanged2 = "test it 4234325";

            #region byte generation code
            // before using: added properties must be commented and removed properties uncommented
            //PropertyChangedAdvanced propertiesWithoutAdded = new PropertyChangedAdvanced();
            //propertiesWithoutAdded.Unchanged1 = expUnchanged1;
            //propertiesWithoutAdded.Unchanged2 = expUnchanged2;
            //propertiesWithoutAdded.Removed1 = 999999d;
            //propertiesWithoutAdded.Removed2 = "removed dummy value";

            //byte[] result = serializer.Serialize(propertiesWithoutAdded, null);
            //string resultBytesStr = string.Join(",", result);
            #endregion

            // Binary meta and payload data of PropertyExtended instance without Name property
            byte[] data = new byte[] { 243, 125, 29, 83, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 67, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 67, 104, 97, 110, 103, 101, 100, 65, 100, 118, 97, 110, 99, 101, 100, 4, 0, 10, 0, 10, 0, 0, 0, 8, 82, 101, 109, 111, 118, 101, 100, 49, 0, 4, 0, 4, 0, 0, 0, 10, 85, 110, 99, 104, 97, 110, 103, 101, 100, 49, 0, 2, 0, 2, 0, 0, 0, 8, 82, 101, 109, 111, 118, 101, 100, 50, 1, 2, 0, 2, 0, 0, 0, 10, 85, 110, 99, 104, 97, 110, 103, 101, 100, 50, 1, 1, 0, 0, 0, 0, 126, 132, 46, 65, 1, 3, 19, 114, 101, 109, 111, 118, 101, 100, 32, 100, 117, 109, 109, 121, 32, 118, 97, 108, 117, 101, 3, 15, 116, 101, 115, 116, 32, 105, 116, 32, 52, 50, 51, 52, 51, 50, 53 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyChangedAdvanced resultObject = (PropertyChangedAdvanced)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(expUnchanged1, resultObject.Unchanged1);
            Assert.Equal(expUnchanged2, resultObject.Unchanged2);
            Assert.Null(resultObject.Added4);
        }


        [Fact]
        public void TestMethodBinaryLayoutModification_IntToLong()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            long expectedNumber = 435387293;

            #region byte generation code
            // before use: NumberProperty property must be changed to int
            //PropertyChangeIntToLong propertyChangedObj = new PropertyChangeIntToLong();
            //propertyChangedObj.NumberProperty = (int)expectedNumber;

            //byte[] result = serializer.Serialize(propertyChangedObj, null);
            //string resultBytesStr = string.Join(",", result);
            #endregion

            // Binary meta and payload data of PropertyChangeIntToLong instance with int property
            byte[] data = new byte[] { 60, 9, 90, 236, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 67, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 67, 104, 97, 110, 103, 101, 73, 110, 116, 84, 111, 76, 111, 110, 103, 1, 0, 3, 0, 3, 0, 0, 0, 14, 78, 117, 109, 98, 101, 114, 80, 114, 111, 112, 101, 114, 116, 121, 0, 1, 157, 123, 243, 25 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyChangeIntToLong resultObject = (PropertyChangeIntToLong)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(expectedNumber, resultObject.NumberProperty);
        }

        [Fact]
        public void TestMethodBinaryLayoutModification_DoubleToDecimal()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            long expectedNumber = 435387293;

            #region byte generation code
            // before use: NumberProperty property must be changed to double
            //PropertyChangeDoubleToDecimal propertyChangedObj = new PropertyChangeDoubleToDecimal();
            //propertyChangedObj.NumberProperty = (int)expectedNumber;

            //byte[] result = serializer.Serialize(propertyChangedObj, null);
            //string resultBytesStr = string.Join(",", result);
            #endregion

            // Binary meta and payload data of PropertyChangeIntToLong instance with int property
            byte[] data = new byte[] { 130, 237, 223, 38, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 73, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 67, 104, 97, 110, 103, 101, 68, 111, 117, 98, 108, 101, 84, 111, 68, 101, 99, 105, 109, 97, 108, 1, 0, 10, 0, 10, 0, 0, 0, 14, 78, 117, 109, 98, 101, 114, 80, 114, 111, 112, 101, 114, 116, 121, 0, 1, 0, 0, 0, 157, 123, 243, 185, 65 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyChangeDoubleToDecimal resultObject = (PropertyChangeDoubleToDecimal)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(expectedNumber, resultObject.NumberProperty);
        }



        [Fact]
        public void TestMethodBinaryLayoutModification_NullableChange()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());
            int expectedProperty1 = 9386428;

            #region byte generation code
            // before use: Properies property must be changed to non nullable
            //PropertyChangedNullable propertyChangedObj = new PropertyChangedNullable();
            //propertyChangedObj.Property1 = (int)expectedProperty1;

            //byte[] result = serializer.Serialize(propertyChangedObj, null);
            //string resultBytesStr = string.Join(",", result);
            #endregion

            // Binary meta and payload data of PropertyChangedNullable instance with not nullable properties
            byte[] data = new byte[] { 27, 35, 82, 199, 4, 1, 17, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 67, 66, 83, 65, 71, 46, 73, 79, 67, 84, 97, 108, 107, 46, 84, 101, 115, 116, 46, 84, 101, 115, 116, 79, 98, 106, 101, 99, 116, 115, 46, 67, 104, 97, 110, 103, 101, 100, 76, 97, 121, 111, 117, 116, 46, 80, 114, 111, 112, 101, 114, 116, 121, 67, 104, 97, 110, 103, 101, 100, 78, 117, 108, 108, 97, 98, 108, 101, 1, 0, 3, 0, 3, 0, 0, 0, 9, 80, 114, 111, 112, 101, 114, 116, 121, 49, 0, 1, 188, 57, 143, 0 };

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            PropertyChangedNullable resultObject = (PropertyChangedNullable)serializer.Deserialize(data, deserializationContext);

            Assert.Equal(expectedProperty1, resultObject.Property1);
        }



        //[Fact]
        //public void TestMethodBinary_EnumSerialization()
        //{
        //    BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

        //    BaseTestObject simpleObj = new BaseTestObject();
        //    simpleObj.BaseProperty = "Hallo";
        //    simpleObj.TestId = 53435422;

        //    byte[] result = serializer.Serialize(simpleObj, null);

        //    BaseTestObject resultObject = (BaseTestObject)serializer.Deserialize(result, null);

        //    Assert.Equal(simpleObj.BaseProperty, resultObject.BaseProperty);
        //}



        [Fact]
        public void TestMethodBinary_TypeDescriptionSerialization()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            TypeDescription typeRefItem = new TypeDescription();
            typeRefItem.TypeReference = typeof(BaseTestObject);
            typeRefItem.TypeReference2 = typeof(int);
            typeRefItem.TypeReference3 = null;
            typeRefItem.OtherProperty = "Hallo";

            byte[] result = serializer.Serialize(typeRefItem, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            TypeDescription resultObject = (TypeDescription)serializer.Deserialize(result, deserializationContext);

            Assert.Equal(typeRefItem.TypeReference, resultObject.TypeReference);
            Assert.Equal(typeRefItem.TypeReference2, resultObject.TypeReference2);
            Assert.Null(resultObject.TypeReference3);
            Assert.Equal(typeRefItem.OtherProperty, resultObject.OtherProperty);
        }

        [Fact]
        public void TestMethodBinary_MethodDescriptionSerialization()
        {
            var unknownTypeResovler = new UnknowTestTypeResolver();
            BinarySerializer serializer = new BinarySerializer(unknownTypeResovler);

            // register string hash property
            SerializationContext serializeContext = new SerializationContext(serializer, false, null);
            SerializationContext deserializeContext = new SerializationContext(serializer, true, null);
            serializeContext.RegisterStringHashProperty(typeof(StringHashTestItem), nameof(StringHashTestItem.HashString));
            deserializeContext.RegisterStringHashProperty(typeof(StringHashTestItem), nameof(StringHashTestItem.HashString));

            StringHashTestItem hastStringObj = new StringHashTestItem();
            hastStringObj.HashString = "Some common data string";

            {
                byte[] result = serializer.Serialize(hastStringObj, null);

                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                StringHashTestItem deserialized = (StringHashTestItem)serializer.Deserialize(result, deserializeContext);

                Assert.Equal(hastStringObj.HashString, deserialized.HashString);
            }

            {
                byte[] result2 = serializer.Serialize(hastStringObj, null);

                BinarySerializer serializer2 = new BinarySerializer(new UnknowTestTypeResolver());
                SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
                StringHashTestItem deserialized2 = (StringHashTestItem)serializer2.Deserialize(result2, deserializeContext);

                Assert.Equal(hastStringObj.HashString, deserialized2.HashString);
            }
        }

        [Fact]
        public void TestMethodBinaryMessageSerialization()
        {
            BinaryMessageSerializer msgSerializer = new BinaryMessageSerializer();

            GenericMessage msg = new GenericMessage();
            msg.Type = MessageType.MethodInvokeRequest;
            msg.Target = typeof(TypeDescription).FullName;
            msg.Name = "CallThisMethod(int)";
            msg.RequestId = 1;
            msg.Payload = new object[] { 1, 2, 3, 4 };

            var bytes = msgSerializer.SerializeToBytes(msg, null);
            // 425
            DeserializeAndCheck(msgSerializer, msg, bytes);

            var bytes2 = msgSerializer.SerializeToBytes(msg, null);
            // 77
            DeserializeAndCheck(msgSerializer, msg, bytes2);

            var bytes3 = msgSerializer.SerializeToBytes(msg, null);
            // 77
            DeserializeAndCheck(msgSerializer, msg, bytes3);

        }

        private static void DeserializeAndCheck(BinaryMessageSerializer msgSerializer, GenericMessage orgMsg, byte[] bytes)
        {
            GenericMessage deserializedMsg = (GenericMessage)msgSerializer.DeserializeFromBytes(bytes, bytes.Length, null, 0);

            Assert.Equal(orgMsg.Type, deserializedMsg.Type);
            Assert.Equal(orgMsg.Target, deserializedMsg.Target);
            Assert.Equal(orgMsg.Name, deserializedMsg.Name);
            Assert.Equal(orgMsg.RequestId, deserializedMsg.RequestId);
            Assert.True(orgMsg.Payload is object[] arr && arr.Length == 4);
        }


        [Fact]
        public void TestMethodBinaryMessageSerializationComplexPayload()
        {
            BinaryMessageSerializer msgSerializer = new BinaryMessageSerializer();

            TalkCompositionHost containerHost = new TalkCompositionHost("TestContainer");
            msgSerializer.RegisterContainerHost(containerHost);

            var testItem = new TestItem() { ID = 5, Name = "Test it" };

            GenericMessage msg = new GenericMessage();
            msg.Type = MessageType.MethodInvokeRequest;
            msg.Target = typeof(ITestService).FullName;
            msg.Name = "CallTest(BSAG.IOCTalk.Serialization.Binary.Test.Model.ITestItem)";
            msg.RequestId = 1;
            msg.Payload = new object[] { testItem };

            IInvokeMethodInfo invokeInfo = new InvokeMethodInfo(typeof(ITestService), nameof(ITestService.CallTest), new Type[] { typeof(ITestItem) });
            var bytes = msgSerializer.SerializeToBytes(msg, invokeInfo);

            BinarySerializer.ClearGlobalStructureCache();

            BinaryMessageSerializer msgSerializerNew = new BinaryMessageSerializer();
            msgSerializerNew.RegisterContainerHost(new DummyContainerHost());

            GenericMessage deserializedMsg = (GenericMessage)msgSerializerNew.DeserializeFromBytes(bytes, bytes.Length, null, 0);
            object[] payloadArr = (object[])deserializedMsg.Payload;

            ITestItem testItemDeserialized = (ITestItem)payloadArr.First();

            Assert.Equal(testItem.ID, testItemDeserialized.ID);
            Assert.Equal(testItem.Name, testItemDeserialized.Name);
        }


        [Fact]
        public void TestMethodBinaryMessageSerializationComplexPayloadWithResponse()
        {
            BinaryMessageSerializer msgSerializer = new BinaryMessageSerializer();

            var testItem = new TestItem() { ID = 5, Name = "Test it" };

            GenericMessage msg = new GenericMessage();
            msg.Type = MessageType.MethodInvokeRequest;
            msg.Target = typeof(ITestService).FullName;
            msg.Name = "CallTest(BSAG.IOCTalk.Serialization.Binary.Test.Model.ITestItem)";
            msg.RequestId = 1;
            msg.Payload = new object[] { testItem };

            var bytes = msgSerializer.SerializeToBytes(msg, null);

            BinaryMessageSerializer msgSerializerNewDeserialize = new BinaryMessageSerializer();

            GenericMessage deserializedMsg = (GenericMessage)msgSerializerNewDeserialize.DeserializeFromBytes(bytes, bytes.Length, null, 0);
            object[] payloadArr = (object[])deserializedMsg.Payload;

            ITestItem testItemDeserialized = (ITestItem)payloadArr.First();

            Assert.Equal(testItem.ID, testItemDeserialized.ID);
            Assert.Equal(testItem.Name, testItemDeserialized.Name);

            // response test
            GenericMessage msg2 = new GenericMessage();
            msg2.Type = MessageType.MethodInvokeResponse;
            msg2.Target = typeof(ITestService).FullName;
            msg2.Name = "GetTestItem()";
            msg2.RequestId = 2;
            msg2.Payload = testItem;

            var bytesMsg2 = msgSerializer.SerializeToBytes(msg2, null);

            GenericMessage deserializedMsg2 = (GenericMessage)msgSerializerNewDeserialize.DeserializeFromBytes(bytesMsg2, bytesMsg2.Length, null, 0);

            ITestItem deserializedTest2 = (ITestItem)deserializedMsg2.Payload;

            Assert.Equal(testItem.ID, deserializedTest2.ID);
            Assert.Equal(testItem.Name, deserializedTest2.Name);
        }


        [Fact]
        public void TestMethodDateTimeOffsetSerialize()
        {
            BinarySerializer serializer = new BinarySerializer(new UnknowTestTypeResolver());

            DateTimeOffsetTest dtoTest = new DateTimeOffsetTest();
            dtoTest.Time1 = DateTimeOffset.Now;
            dtoTest.Time2 = null;
            dtoTest.Duration = TimeSpan.MaxValue;

            byte[] result = serializer.Serialize(dtoTest, null);

            SerializationContext deserializationContext = new SerializationContext(serializer, true, null);
            DateTimeOffsetTest resultObject = (DateTimeOffsetTest)serializer.Deserialize(result, deserializationContext);

            Assert.Equal(dtoTest.Time1, resultObject.Time1);
            Assert.Equal(dtoTest.Time2, resultObject.Time2);
            Assert.Equal(dtoTest.Duration, resultObject.Duration);
        }
    }
}
