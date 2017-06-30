using System;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class DateTimeItem : AbstractValueItem
    {
        public DateTimeItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.DateTime)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                var ticks = reader.ReadInt64();

                return new DateTime(ticks);
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                DateTime dt = (DateTime)value;

                writer.WriteInt64(dt.Ticks);
            });
        }
    }
}
