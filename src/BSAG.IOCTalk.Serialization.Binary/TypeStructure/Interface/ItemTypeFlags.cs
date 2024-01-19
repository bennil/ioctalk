using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    [Flags]
    public enum ItemTypeFlags : byte
    {
        None = 0,

        Nullable = 1,

        Collection = 2,

        Array = 4,
    }
}
