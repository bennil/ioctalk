using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class DoubleItem : AbstractValueItem
    {
        public DoubleItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.Double)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                return reader.ReadDouble();
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                writer.WriteDouble((double)value);
            });
        }
    }
}
