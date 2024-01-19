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
    }
}
