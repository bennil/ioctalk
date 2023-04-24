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
    /// Class Int32Item.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.AbstractValueItem" />
    public class UInt32Item : AbstractValueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Int32Item" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="getter">The getter.</param>
        /// <param name="setter">The setter.</param>
        public UInt32Item(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.UInt32)
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
                return reader.ReadUInt32();
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
                writer.WriteUInt32((uint)value);
            });
        }
    }
}
