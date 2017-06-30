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
    /// Class Int64Item.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.AbstractValueItem" />
    public class Int64Item : AbstractValueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Int64Item" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        public Int64Item(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.Int64)
        {
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
                return reader.ReadInt64();
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
                writer.WriteInt64((long)value);
            });
        }
    }
}
