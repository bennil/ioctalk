using BSAG.IOCTalk.Test.Interface.Expose;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.Expose
{
    public class ExposeSubTypeRoundTripTestService : IExposeSubTypeRoundTripTest
    {
        public IExposeTestBase TestExposeTypeMain(IExposeTestBase item)
        {
            var expectedType = typeof(ExposeTestLevel1);
            if (item.GetType().Equals(expectedType) == false)
            {
                throw new InvalidOperationException($"Unexpected concrete type received: {item.GetType().FullName}; Expected: {expectedType}");
            }

            return new ExposeTestLevel1 { TestId = item.TestId, TestLevel1 = "exposed" };
        }

        public IExposeTestOther TestExposeTypeOther(IExposeTestOther other)
        {
            var expectedType = typeof(ExposeTestBase);
            if (other.GetType().Equals(expectedType) == false)
            {
                throw new InvalidOperationException($"Unexpected concrete type received: {other.GetType().FullName}; Expected: {expectedType}");
            }

            return new ExposeTestBase { OtherTypeProperty = other.OtherTypeProperty };
        }


        public IReadOnlyList<IExposeTestBase> GetExposedCollection()
        {
            List<IExposeTestBase> list = new List<IExposeTestBase>();

            list.Add(new ExposeTestBase());
            list.Add(new ExposeTestLevel1());

            return list.AsReadOnly();
        }

        public IExposeTest2Base ExposeDerivedInterfaceTest(IExposeTest2Base input)
        {
            if (input is IExposeTest2Level1 level1)
                return level1;
            else
                return new ExposeTest2Level1
                {
                    BaseProperty = input.BaseProperty,
                    Level1Property = null
                };
        }

        public IExposeTest2Container ExposeDerivedInterfaceContainerTest(IExposeTest2Container input)
        {
            return new ExposeTest2Container { NestedItem = ExposeDerivedInterfaceTest(input.NestedItem) };
        }

        public IExposeTest3Base[] GetTest3BaseItems()
        {
            List<IExposeTest3Base> items = new List<IExposeTest3Base>();

            items.Add(new ExposeTest3Combined { SomeBasicProperty = "basic data1", OtherTypeProperty = "other data1" });
            items.Add(new ExposeTest3Combined { SomeBasicProperty = "basic data2", OtherTypeProperty = "other data2" });
            items.Add(new ExposeTest3Combined { SomeBasicProperty = "basic data3", OtherTypeProperty = "other data3" });

            return items.ToArray();
        }

        public IExposeTest3Other GetTest3OtherItem()
        {
            return new ExposeTest3Combined { SomeBasicProperty = "basic data", OtherTypeProperty = "other data expected" };
        }
    }
}
