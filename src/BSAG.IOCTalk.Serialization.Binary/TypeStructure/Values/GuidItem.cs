using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class GuidItem : AbstractValueItem
    {
        public GuidItem(string name, Func<object, object> getter, Action<object, object> setter)
                    : base(name, getter, setter, ItemType.Guid)
        {
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                var guidBytes = reader.ReadBytes(16);
                Guid guid = new Guid(guidBytes.ToArray());
                return guid;
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                Guid guid = (Guid)value;
                var seg = new ArraySegment<byte>(guid.ToByteArray());
                writer.WriteBytes(seg);
            });
        }
    }
}
