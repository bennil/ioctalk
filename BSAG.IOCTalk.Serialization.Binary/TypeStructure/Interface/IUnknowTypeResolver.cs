using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface IUnknowTypeResolver
    {
        /// <summary>
        /// Determines the concrete target type from the given interface type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns>Type.</returns>
        Type DetermineTargetType(Type interfaceType);

    }
}
