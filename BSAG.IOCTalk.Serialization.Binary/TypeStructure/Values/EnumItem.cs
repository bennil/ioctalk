using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class EnumItem : AbstractValueItem
    {
        private Type enumType;

        public EnumItem(string name, Func<object, object> getter, Action<object, object> setter, Type enumType)
                    : base(name, getter, setter, ItemType.Enum)
        {
            this.enumType = enumType;
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return NullableRead(reader, () =>
            {
                int intEnumVal = reader.ReadInt32();
                return Enum.ToObject(enumType, intEnumVal);
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            NullableWrite(writer, value, () =>
            {
                writer.WriteInt32((int)value);
            });
        }
    }
}
