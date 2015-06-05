using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// JSON type structure interface
    /// </summary>
    public interface IJsonTypeStructure
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="context">The context.</param>
        void Serialize(StringBuilder sb, object obj, SerializationContext context);

        /// <summary>
        /// Deserializes the specified json string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        object Deserialize(string json, ref int currentReadIndex, SerializationContext context);
    }
}
