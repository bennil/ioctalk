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



        public static IValueItem ReadContentTypeMetaInfo(IStreamReader reader, uint typeId, ISerializeContext ctx)
        {
            IValueItem structure;
            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                structure = ReadTypeMetaInfo(reader, typeId, ctx);
            }
            else
            {
                throw new TypeAccessException($"Type information for type id {typeId} not provided! Content Type: {contentType}");
            }

            return structure;
        }

        internal static IValueItem ReadTypeMetaInfo(IStreamReader reader, uint typeId, ISerializeContext ctx)
        {
            byte metaTypeVersion = reader.ReadUInt8();

            string assemblyName;
            string typeFullName;
            Type type;
            if (metaTypeVersion == SingleTypeDescr)
            {
                assemblyName = reader.ReadString();
                typeFullName = reader.ReadString();

                if (!TypeService.TryGetTypeByName(typeFullName, out type, ctx.Serializer.CustomLookupAssemblies))
                {
                    if (ctx.Serializer.AutoCreateMissingTypes)
                    {
                        return AutoCreateMissingDeserializeType(typeId, assemblyName, typeFullName, reader, ctx);
                    }
                    else
                    {
                        throw new TypeAccessException($"Type {typeFullName} not found!");
                    }
                }

                IValueItem resultItem = ctx.GetByType(type);

                if (resultItem is ITypeStructure)
                {
                    ITypeStructure result = (ITypeStructure)resultItem;

                    short itemCount = reader.ReadInt16();

                    if (result.TypeId == typeId)
                    {
                        // internal type is equal remote type
                        for (int i = 0; i < itemCount; i++)
                        {
                            SkipPropertyMetaInfo(reader);
                        }

                        return result;
                    }
                    else
                    {
                        // differences between local type and remote type

                        // read actual structure and try map to old/new structure
                        ComplexStructure tolerantLayoutStructure = ComplexStructure.CreateTolerantLayoutStructure(type, ctx);

                        string lastNoMatchPropertyInfo = null;
                        for (int i = 0; i < itemCount; i++)
                        {
                            ItemType itemType = (ItemType)reader.ReadInt16();         // ItemType Enum
                            uint propertyTypeId = reader.ReadUInt32();        // TypeId (Type Hash)
                            string propertyName = reader.ReadString();        // Property Name
                            bool isNullable = reader.ReadBool();          // Nullable Flag

                            IValueItem nameMatchingItem = result.Items.Where(item => item.Name == propertyName).FirstOrDefault();

                            if (nameMatchingItem != null)
                            {
                                bool typeMatch = itemType == nameMatchingItem.Type;
                                bool typeIdMatch = propertyTypeId == nameMatchingItem.TypeId;
                                bool nullableMatch = isNullable == nameMatchingItem.IsNullable;
                                bool fromNotNullable = !isNullable && nameMatchingItem.IsNullable;

                                if (typeMatch
                                    && typeIdMatch
                                    && nullableMatch)
                                {
                                    // Property has exactly the same attributes > no conversion required
                                    tolerantLayoutStructure.AddTolerantLayoutProperty(ctx, propertyName);
                                }
                                else if (typeMatch
                                          && typeIdMatch
                                          && !nullableMatch
                                          && fromNotNullable)
                                {
                                    // Property changed only from not nullable to nullable
                                    tolerantLayoutStructure.AddTolerantLayoutConverterProperty(nameMatchingItem, ctx, propertyName, itemType, propertyTypeId, isNullable, v => v);
                                }
                                else if (!typeMatch
                                    && !typeIdMatch
                                    && (nullableMatch || fromNotNullable))
                                {
                                    // Only property type changed
                                    // Check if type can be converted without loosing data
                                    Func<object, object> converter = null;
                                    if (itemType == ItemType.Int32
                                        && nameMatchingItem.Type == ItemType.Int64)
                                    {
                                        // Read incoming Int32 and convert to local Int64 property
                                        converter = v => (long)(int)v;
                                    }
                                    else if (itemType == ItemType.Int16
                                        && nameMatchingItem.Type == ItemType.Int32)
                                    {
                                        // Read incoming Int16 and convert to local Int32 property
                                        converter = v => (int)(short)v;
                                    }
                                    else if (itemType == ItemType.Byte
                                        && nameMatchingItem.Type == ItemType.Int16)
                                    {
                                        // Read incoming Byte and convert to local Int16 property
                                        converter = v => (short)(byte)v;
                                    }
                                    else if (itemType == ItemType.Double
                                        && nameMatchingItem.Type == ItemType.Decimal)
                                    {
                                        // Read incoming Double and convert to local Decimal property
                                        converter = v => (decimal)(double)v;
                                    }
                                    else if (itemType == ItemType.Char
                                        && nameMatchingItem.Type == ItemType.String)
                                    {
                                        // Read incoming Char and convert to local String property
                                        converter = v => new string(new char[] { (char)v });
                                    }
                                    else if (itemType == ItemType.String
                                        && nameMatchingItem.Type == ItemType.StringHash)
                                    {
                                        // Read incoming string as StringHash result
                                        converter = v => v?.ToString();
                                    }

                                    if (converter != null)
                                    {
                                        tolerantLayoutStructure.AddTolerantLayoutConverterProperty(nameMatchingItem, ctx, propertyName, itemType, propertyTypeId, isNullable, converter);
                                    }
                                    else
                                        lastNoMatchPropertyInfo = $"Last Property: {propertyName}; Type: {itemType} <> {nameMatchingItem.Type}; PropertyTypeId: {propertyTypeId} <> {nameMatchingItem.TypeId}";
                                }
                                else
                                    lastNoMatchPropertyInfo = $"Last Unknown Property: {propertyName}; Type: {itemType}; PropertyTypeId: {propertyTypeId}";
                            }
                            else
                            {
                                // no name matching property found (removed, renamed or old local version) > create dummy item
                                tolerantLayoutStructure.AddTolerantLayoutDummyProperty(ctx, propertyName, itemType, propertyTypeId, isNullable);
                            }
                        }

                        tolerantLayoutStructure.FinalizeTolerantLayoutStructure();
                        if (typeId == tolerantLayoutStructure.TypeId)
                        {
                            // Tolerant mapping was successful
                            // Register only type ID in global cache
                            BinarySerializer.RegisterTolerantTypeMapping(tolerantLayoutStructure);

                            return tolerantLayoutStructure;
                        }
                        else
                        {
                            // Can't map old/new structure to incoming type meta data
                            throw new FormatException($"The incoming binary format does not match the loaded type {type.FullName}! An tolerant mapping was not possible. Please update the remote or local interface assemblies. Expected Type Id: {result.TypeId}; Received Type Id: {typeId}; {lastNoMatchPropertyInfo}");
                        }
                    }
                }
                else if (resultItem is ITypePrefix)
                {
                    // Only type prefix - e.g. enum
                    ITypePrefix result = (ITypePrefix)resultItem;

                    short itemCount = reader.ReadInt16();

                    if (result.TypeId == typeId)
                    {
                        // internal type is equal remote type
                        for (int i = 0; i < itemCount; i++)
                        {
                            SkipPropertyMetaInfo(reader);
                        }

                        return result;
                    }
                    else
                    {
                        throw new FormatException($"The incoming binary format does not match the loaded type {type.FullName}! An tolerant mapping was not possible. Please update the remote or local interface assemblies. Expected Type Id: {result.TypeId}; Received Type Id: {typeId}");
                    }
                }
                else
                {
                    return resultItem;
                }
            }
            else if (metaTypeVersion == CollectionTypeDescr)
            {
                assemblyName = reader.ReadString();
                typeFullName = reader.ReadString();

                //todo: load assembly if required
                if (TypeService.TryGetTypeByName(typeFullName, out type))
                {
                    CollectionItems result = (CollectionItems)ctx.GetByType(type);

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
                    SkipPropertyMetaInfo(reader);
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

        private static void SkipPropertyMetaInfo(IStreamReader reader)
        {
            reader.SkipInt16();         // ItemType Enum
            reader.SkipUInt32();        // TypeId (Type Hash)
            reader.SkipString();        // Property Name
            reader.SkipBool();          // Nullable Flag
        }

        public static void WriteTypeMetaInfo(IStreamWriter writer, ITypeStructure typeItem, bool writeTypeMetaPrefix = true)
        {
            if (writeTypeMetaPrefix)
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

                writer.WriteInt16((short)memberItem.Type);  // ItemType Enum
                writer.WriteUInt32(memberItem.TypeId);      // TypeId (Type Hash)
                writer.WriteString(memberItem.Name);        // Property Name
                writer.WriteBool(memberItem.IsNullable);    // Nullable Flag
            }
        }

        /// <summary>
        /// Write enum type meta infos
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="type"></param>
        public static void WriteTypeMetaInfo(IStreamWriter writer, Type type)
        {
            writer.WriteUInt8(ValueItem.TypeMetaInfo);
            writer.WriteUInt8(SingleTypeDescr);


            writer.WriteString(type.Assembly.GetName().Name);

            writer.WriteString(type.FullName);

            // Write 0 items (only type specification in case of enum)
            writer.WriteInt16(0);
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


        #region Auto Create Type


        private static IValueItem AutoCreateMissingDeserializeType(uint expectedTypeId, string orgAssemblyName, string typeFullName, IStreamReader reader, ISerializeContext ctx)
        {
            //todo: restrict property count and global auto create types
            short propertyCount = reader.ReadInt16();

            string typeName = typeFullName.Split('.').Last();

            string assemblyName = $"IOCTalk.AutoGenerated.{orgAssemblyName}";

            StringBuilder code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine();
            code.Append("namespace ");
            code.Append(assemblyName);
            code.AppendLine();
            code.AppendLine("{");
            code.Append($"   public class {typeName}AutoGenerated");

            code.AppendLine();
            code.AppendLine("   {");
            code.AppendLine();

            for (int i = 0; i < propertyCount; i++)
            {
                ItemType itemType = (ItemType)reader.ReadInt16();         // ItemType Enum
                uint propertyTypeId = reader.ReadUInt32();        // TypeId (Type Hash)
                string propertyName = reader.ReadString();        // Property Name
                bool isNullable = reader.ReadBool();          // Nullable Flag

                // add property code
                Type propType = ValueItem.GetRuntimeType(itemType);

                if (propType == null)
                {
                    propType = typeof(object);
                }

                code.AppendLine($"      public {TypeService.GetSourceCodeTypeName(propType)} {propertyName} {{get; set;}}");
                code.AppendLine();
            }

            code.AppendLine();
            code.AppendLine("   }");
            code.AppendLine("}");

            var type = TypeService.ImplementDynamicType(code.ToString(), assemblyName);

            ComplexStructure autoCreateStructure = new ComplexStructure(type, null, null, null, ctx);
            autoCreateStructure.TypeId = expectedTypeId;    // reset type id because of object only properties

            // Register auto created type
            BinarySerializer.RegisterTolerantTypeMapping(autoCreateStructure);

            return autoCreateStructure;
        }

        #endregion
    }
}
