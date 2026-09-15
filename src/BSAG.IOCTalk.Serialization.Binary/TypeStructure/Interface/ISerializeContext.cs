using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface ISerializeContext : ITypeResolver
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        string Key { get; set; }

        /// <summary>
        /// Gets or sets the external context object.
        /// </summary>
        /// <value>
        /// The context object.
        /// </value>
        object ExternalContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is deserialize.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deserialize; otherwise, <c>false</c>.
        /// </value>
        bool IsDeserialize { get; set; }

        /// <summary>
        /// Gets or sets the parent serialization object.
        /// </summary>
        /// <value>
        /// The parent object.
        /// </value>
        object ParentObject { get; set; }

        /// <summary>
        /// Gets or sets the parent level 2 serialization object.
        /// </summary>
        object ParentParentObject { get; set; }

        /// <summary>
        /// Gets or sets the child level
        /// </summary>
        int ChildLevel { get; set; }

        /// <summary>
        /// Gets or sets the index of the array.
        /// </summary>
        /// <value>The index of the array.</value>
        int? ArrayIndex { get; set; }

        
        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <value>The serializer.</value>
        BinarySerializer Serializer { get; }

        /// <summary>
        /// Determines the type of the target.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns>Type.</returns>
        Type DetermineTargetType(Type interfaceType);


        /// <summary>
        /// Tries to get a cached different target structure for the given runtime object type and the expected (contextual or declared) type.
        /// </summary>
        /// <param name="objectType">Runtime type of the object to serialize</param>
        /// <param name="expectedType">Expected type of the current slot (method parameter / return type if known, otherwise the declared type)</param>
        /// <param name="targetItem">Cached structure or null (negative cache entry)</param>
        bool TryGetDifferentTargetType(Type objectType, Type expectedType, out IValueItem targetItem);

        /// <summary>
        /// Registers a different target structure for the given runtime object type and expected type.
        /// </summary>
        /// <param name="objectType">Runtime type of the object to serialize</param>
        /// <param name="expectedType">Expected type of the current slot (cache key part)</param>
        /// <param name="defaultInterfaceType">Declared type of the current structure slot</param>
        /// <param name="diffType">Resolved target type (null = no different type)</param>
        /// <param name="cacheDifference">Cache the result for the given (objectType, expectedType) key</param>
        IValueItem RegisterDifferentTargetType(Type objectType, Type expectedType, Type defaultInterfaceType, Type diffType, bool cacheDifference);


        //IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType);


        string GetHashString(uint hashCode);

        void RegisterStringHashCodeValue(string stringValue, uint stringHashCode);
        
        void RegisterStringHashProperty(Type type, string propertyName);

        bool IsWriteHashStringRequired(string stringValue, out uint stringHashCode);

        bool IsStringHashProperty(Type type, string propertyName);
    }
}
