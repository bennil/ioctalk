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
    }
}
