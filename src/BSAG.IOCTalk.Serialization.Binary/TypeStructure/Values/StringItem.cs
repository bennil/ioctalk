using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bond.IO.Safe;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    /// <summary>
    /// Class StringItem.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.AbstractValueItem" />
    public class StringItem : AbstractValueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringItem" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        public StringItem(string name, Func<object, object> getter, Action<object, object> setter)
            : base(name, getter, setter, ItemType.String)
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
            return NullableRead(reader, () =>
            {
                return reader.ReadString();
            });
        }

        /// <summary>
        /// Writes the value.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                string stringValue = (string)value;

                writer.WriteString(stringValue);
            });
        }
    }
}
