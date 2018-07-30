using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BSAG.IOCTalk.Serialization.Json;
using BSAG.IOCTalk.Test.TestObjects;
using System.Diagnostics;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Test.TestObjects.MissingProperties;
using BSAG.IOCTalk.Test.TestObjects.NoProperties;
using BSAG.IOCTalk.Common.Attributes;
using System.Globalization;
using BSAG.IOCTalk.Serialization.Json.TypeStructure;
using System.Threading;
using Xunit;
using BSAG.IOCTalk.Test.Common.Service.Implementation;

namespace BSAG.IOCTalk.Common.Test
{
    public class JsonSerializerTest
    {
        [Fact]
        public void TestMethodBasicSerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);


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
            testObj.IntValueNullable2 = 45435323;


            string json = serializer.Serialize(testObj, null);


            TestObject deserializedTestObj = (TestObject)serializer.Deserialize(json, typeof(TestObject), null);

            Assert.Equal<int>(testObj.ID, deserializedTestObj.ID);
            Assert.Equal<string>(testObj.Description, deserializedTestObj.Description);

            Assert.Equal<int>(testObj.SubObject.SubId, deserializedTestObj.SubObject.SubId);
            Assert.Equal<string>(testObj.SubObject.SubDescr, deserializedTestObj.SubObject.SubDescr);

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

            Assert.Equal<string>(testObj.BaseProperty, deserializedTestObj.BaseProperty);

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


