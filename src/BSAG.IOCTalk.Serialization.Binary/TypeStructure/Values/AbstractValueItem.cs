using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bond.IO.Safe;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    /// <summary>
    /// Class AbstractValueItem.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface.IValueItem" />
    public abstract class AbstractValueItem : IValueItem
    {
        /// <summary>
        /// The get property delegate
        /// </summary>
        protected Func<object, object> getProperty;

        /// <summary>
        /// The set property delegate
        /// </summary>
        protected Action<object, object> setProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractValueItem"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        /// <param name="itemType">Type of the item.</param>
        public AbstractValueItem(string name, Func<object, object> getter, Action<object, object> setter, ItemType itemType)
        {
            this.Name = name;
            this.Type = itemType;
            this.getProperty = getter;
            this.setProperty = setter;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>The type.</value>
        public ItemType Type { get; protected set; }

        /// <summary>
        /// Gets the additional item type flags.
        /// </summary>
        /// <value>The type.</value>
        public ItemTypeFlags TypeFlags { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is nullable.
        /// </summary>
        /// <value><c>true</c> if this instance is nullable; otherwise, <c>false</c>.</value>
        public bool IsNullable => TypeFlags.HasFlag(ItemTypeFlags.Nullable);

        /// <summary>
        /// Gets the type identifier (unique type hash code).
        /// </summary>
        /// <value>The type identifier.</value>
        public virtual uint TypeId
        {
            get
            {
                return (uint)Type;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is type prefix exptected.
        /// </summary>
        /// <value><c>true</c> if this instance is type prefix exptected; otherwise, <c>false</c>.</value>
        public virtual bool IsTypePrefixExpected
        {
            get
            {
                return IsNullable;
            }
        }


        /// <summary>
        /// Gets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Object.</returns>
        public object GetItemValue(object instance)
        {
            return getProperty(instance);
        }

        /// <summary>
        /// Sets the item value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyValue">The property value.</param>
        public void SetItemValue(object instance, object propertyValue)
        {
            setProperty(instance, propertyValue);
        }

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="context">The context.</param>
        /// <returns>System.Object.</returns>
        public abstract object ReadValue(IStreamReader reader, ISerializeContext context);

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        public abstract void WriteValue(IStreamWriter writer, ISerializeContext context, object value);


        protected object NullableRead(IStreamReader reader, Func<object> readFunc)
        {
            if (IsNullable)
            {
                byte contentType = reader.ReadUInt8();
                switch (contentType)
                {
                    case ValueItem.SingleValueIdent:
                        return readFunc();

                    case ValueItem.NullValueIdent:
                        return null;

                    default:
                        throw new InvalidOperationException($"Type ident {contentType} not expected!");
                }
            }
            else
            {
                return readFunc();
            }
        }

        protected void NullableWrite(IStreamWriter writer, object value, Action writeFunc)
        {
            if (IsNullable)
            {
                if (value == null)
                {
                    writer.WriteUInt8(ValueItem.NullValueIdent);
                }
                else
                {
                    writer.WriteUInt8(ValueItem.SingleValueIdent);
                    writeFunc();
                }
            }
            else
            {
                writeFunc();
            }
        }
    }
}
