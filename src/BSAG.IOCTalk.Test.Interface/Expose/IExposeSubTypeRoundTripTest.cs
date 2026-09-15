using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Interface.Expose
{
    public interface IExposeSubTypeRoundTripTest
    {
        IExposeTestBase TestExposeTypeMain(IExposeTestBase item);


        IExposeTestOther TestExposeTypeOther(IExposeTestOther other);


        IReadOnlyList<IExposeTestBase> GetExposedCollection();

        // test 2
        IExposeTest2Base ExposeDerivedInterfaceTest(IExposeTest2Base input);
        IExposeTest2Container ExposeDerivedInterfaceContainerTest(IExposeTest2Container input);



        // test 3
        IExposeTest3Base[] GetTest3BaseItems();

        IExposeTest3Other GetTest3OtherItem();


        // test 4: same concrete class transferred as base interface and as derived interface (no exposure registration)
        IExposeTest4Base GetTest4Base();

        IExposeTest4Sub GetTest4Sub();

        string SendTest4Base(IExposeTest4Base item);

        string SendTest4Sub(IExposeTest4Sub item);
    }
}
