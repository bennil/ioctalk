using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class TimeSpanItem : AbstractValueItem
    {
        public TimeSpanItem(string name, Func<object, object> getter, Action<object, object> setter)
            : base(name, getter, setter, ItemType.TimeSpan)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                long timeTicks = reader.ReadInt64();
                return TimeSpan.FromTicks(timeTicks);
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                TimeSpan time = (TimeSpan)value;
                writer.WriteInt64(time.Ticks);
            });
        }
    }
}
