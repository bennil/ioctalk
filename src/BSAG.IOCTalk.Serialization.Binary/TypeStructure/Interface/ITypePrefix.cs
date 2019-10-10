using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface ITypePrefix : IValueItem
    {
        object ReadValue(IStreamReader reader, ISerializeContext context, bool isReadTypeIdExpected);
    }
}
