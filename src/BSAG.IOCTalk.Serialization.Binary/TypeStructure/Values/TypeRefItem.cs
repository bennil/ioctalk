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
            // Write ItemType.TypeRef ID
            //writer.WriteUInt32(this.TypeId);

            if (value == null)
            {
                writer.WriteUInt8(ValueItem.NullValueIdent);
            }
            else
            {

                Type typeRef = (Type)value;

                IValueItem item = context.GetByType(typeRef);

                bool typeMetaInfo = false;
                if (item is ITypeStructure)
                {
                    ITypeStructure tStructure = (ITypeStructure)item;

                    if (context.IsWriteTypeMetaInfoRequired(item.TypeId))
                    {
                        writer.WriteUInt8(ValueItem.TypeMetaInfo);

                        typeMetaInfo = true;
                    }
                }

                if (!typeMetaInfo)
                {
                    writer.WriteUInt8(ValueItem.SingleValueIdent);
                }

                writer.WriteUInt32(item.TypeId);

                if (typeMetaInfo)
                {
                    // serialize type meta info
                    TypeMetaStructure.WriteTypeMetaInfo(writer, (ITypeStructure)item, false);
                }
            }
        }

        public override object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            byte contentType = reader.ReadUInt8();


            if (contentType == ValueItem.SingleValueIdent)
            {
                uint typeIdValue = reader.ReadUInt32();

                var actualStructure = context.GetByTypeId(typeIdValue);

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
            else if (contentType == ValueItem.TypeMetaInfo)
            {
                uint typeIdValue = reader.ReadUInt32();

                var actualStructure = TypeMetaStructure.ReadTypeMetaInfo(reader, typeIdValue, context);

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
