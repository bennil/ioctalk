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

            return new ExposeTestLevel1 {  TestId = item.TestId, TestLevel1 = "exposed" };
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
    }
}
