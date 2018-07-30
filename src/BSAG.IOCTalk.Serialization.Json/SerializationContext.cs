using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Json
{
    /// <summary>
    /// JSON serialization context
    /// </summary>
    public class SerializationContext
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the external context object.
        /// </summary>
        /// <value>
        /// The context object.
        /// </value>
        public object ExternalContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is deserialize.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deserialize; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeserialize { get; set; }

        /// <summary>
        /// Gets or sets the parent serialization object.
        /// </summary>
        /// <value>
        /// The parent object.
        /// </value>
        public object ParentObject { get; set; }

        /// <summary>
        /// Gets or sets the json string.
        /// </summary>
        /// <value>
        /// The json string.
        /// </value>
        public string JsonString { get; set; }

        /// <summary>
        /// Gets or sets the start index of the value.
        /// </summary>
        /// <value>
        /// The start index of the value.
        /// </value>
        public int ValueStartIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the array.
        /// </summary>
        /// <value>
        /// The index of the array.
        /// </value>
        public int? ArrayIndex { get; set; }


        /// <summary>
        /// Gets or sets the type of the interface.
        /// </summary>
        /// <value>
        /// The type of the interface.
        /// </value>
        public Type InterfaceType { get; set; }

        /// <summary>
        /// Gets or sets the serializer.
        /// </summary>
        /// <value>
        /// The serializer.
        /// </value>
        public JsonObjectSerializer Serializer { get; set; }

        /// <summary>
        /// Gets or sets the unknow type resolver.
        /// </summary>
        /// <value>
        /// The unknow type resolver.
        /// </value>
        public UnknownTypeResolver UnknowTypeResolver { get; set; }

        /// <summary>
        /// Gets or sets the special type resolver.
        /// </summary>
        /// <value>
        /// The special type resolver.
        /// </value>
        public SpecialTypeResolver SpecialTypeResolver { get; set; }
    }
}
