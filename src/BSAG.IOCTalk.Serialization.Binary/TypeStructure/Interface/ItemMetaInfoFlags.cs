using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    [Flags]
    public enum ItemMetaInfoFlags : byte
    {
        None = 0,
              
        /// <summary>
        /// Type meta info appended after item type data
        /// </summary>
        TypeInfo = 1,

        CollectionTypeInfo = 2,
    }
}
