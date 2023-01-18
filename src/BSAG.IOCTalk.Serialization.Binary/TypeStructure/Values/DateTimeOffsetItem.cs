using System;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class DateTimeOffsetItem : AbstractValueItem
    {
        public DateTimeOffsetItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.DateTimeOffset)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                var ticks = reader.ReadInt64();
                var offsetTicks = reader.ReadInt64();
                TimeSpan offset = TimeSpan.FromTicks(offsetTicks);

                return new DateTimeOffset(ticks, offset);
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                DateTimeOffset dt = (DateTimeOffset)value;

                writer.WriteInt64(dt.Ticks);
                writer.WriteInt64(dt.Offset.Ticks);
            });
        }
    }
}