        [Fact]
        public void TestMethodInterfaceSerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);

            TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
            testObj1.TestBaseProperty = "TEST";
            testObj1.AdditionalProperty = "Some data";
            testObj1.DeepTestProperty1 = "Inherited interface property 1";
            testObj1.DeepTestProperty2 = "Inherited interface property 2";

            InterfRefObject interfRef = new InterfRefObject();
            interfRef.BaseObject = testObj1;

            string json = serializer.Serialize(interfRef, null);

            InterfRefObject deserializedObj = (InterfRefObject)serializer.Deserialize(json, typeof(InterfRefObject), null);

            Assert.Equal<string>(interfRef.BaseObject.TestBaseProperty, deserializedObj.BaseObject.TestBaseProperty);

            TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedObj.BaseObject;
            Assert.Null(implObj.AdditionalProperty);

            Assert.Equal<string>(testObj1.DeepTestProperty1, implObj.DeepTestProperty1);
            Assert.Equal<string>(testObj1.DeepTestProperty2, implObj.DeepTestProperty2);
        }


        /// <summary>
        /// Tests the method interface inheritance serialization.
        /// Expects typed meta serialization attribute because of interface inheritance serialization.
        /// </summary>
        [Fact]
        public void TestMethodInterfaceInheritanceSerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);

            TestInterfaceImpl1 testObj1 = new TestInterfaceImpl1();
            testObj1.TestBaseProperty = "TEST";
            testObj1.AdditionalProperty = "Some data";
            testObj1.DeepTestProperty1 = "Inherited interface property 1";
            testObj1.DeepTestProperty2 = "Inherited interface property 2";

            InterfRefObject interfRef = new InterfRefObject();
            interfRef.BaseObjectInstance = testObj1;

            string json = serializer.Serialize(interfRef, null);

            InterfRefObject deserializedObj = (InterfRefObject)serializer.Deserialize(json, typeof(InterfRefObject), null);

            Assert.Equal<string>(((ITestInterfaceBase)interfRef.BaseObjectInstance).TestBaseProperty, ((ITestInterfaceBase)deserializedObj.BaseObjectInstance).TestBaseProperty);

            TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedObj.BaseObjectInstance;
            Assert.Null(implObj.AdditionalProperty);

            Assert.Equal<string>(testObj1.DeepTestProperty1, implObj.DeepTestProperty1);
            Assert.Equal<string>(testObj1.DeepTestProperty2, implObj.DeepTestProperty2);
        }


        /// <summary>
        /// Tests the method interface inheritance array serialization.
        /// Expects typed meta serialization attribute because of interface inheritance serialization.
        /// </summary>
        [Fact]
        public void TestMethodInterfaceInheritanceArraySerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);

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

            string json = serializer.Serialize(collHolder, null);

            TestInterfaceImpl1Collections deserializedObj = (TestInterfaceImpl1Collections)serializer.Deserialize(json, typeof(TestInterfaceImpl1Collections), null);

            Assert.Equal<int>(collHolder.Array.Length, deserializedObj.Array.Length);
            for (int i = 0; i < collHolder.Array.Length; i++)
            {
                ITestInterfaceBase orginalItem = collHolder.Array[i];
                ITestInterfaceBase deserializedItem = deserializedObj.Array[i];

                CheckCollectionItem(orginalItem, deserializedItem);
            }

            for (int i = 0; i < collHolder.List.Count; i++)
            {
                ITestInterfaceBase orginalItem = collHolder.List[i];
                ITestInterfaceBase deserializedItem = deserializedObj.List[i];

                CheckCollectionItem(orginalItem, deserializedItem);
            }

            for (int i = 0; i < collHolder.ObjectArray.Length; i++)
            {
                ITestInterfaceBase orginalItem = (ITestInterfaceBase)collHolder.ObjectArray[i];
                ITestInterfaceBase deserializedItem = (ITestInterfaceBase)deserializedObj.ObjectArray[i];

                CheckCollectionItem(orginalItem, deserializedItem);
            }

            // check own collection implementation
            var deserializedEnumerator = deserializedObj.OwnCollection.GetEnumerator();
            foreach (var item in collHolder.OwnCollection)
            {
                deserializedEnumerator.MoveNext();

                Assert.True(item.Equals(deserializedEnumerator.Current));
            }
        }

        private static void CheckCollectionItem(ITestInterfaceBase orginalItem, ITestInterfaceBase deserializedItem)
        {
            Assert.Equal<string>(orginalItem.TestBaseProperty, deserializedItem.TestBaseProperty);
            Assert.Equal<string>(orginalItem.DeepTestProperty1, deserializedItem.DeepTestProperty1);
            Assert.Equal<string>(orginalItem.DeepTestProperty2, deserializedItem.DeepTestProperty2);

            TestInterfaceImpl1 implObj = (TestInterfaceImpl1)deserializedItem;
            Assert.Null(implObj.AdditionalProperty);
        }


        private Type UnknownTypeResolver(SerializationContext context)
        {
            if (context.InterfaceType != null)
            {
                if (context.InterfaceType.Equals(typeof(ITestInterfaceBase)))
                {
                    return typeof(TestInterfaceImpl1);
                }
                else if (context.InterfaceType.Equals(typeof(ITestInterfaceWithoutSetProperties)))
                {
                    return typeof(TestImplementationWithoutSetProperties);
                }
            }
            else if (context.Key == "BaseObject")
            {
                return typeof(TestInterfaceImpl1);
            }
            else if (context.Key == "PerfData")
            {
                return typeof(PerformanceData);
            }
            else if (context.Key == "ObjectArray"
                && context.ArrayIndex.HasValue)
            {
                switch (context.ArrayIndex.Value)
                {
                    case 3:
                        return typeof(TimeSpan);

                    case 5:
                        return typeof(SubObject);
                }
            }

            return null;
        }

        private Type SpecialTypeResolver(Type sourceType)
        {
            // check expose sub type attribute
            var exposureAttributes = sourceType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
            if (exposureAttributes.Length > 0)
            {
                return ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
            }

            return null;
        }


        [Fact]
        public void TestMethodObjectArrayInObjectArraySerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolverOjectArray, SpecialTypeResolver);

            TestObject testObj = new TestObject();
            testObj.ObjectArray = new object[] { 1, 10, 100005, new object[] { "Nested", "Array", 1 } };
            testObj.AnyObject = testObj.ObjectArray;

            string json = serializer.Serialize(testObj, null);

            TestObject deserializedObj = (TestObject)serializer.Deserialize(json, typeof(TestObject), null);

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
        public void TestMethodEmptyObjectArraySerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolverOjectArray, SpecialTypeResolver);

            TestObject testObj = new TestObject();
            testObj.ObjectArray = new object[] { 1, 10, 100005, new object[0] };
            testObj.AnyObject = testObj.ObjectArray;

            string json = serializer.Serialize(testObj, null);

            TestObject deserializedObj = (TestObject)serializer.Deserialize(json, typeof(TestObject), null);

            {
                Assert.Equal<int>(testObj.ObjectArray.Length, deserializedObj.ObjectArray.Length);
                object[] targetArr = (object[])deserializedObj.ObjectArray[3];

                Assert.Equal<int>(0, targetArr.Length);
            }

            // any object check
            {
                Assert.Equal<int>(((object[])testObj.AnyObject).Length, ((object[])deserializedObj.AnyObject).Length);
                object[] targetArr = (object[])((object[])deserializedObj.AnyObject)[3];

                Assert.Equal<int>(0, targetArr.Length);
            }
        }


        private Type UnknownTypeResolverOjectArray(SerializationContext context)
        {
            if (context.Key == "ObjectArray"
                && context.ArrayIndex.HasValue)
            {
                switch (context.ArrayIndex.Value)
                {
                    case 3:
                        return typeof(object[]);
                }
            }
            else if (context.Key == "AnyObject")
            {
                if (context.ArrayIndex.HasValue)
                {
                    switch (context.ArrayIndex.Value)
                    {
                        case 3:
                            return typeof(object[]);
                    }
                }
                else
                {
                    return typeof(object[]);
                }
            }

            return null;
        }


        [Fact]
        public void TestMethodOutOfOrderDeserialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();

            string outOfOrderJson = "{\"SubDescr\":\"Test String\",\"SubId\":2}";

            SubObject subObj = (SubObject)serializer.Deserialize(outOfOrderJson, typeof(SubObject), null);

            Assert.Equal<int>(2, subObj.SubId);
            Assert.Equal<string>("Test String", subObj.SubDescr);
        }


        [Fact]
        public void TestMethodEnumSerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();

            EnumTestObject testObject = new EnumTestObject();
            testObject.Type = IOCTalk.Common.Interface.Communication.MessageType.MethodInvokeRequest;

            string json = serializer.Serialize(testObject, null);
            EnumTestObject deserializedObj = (EnumTestObject)serializer.Deserialize(json, typeof(EnumTestObject), null);

            Assert.Equal<MessageType>(testObject.Type, deserializedObj.Type);
        }

        [Fact]
        public void TestMethodMissingProperties()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();
            serializer.IsMissingFieldDataAllowed = true;

            FullFeaturedObject fullFeaturedObj = new FullFeaturedObject();
            fullFeaturedObj.Property1 = 3;
            fullFeaturedObj.Property2 = "2sdfjsalf32p3sdf";
            fullFeaturedObj.Property3 = 4.67;
            fullFeaturedObj.Property4 = "Test";
            fullFeaturedObj.Property5 = 7;

            string json = serializer.Serialize(fullFeaturedObj, null);
            FeaturedObject deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);

            // do the same with different property order
            json = "{\"Property1\":3,\"Property2\":\"2sdfjsalf32p3sdf\",\"Property3\":4.67,\"Property4\":\"Test\",\"Property5\":7}";

            deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);
        }

        [Fact]
        public void TestMethodMissingSubObjectProperties()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();
            serializer.IsMissingFieldDataAllowed = true;

            FullFeaturedSubObject fullFeaturedObj = new FullFeaturedSubObject();
            fullFeaturedObj.Property1 = 3;
            fullFeaturedObj.Property2 = "2sdfjsalf32p3sdf";
            fullFeaturedObj.Property3 = 4.67;
            fullFeaturedObj.Property4 = "Test";
            fullFeaturedObj.Property5 = 7;

            fullFeaturedObj.SubObjectProperty = new SubObject() { SubId = 1, SubDescr = "ldsfadsfs" };

            string json = serializer.Serialize(fullFeaturedObj, null);
            FeaturedObject deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);

            // do the same with different property orders
            json = "{\"Property1\":3,\"Property2\":\"2sdfjsalf32p3sdf\",\"Property3\":4.67,\"SubObjectProperty\":{\"SubId\":1,\"SubDescr\":\"ldsfadsfs\"},\"Property4\":\"Test\",\"Property5\":7}";

            deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);

            json = "{\"Property1\":3,\"Property2\":\"2sdfjsalf32p3sdf\",\"Property3\":4.67,\"Property4\":\"Test\",\"Property5\":7,\"SubObjectProperty\":{\"SubId\":1,\"SubDescr\":\"ldsfadsfs\"}}";

            deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);
        }

        [Fact]
        public void TestMethodMissingMultiSubObjectProperties()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();
            serializer.IsMissingFieldDataAllowed = true;

            FullFeaturedMultiSubObject fullFeaturedObj = new FullFeaturedMultiSubObject();
            fullFeaturedObj.Property1 = 3;
            fullFeaturedObj.Property2 = "2sdfjsalf32p3sdf";
            fullFeaturedObj.Property3 = 4.67;
            fullFeaturedObj.Property4 = "Test";
            fullFeaturedObj.Property5 = 7;
            fullFeaturedObj.Holder1 = new SubObjectHolder() { SubSubObject = new SubObject() { SubId = 3, SubDescr = "ldfdlsajsd" } };
            fullFeaturedObj.Holder2 = new SubObjectHolder() { SubSubObject = new SubObject() { SubId = 6, SubDescr = "dsgfdgsfdgfs dfdsf" } };

            fullFeaturedObj.SubObjectProperty = new SubObject() { SubId = 1, SubDescr = "ldsfadsfs" };

            string json = serializer.Serialize(fullFeaturedObj, null);
            FeaturedObject deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);

            // do the same with different property orders
            json = "{\"Property1\":3,\"Property2\":\"2sdfjsalf32p3sdf\",\"Property3\":4.67,\"Holder1\":{\"SubSubObject\":{\"SubId\":3,\"SubDescr\":\"ldfdlsajsd\"}},\"Holder2\":{\"SubSubObject\":{\"SubId\":6,\"SubDescr\":\"dsgfdgsfdgfs dfdsf\"}},\"SubObjectProperty\":{\"SubId\":1,\"SubDescr\":\"ldsfadsfs\"},\"Property4\":\"Test\",\"Property5\":7}";

            deserializedObj = (FeaturedObject)serializer.Deserialize(json, typeof(FeaturedObject), null);

            Assert.Equal<int>(fullFeaturedObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(fullFeaturedObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(fullFeaturedObj.Property3, deserializedObj.Property3);
        }


        [Fact]
        public void TestMethodMissingPropertiesOtherWay()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer();
            serializer.IsMissingFieldDataAllowed = true;

            FeaturedObject featuredObj = new FeaturedObject();
            featuredObj.Property1 = 3;
            featuredObj.Property2 = "2sdfjsalf32p3sdf";
            featuredObj.Property3 = 4.67;

            string json = serializer.Serialize(featuredObj, null);
            FullFeaturedObject deserializedObj = (FullFeaturedObject)serializer.Deserialize(json, typeof(FullFeaturedObject), null);

            Assert.Equal<int>(featuredObj.Property1, deserializedObj.Property1);
            Assert.Equal<string>(featuredObj.Property2, deserializedObj.Property2);
            Assert.Equal<double>(featuredObj.Property3, deserializedObj.Property3);

            Assert.Null(deserializedObj.Property4);
            Assert.Equal<int>(0, deserializedObj.Property5);
        }

        [Fact]
        public void TestMethodInterfaceWithoutSetPropertiesSerialization()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);
            serializer.IsMissingFieldDataAllowed = true;

            NoPropertiesTestHolder noPropertiesHolder = new NoPropertiesTestHolder();
            noPropertiesHolder.NoPropertiesObject = new TestImplementationWithoutSetProperties();
            noPropertiesHolder.Dummy = "Test324545fd43";

            string json = serializer.Serialize(noPropertiesHolder, null);
            NoPropertiesTestHolder deserializedObj = (NoPropertiesTestHolder)serializer.Deserialize(json, typeof(NoPropertiesTestHolder), null);

            Assert.NotNull(deserializedObj.NoPropertiesObject);
            Assert.Equal<string>(noPropertiesHolder.Dummy, deserializedObj.Dummy);
        }

        [Fact]
        public void TestMethodNumberPrecision()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);
            serializer.IsMissingFieldDataAllowed = true;
            
            CheckNumberSerialization<decimal>(serializer, 0.0000001m, "0", "0000001");
            CheckNumberSerialization<double>(serializer, 0.0001d, "0", "0001");
            CheckNumberSerialization<double>(serializer, 123456789.0001d, "123456789", "0001");
            CheckNumberSerialization<double>(serializer, 12345678901.0001d, "12345678901", "0001");
            CheckNumberSerialization<double>(serializer, 99999999999.9999d, "99999999999", "9999");
            CheckNumberSerialization<decimal>(serializer, 999.999999999999m, "999", "999999999999");
            CheckNumberSerialization<decimal>(serializer, 123456789012.0001m, "123456789012", "0001");
        }

        private void CheckNumberSerialization<T>(JsonObjectSerializer serializer, T sourceValue, string expectedIntValueStr, string expectedDecimalPlaces)
        {
            NumberPrecisionItem numberPrecision = new NumberPrecisionItem();
            numberPrecision.ObjectNumberValue = sourceValue;

            string json = serializer.Serialize(numberPrecision, null);
            NumberPrecisionItem deserializedObj = (NumberPrecisionItem)serializer.Deserialize(json, typeof(NumberPrecisionItem), null);

            Assert.True(deserializedObj.ObjectNumberValue is T);

            Assert.Equal<T>(sourceValue, (T)deserializedObj.ObjectNumberValue);

            // string compare
            if (expectedIntValueStr != null)
            {
                string deserializedStr = deserializedObj.ObjectNumberValue.ToString();
                string expectedValueStr = string.Concat(expectedIntValueStr, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, expectedDecimalPlaces);

                Assert.Equal<string>(expectedValueStr, deserializedStr);
            }
        }

        [Fact]
        public void TestMethodDateTimieSerializationTest()
        {
            JsonObjectSerializer serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);
            
            // 2015-10-12T17:24:33.221224
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.2512345"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.251234"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.25123"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.2512"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.251"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.25"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.2"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33"), serializer);

            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.1000001"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.100001"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.10001"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.1001"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.101"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.11"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.1"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.0"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.000"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.00000"), serializer);

            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.9999999"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.999999"), serializer);
            CheckDateTimeSerialisation(DateTime.Parse("2015-10-12 17:24:33.9"), serializer);
            
            ExplicitCheckTimeDeserialization("17:24:33.1234567");
            ExplicitCheckTimeDeserialization("17:24:33.123456");
            ExplicitCheckTimeDeserialization("17:24:33.12345");
            ExplicitCheckTimeDeserialization("17:24:33.1234");
            ExplicitCheckTimeDeserialization("17:24:33.123");
            ExplicitCheckTimeDeserialization("17:24:33.12");
            ExplicitCheckTimeDeserialization("17:24:33.1");
            ExplicitCheckTimeDeserialization("17:24:33");
        }

        private static void ExplicitCheckTimeDeserialization(string timeStr)
        {
            StructureTimeSpan sTimeSpan = new StructureTimeSpan("test", false);
            TimeSpan expectedTime = TimeSpan.Parse(timeStr);
            string jsonPart = "\"test\":\"" + timeStr + "\"";
            int readIndex = 0;
            TimeSpan result = (TimeSpan)sTimeSpan.Deserialize(jsonPart, ref readIndex, null);

            Assert.Equal<long>(expectedTime.Ticks, result.Ticks);
        }

        private void CheckDateTimeSerialisation(DateTime dateTime, JsonObjectSerializer serializer)
        {
            TestObject testObj = new TestObject();
            testObj.TimeSpanValue = dateTime.TimeOfDay;            
            testObj.DateTimeValue = dateTime;

            string json = serializer.Serialize(testObj, null);

            TestObject deserializedTestObj = (TestObject)serializer.Deserialize(json, typeof(TestObject), null);


            Assert.Equal<long>(testObj.TimeSpanValue.Ticks, deserializedTestObj.TimeSpanValue.Ticks);
            Assert.Equal<long>(testObj.DateTimeValue.Ticks, deserializedTestObj.DateTimeValue.Ticks);
        }

    }
}
