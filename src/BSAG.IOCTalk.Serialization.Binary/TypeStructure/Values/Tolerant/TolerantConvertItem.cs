using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.Tolerant
{
    /// <summary>
    /// TolerantConvertItem used to tolerant read modified binary layouts
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface.IValueItem" />
    public class TolerantConvertItem : IValueItem
    {
        private Func<object, object> converter;

        /// <summary>
        /// The source type item
        /// </summary>
        private IValueItem sourceTypeItem;
        private IValueItem targetTypeItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TolerantConvertItem" /> class.
        /// </summary>
        /// <param name="sourceTypeItem">The source type item.</param>
        /// <param name="targetTypeItem">The target type item.</param>
        /// <param name="converter">The converter.</param>
        public TolerantConvertItem(IValueItem sourceTypeItem, IValueItem targetTypeItem, Func<object, object> converter)
        {
            this.sourceTypeItem = sourceTypeItem;
            this.targetTypeItem = targetTypeItem;
            this.converter = converter;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is nullable.
        /// </summary>
        /// <value><c>true</c> if this instance is nullable; otherwise, <c>false</c>.</value>
        public bool IsNullable { get; set; }


        /// <summary>
        /// Gets a value indicating whether this instance is type prefix expected.
        /// </summary>
        /// <value><c>true</c> if this instance is type prefix expected; otherwise, <c>false</c>.</value>
        public bool IsTypePrefixExpected { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }


        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>The type.</value>
        public ItemType Type { get; set; }


        /// <summary>
        /// Gets the type identifier (unique type hash code).
        /// </summary>
        /// <value>The type identifier.</value>
        public uint TypeId { get; set; }


        /// <summary>
        /// Gets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Object.</returns>
        public object GetItemValue(object instance)
        {
            return null;
        }

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="context">The context.</param>
        /// <returns>System.Object.</returns>
        public object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            object orgValue = sourceTypeItem.ReadValue(reader, context);
            return converter(orgValue);
        }

        /// <summary>
        /// Sets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyValue">The property value.</param>
        public void SetItemValue(object instance, object propertyValue)
        {
            targetTypeItem.SetItemValue(instance, propertyValue);
        }

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        public void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
        }
    }
}
