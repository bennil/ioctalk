using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure
{
    public interface ISerializeItem
    {
        void SerializeItem(IProtocolWriter writer, object value);
    }
}
