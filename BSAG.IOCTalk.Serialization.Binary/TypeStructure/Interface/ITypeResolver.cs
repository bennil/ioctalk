using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    /// <summary>
    /// Interface ITypeResolver
    /// </summary>
    public interface ITypeResolver : IUnknowTypeResolver
    {
        IValueItem GetByType(Type type);


        IValueItem GetByTypeId(uint typeId);


        /// <summary>
        /// Determines different target type structures.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="defaultInterfaceType">Default type of the interface.</param>
        /// <returns>Type.</returns>
        IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType);

        /// <summary>
        /// Determines whether [is write type meta information required] [the specified type identifier].
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns><c>true</c> if [is write type meta information required] [the specified type identifier]; otherwise, <c>false</c>.</returns>
        bool IsWriteTypeMetaInfoRequired(uint typeId);
    }
}
