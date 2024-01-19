using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    /// <summary>
    /// Type reference value
    /// </summary>
    public class TypeRefItem : AbstractValueItem
    {
        public TypeRefItem(string name, Func<object, object> getter, Action<object, object> setter)
            : base(name, getter, setter, ItemType.TypeRef)
        {

        }

        public override void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            if (value == null)
            {
                writer.WriteUInt8(ValueItem.NullValueIdent);
            }
            else
            {

                Type typeRef = (Type)value;

                IValueItem item = context.GetByType(typeRef);

                //bool typeMetaInfo = false;
                //if (item is ITypeStructure)
                //{
                //    ITypeStructure tStructure = (ITypeStructure)item;

                //    if (context.IsWriteTypeMetaInfoRequired(item.TypeId))
                //    {
                //        writer.WriteUInt8(ValueItem.TypeMetaInfo);

                //        typeMetaInfo = true;
                //    }
                //}

                //if (!typeMetaInfo)
                //{
                //    writer.WriteUInt8(ValueItem.SingleValueIdent);
                //}

                writer.WriteUInt8(ValueItem.SingleValueIdent);
                writer.WriteUInt32(item.TypeId);

                if (item is ComplexStructure cs
                    && cs.IsObjectType == false
                    && context.IsWriteTypeMetaInfoRequired(item.TypeId))
                {
                    // serialize type meta info
                    TypeMetaStructure.WriteObjectTypeMetaInfo(writer, (ITypeStructure)item, context, true);
                }
                else if (item is CollectionItems ci
                    && ci.ItemStructure is ComplexStructure csItem
                    && csItem.IsObjectType == false
                    && context.IsWriteTypeMetaInfoRequired(item.TypeId))
                {
                    TypeMetaStructure.WriteCollectionTypeMetaInfo(writer, ci, context, true);
                }
                else if (item is EnumItem ei
                    && context.IsWriteTypeMetaInfoRequired(item.TypeId))
                {
                    TypeMetaStructure.WriteEnumTypeMetaInfo(writer, ei);
                }
                else
                    writer.WriteUInt8(ValueItem.NullValueIdent);

            }
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.SingleValueIdent)
            {
                uint typeIdValue = reader.ReadUInt32();

                byte contentType2 = reader.ReadUInt8();

                IValueItem actualStructure = null;
                if (contentType2 == ValueItem.TypeMetaInfo)
                    actualStructure = TypeMetaStructure.ReadTypeMetaInfo(reader, typeIdValue, context, true);
                else if (contentType2 != ValueItem.NullValueIdent)
                    throw new InvalidOperationException($"Unexpected typeref content type 2 value: {contentType2}");

                if (actualStructure is null)
                    actualStructure = context.GetByTypeId(typeIdValue);

                Type runtimeType = ValueItem.GetRuntimeType(actualStructure);
                if (runtimeType != null)
                {
                    return runtimeType;
                }
                else
                {
                    throw new InvalidOperationException($"Runtime type for structure \"{actualStructure.Name}\" not found. Type ID: {actualStructure.TypeId}");
                }
            }
            else if (contentType == ValueItem.NullValueIdent)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected content type \"{contentType}\"!");
            }
        }
    }
}
