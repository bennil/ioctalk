using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.Utils;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class EnumItem : AbstractValueItem, ITypePrefix
    {
        private Type enumType;
        private uint enumTypeId;

        public EnumItem(string name, Func<object, object> getter, Action<object, object> setter, Type enumType)
                    : base(name, getter, setter, ItemType.Enum)
        {
            this.enumType = enumType;
            this.enumTypeId = CalculateTypeId(enumType);
        }

        public override uint TypeId
        {
            get
            {
                return enumTypeId;
            }
        }

        public override bool IsTypePrefixExpected
        {
            get
            {
                return true;
            }
        }

        private uint CalculateTypeId(Type enumType)
        {
            uint typeCode = Hashing.CreateHash(enumType.FullName);

            if (typeCode <= 150)
            {
                // Reserved type IDs for ItemType enumeration
                typeCode += 151;
            }

            return typeCode;
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return ReadValue(reader, context, true);
        }

        public object ReadValue(IStreamReader reader, ISerializeContext context, bool isReadTypeIdExpected)
        {
            if (isReadTypeIdExpected)
            {
                uint actualTypeId = reader.ReadUInt32();

                if (actualTypeId != enumTypeId)
                {
                    throw new InvalidOperationException($"Unexptected enum type ID: {actualTypeId}; exptected ID: {enumTypeId}; exptected type: {enumType.FullName}");
                }
            }

            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                TypeMetaStructure.SkipTypeMetaInfo(reader);
                contentType = reader.ReadUInt8();

            }

            return NullableRead(reader, () =>
            {
                int intEnumVal = reader.ReadInt32();
                return Enum.ToObject(enumType, intEnumVal);
            });
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            // Enum starts with the actual type id
            writer.WriteUInt32(this.TypeId);

            if (context.IsWriteTypeMetaInfoRequired(this.TypeId))
            {
                // serialize type meta info at the first time
                TypeMetaStructure.WriteTypeMetaInfo(writer, this.enumType);
            }

            writer.WriteUInt8(ValueItem.SingleValueIdent);

            NullableWrite(writer, value, () =>
            {
                writer.WriteInt32((int)value);
            });
        }
    }
}
