using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class DecimalItem : AbstractValueItem
    {
        public DecimalItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.Decimal)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                int i1 = reader.ReadInt32();
                int i2 = reader.ReadInt32();
                int i3 = reader.ReadInt32();
                int i4 = reader.ReadInt32();

                return new decimal(new int[] { i1, i2, i3, i4 });
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                decimal decVal = (decimal)value;

                int[] bits = Decimal.GetBits(decVal);

                writer.WriteInt32(bits[0]);
                writer.WriteInt32(bits[1]);
                writer.WriteInt32(bits[2]);
                writer.WriteInt32(bits[3]);
            });
        }
    }
}
