using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    /// <summary>
    /// Class StringHashItem.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.AbstractValueItem" />
    public class StringHashItem : AbstractValueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringItem" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        public StringHashItem(string name, Func<object, object> getter, Action<object, object> setter)
            : base(name, getter, setter, ItemType.StringHash)
        {
            this.TypeFlags |= ItemTypeFlags.Nullable;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is type prefix exptected.
        /// </summary>
        /// <value><c>true</c> if this instance is type prefix exptected; otherwise, <c>false</c>.</value>
        public override bool IsTypePrefixExpected
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.Object.</returns>
        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.SingleValueIdent)
            {
                uint stringHashCode = reader.ReadUInt32();

                string stringResult = context.GetHashString(stringHashCode);

                if (stringResult != null)
                {
                    return stringResult;
                }
                else
                {
                    throw new InvalidOperationException($"No string for hashcode \"{stringHashCode}\" found!");
                }
            }
            else if (contentType == ValueItem.HashCodeString)
            {
                string stringValue = reader.ReadString();
                uint stringHashCode = reader.ReadUInt32();

                context.RegisterStringHashCodeValue(stringValue, stringHashCode);

                return stringValue;
            }
            else if (contentType == ValueItem.NullValueIdent)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected content type \"{contentType}\"!");
            }
        }

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            if (value == null)
            {
                writer.WriteUInt8(ValueItem.NullValueIdent);
            }
            else
            {

                string stringValue = (string)value;
                uint stringHashCode;
                if (context.IsWriteHashStringRequired(stringValue, out stringHashCode))
                {
                    writer.WriteUInt8(ValueItem.HashCodeString);
                    writer.WriteString(stringValue);
                }
                else
                {
                    writer.WriteUInt8(ValueItem.SingleValueIdent);
                }

                writer.WriteUInt32(stringHashCode);
            }
        }
    }
}
