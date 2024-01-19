using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.Utils;
using BSAG.IOCTalk.Common.Reflection;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public class EnumItem : AbstractValueItem, ITypePrefix
    {
        Type enumType;
        uint enumTypeId;
        Type underlyingEnumType;
        bool isDefaultUnderlyingType;
        IValueItem otherUnderlyingTypeEnum;

        public EnumItem(string name, Func<object, object> getter, Action<object, object> setter, Type enumType)
                    : base(name, getter, setter, ItemType.Enum)
        {
            this.enumType = enumType;
            this.enumTypeId = CalculateTypeId(enumType);

            this.underlyingEnumType = Enum.GetUnderlyingType(enumType);
            this.isDefaultUnderlyingType = underlyingEnumType.Equals(typeof(int));
            if (isDefaultUnderlyingType == false)
            {
                otherUnderlyingTypeEnum = ValueItem.CreateValueItem(null, underlyingEnumType, name, getter, setter, null);
            }
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

        public ItemType UnderlyingItemType => isDefaultUnderlyingType ? ItemType.Int32 : otherUnderlyingTypeEnum.Type;

        public Type EnumType => enumType;

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

                if (actualTypeId != enumTypeId
                    && context.Serializer.AutoCreateMissingTypes == false)
                {
                    throw new InvalidOperationException($"Unexptected enum type ID: {actualTypeId}; exptected ID: {enumTypeId}; exptected type: {enumType.FullName}");
                }
            }

            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                TypeMetaStructure.SkipTypeMetaInfo(reader, true);
                contentType = reader.ReadUInt8();
            }

            if (contentType == ValueItem.UnderlyingTypeId)
            {
                uint underylingTypeId = reader.ReadUInt32();
                if (isDefaultUnderlyingType)
                {
                    throw new NotImplementedException($"Support for different underlying enum type casting not yet supported! Underyling type ID: {underylingTypeId}");
                }
                else
                {
                    if (otherUnderlyingTypeEnum.TypeId != underylingTypeId)
                        throw new InvalidOperationException($"Unexpected underyling type ID! Expected: {otherUnderlyingTypeEnum.TypeId} {otherUnderlyingTypeEnum.Type}; Received: {underylingTypeId}");
                }
            }

            if (isDefaultUnderlyingType)
            {
                return NullableRead(reader, () =>
                {
                    int intEnumVal = reader.ReadInt32();
                    return Enum.ToObject(enumType, intEnumVal);
                });
            }
            else
            {
                return otherUnderlyingTypeEnum.ReadValue(reader, context);
            }
        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            // Enum starts with the actual type id
            writer.WriteUInt32(this.TypeId);

            if (context.IsWriteTypeMetaInfoRequired(this.TypeId))
            {
                // serialize type meta info at the first time
                TypeMetaStructure.WriteEnumTypeMetaInfo(writer, this);
            }

            if (isDefaultUnderlyingType)
            {
                writer.WriteUInt8(ValueItem.SingleValueIdent);

                NullableWrite(writer, value, () =>
                {
                    writer.WriteInt32((int)value);
                });
            }
            else
            {
                // write additional underlying type ID
                writer.WriteUInt8(ValueItem.UnderlyingTypeId);
                writer.WriteUInt32(this.otherUnderlyingTypeEnum.TypeId);


                otherUnderlyingTypeEnum.WriteValue(writer, context, value);
            }
        }
    }
}
