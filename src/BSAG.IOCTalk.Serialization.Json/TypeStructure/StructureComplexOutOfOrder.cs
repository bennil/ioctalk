using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Internal helper class to handle out of order deserialization structures
    /// </summary>
    internal class StructureComplexOutOfOrder
    {
        internal IJsonTypeStructure[] ObjectStructure { get; set; }

        internal Func<object, object>[] GetAccessorByPropertyIndex { get; set; }

        internal Action<object, object>[] SetAccessorByPropertyIndex { get; set; }

        public int MaxTargetIndex { get; set; }
    }
}
