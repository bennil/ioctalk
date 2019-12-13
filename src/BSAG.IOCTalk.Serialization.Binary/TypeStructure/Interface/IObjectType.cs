using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface IObjectType : IValueItem
    {
        /// <summary>
        /// Gets the runtime type.
        /// </summary>
        /// <value>The type of the runtime.</value>
        Type RuntimeType { get; }
    }
}
