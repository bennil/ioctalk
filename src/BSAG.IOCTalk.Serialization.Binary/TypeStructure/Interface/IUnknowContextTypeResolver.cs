using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface IUnknowContextTypeResolver
    {
        /// <summary>
        /// Determines the concrete target type from the given interface type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="context">The context.</param>
        /// <returns>Type.</returns>
        Type DetermineTargetType(Type interfaceType, ISerializeContext context);

        /// <summary>
        /// Determines the type of the special interface.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="defaultInterfaceType">Default type of the interface.</param>
        /// <param name="ctx">The CTX.</param>
        /// <returns>IValueItem.</returns>
        IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType, ISerializeContext ctx);
    }
}
