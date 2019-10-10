using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface IValueItem
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>The type.</value>
        ItemType Type { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is nullable.
        /// </summary>
        /// <value><c>true</c> if this instance is nullable; otherwise, <c>false</c>.</value>
        bool IsNullable { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is type prefix expected.
        /// </summary>
        /// <value><c>true</c> if this instance is type prefix expected; otherwise, <c>false</c>.</value>
        bool IsTypePrefixExpected { get; }

        /// <summary>
        /// Gets the type identifier (unique type hash code).
        /// </summary>
        /// <value>The type identifier.</value>
        uint TypeId { get; }

        /// <summary>
        /// Gets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Object.</returns>
        object GetItemValue(object instance);

        /// <summary>
        /// Sets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyValue">The property value.</param>
        void SetItemValue(object instance, object propertyValue);

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        void WriteValue(IStreamWriter writer, ISerializeContext context, object value);

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.Object.</returns>
        object ReadValue(IStreamReader reader, ISerializeContext context);


    }
}
