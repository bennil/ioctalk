using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class BoolItem : AbstractValueItem
    {
        public BoolItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.Bool)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () => reader.ReadBool());
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                writer.WriteBool((bool)value);
            });
        }
    }
}
