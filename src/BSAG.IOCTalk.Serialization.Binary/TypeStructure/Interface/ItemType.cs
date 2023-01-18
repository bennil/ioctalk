using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public enum ItemType : short
    {
        Undefined = 0,

        ComplexObject = 1,

        String = 2,

        Int32 = 3,

        Bool = 4,

        Enum = 5,

        DateTime = 6,

        TimeSpan = 7,

        Char = 8,

        Guid = 9,

        Double = 10,

        Decimal = 11,

        Int64 = 12,

        Int16 = 13,

        Byte = 14,

        TypeRef = 15,

        StringHash = 16,

        DateTimeOffset = 17,
    }
}
