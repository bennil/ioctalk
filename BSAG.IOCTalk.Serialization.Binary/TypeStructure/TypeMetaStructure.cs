using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure
{
    public class TypeMetaStructure
    {
        /// <summary>
        /// Single object description
        /// </summary>
        public const byte SingleTypeDescr = 1;

        // todo: implement collection /array type meta info serialization
        public const byte CollectionTypeDescr = 2;



        public static IValueItem ReadContentTypeMetaInfo(IStreamReader reader, uint typeId, ITypeResolver typeResolver)
        {
            IValueItem structure;
            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                structure = ReadTypeMetaInfo(reader, typeId, typeResolver);
            }
            else
            {
                throw new TypeAccessException($"Type information for type id {typeId} not provided!");
            }

            return structure;
        }

        private static IValueItem ReadTypeMetaInfo(IStreamReader reader, uint typeId, ITypeResolver typeResolver)
        {
            byte metaTypeVersion = reader.ReadUInt8();

            string assemblyName;
            string typeFullName;
            Type type;
            if (metaTypeVersion == SingleTypeDescr)
            {
                assemblyName = reader.ReadString();
                typeFullName = reader.ReadString();

                //todo: load assembly if required
                if (TypeService.TryGetTypeByName(typeFullName, out type))
                {
                    ITypeStructure result = (ITypeStructure)typeResolver.GetByType(type);

                    short itemCount = reader.ReadInt16();

                    if (result.TypeId == typeId)
                    {
                        // internal type is equal remote type
                        for (int i = 0; i < itemCount; i++)
                        {
                            //var memberItem = result.Items[i];

                            reader.SkipInt16();
                            reader.SkipUInt32();
                            reader.SkipString();
                            reader.SkipBool();
                        }

                        return result;
                    }
                    else
                    {
                        // differences between local type and remote type
                        //todo: implement
                        throw new NotImplementedException($"Support for different binary layout is not implemented yet! Local Type ID: {result.TypeId}; Remote Type ID: {typeId}");
                    }
                }
                else
                {
                    throw new TypeAccessException($"Type {typeFullName} not found!");
                }
            }
            else if (metaTypeVersion == CollectionTypeDescr)
            {
                assemblyName = reader.ReadString();
                typeFullName = reader.ReadString();

                //todo: load assembly if required
                if (TypeService.TryGetTypeByName(typeFullName, out type))
                {
                    CollectionItems result = (CollectionItems)typeResolver.GetByType(type);

                    if (result.TypeId == typeId)
                    {
                        return result;
                    }
                    else
                    {
                        // differences between local type and remote type
                        //todo: implement
                        throw new NotImplementedException($"Support for different binary layout is not implemented yet! Local Type ID: {result.TypeId}; Remote Type ID: {typeId}");
                    }
                }
                else
                {
                    throw new TypeAccessException($"Type {typeFullName} not found!");
                }
            }
            else
            {
                throw new NotSupportedException($"Type meta information version {metaTypeVersion} not supported!");
            }
        }

        internal static void SkipTypeMetaInfo(IStreamReader reader)
        {
            byte metaTypeVersion = reader.ReadUInt8();

            if (metaTypeVersion == SingleTypeDescr)
            {
                reader.SkipString(); // skip assembly name
                reader.SkipString(); // skip type name

                short itemCount = reader.ReadInt16();

                for (int i = 0; i < itemCount; i++)
                {
                    reader.SkipInt16();
                    reader.SkipUInt32();
                    reader.SkipString();
                    reader.SkipBool();
                }
            }
            else if (metaTypeVersion == CollectionTypeDescr)
            {
                reader.SkipString(); // skip assembly name
                reader.SkipString(); // skip type name
            }
            else
            {
                throw new NotSupportedException($"Type meta information version {metaTypeVersion} not supported!");
            }
        }

        public static void WriteTypeMetaInfo(IStreamWriter writer, ITypeStructure typeItem)
        {
            writer.WriteUInt8(ValueItem.TypeMetaInfo);
            writer.WriteUInt8(SingleTypeDescr);

            Type type = typeItem.RuntimeType;

            writer.WriteString(type.Assembly.GetName().Name);

            writer.WriteString(type.FullName);

            short itemCount = (short)typeItem.Items.Count;
            writer.WriteInt16(itemCount);


            for (int i = 0; i < itemCount; i++)
            {
                var memberItem = typeItem.Items[i];

                writer.WriteInt16((short)memberItem.Type);
                writer.WriteUInt32(memberItem.TypeId);
                writer.WriteString(memberItem.Name);
                writer.WriteBool(memberItem.IsNullable);
            }
        }

        public static void WriteTypeMetaInfo(IStreamWriter writer, CollectionItems collection)
        {
            writer.WriteUInt8(ValueItem.TypeMetaInfo);
            writer.WriteUInt8(CollectionTypeDescr);

            Type type = collection.RuntimeType;

            writer.WriteString(type.Assembly.GetName().Name);

            writer.WriteString(type.FullName);

            //writer.WriteBool(collection.IsArray);

            //if (collection.IsArray)
            //{

            //}
        }
    }
}
