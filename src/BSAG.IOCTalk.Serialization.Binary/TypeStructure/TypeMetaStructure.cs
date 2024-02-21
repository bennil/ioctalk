using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values.Tolerant;
using BSAG.IOCTalk.Serialization.Binary.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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

        public const byte CollectionTypeDescr = 2;

        public const byte EnumTypeDescr = 3;


        static int autoCreateMissingObjectTypeCount = 0;

        public static IValueItem ReadContentTypeMetaInfo(IStreamReader reader, uint typeId, ISerializeContext ctx)
        {
            IValueItem structure;
            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                structure = ReadTypeMetaInfo(reader, typeId, ctx, true);
            }
            else
            {
                throw new TypeAccessException($"Type information for type id {typeId} not provided! Content Type: {contentType}");
            }

            return structure;
        }

        internal static IValueItem ReadTypeMetaInfo(IStreamReader reader, uint typeId, ISerializeContext ctx, bool readMetaInfoSize)
        {
            (ushort MetaInfoSize, long MetaInfoStartPosition) headerInfo = default;
            if (readMetaInfoSize)
            {
                headerInfo = ReadTypeMetaInfoHeaderStart(reader);
            }

            try
            {

                byte metaTypeVersion = reader.ReadUInt8();

                if (metaTypeVersion == SingleTypeDescr)
                {
                    return ReadSingleObjectTypeMetaInfo(reader, typeId, ctx);
                }
                else if (metaTypeVersion == CollectionTypeDescr)
                {
                    return ReadCollectionMetaTypeInfo(reader, typeId, ctx);
                }
                else if (metaTypeVersion == EnumTypeDescr)
                {
                    return ReadEnumTypeMetaInfo(reader, ctx);
                }
                else
                {
                    // different version - todo: log warning
                    //throw new NotSupportedException($"Type meta information version {metaTypeVersion} not supported! Type ID: {typeId}");
                    return null;
                }

            }
            finally
            {
                if (readMetaInfoSize)
                    ReadTypeMetaInfoEnd(reader, headerInfo);
            }
        }


        static long WriteTypeMetaInfoHeaderStart(IStreamWriter writer)
        {
            writer.WriteUInt8(ValueItem.TypeMetaInfo);
            long metaInfoLengthPosition = writer.Position;
            writer.Position += 2;   // advance 16-bit as length placeholder

            return metaInfoLengthPosition;
        }

        static void WriteTypeMetaInfoHeaderEnd(IStreamWriter writer, long metaInfoLengthWriterPosition)
        {
            long currentPosition = writer.Position;
            long sizeLng = currentPosition - metaInfoLengthWriterPosition - 2;      // - 16 bit length var
            ushort size = (ushort)sizeLng;

            // jump to header length position
            writer.Position = metaInfoLengthWriterPosition;
            writer.WriteUInt16(size);   // write meta info length

            // jump back to current position
            writer.Position = currentPosition;
        }

        /// <summary>
        /// expected: TypeMetaInfo UInt8 already read
        /// </summary>
        /// <param name="reader"></param>
        static (ushort MetaInfoSize, long MetaInfoStartPosition) ReadTypeMetaInfoHeaderStart(IStreamReader reader)
        {
            ushort metaInfoSize = reader.ReadUInt16();
            long metaInfoStartPosition = reader.Position;

            return (metaInfoSize, metaInfoStartPosition);
        }

        static void ReadTypeMetaInfoEnd(IStreamReader reader, (ushort MetaInfoSize, long MetaInfoStartPosition) headerInfo)
        {
            long currentPosition = reader.Position;
            long readSize = currentPosition - headerInfo.MetaInfoStartPosition;

            if (readSize == headerInfo.MetaInfoSize)
            {
                // header size and actual read size are the same
            }
            else
            {
                // meta info size changed
                int sizeDifference = headerInfo.MetaInfoSize - (int)readSize;

                // adjust position to support future extended meta informations (ignore different meta info versions as well)
                reader.SkipBytes(sizeDifference);
            }
        }

        #region Object type meta info

        private static IValueItem ReadSingleObjectTypeMetaInfo(IStreamReader reader, uint typeId, ISerializeContext ctx)
        {
            Type type;
            string assemblyName = reader.ReadString();
            string typeFullName = reader.ReadString();

            if (ctx.Serializer.ForceAutoCreateMissingTypes
                && ctx.Serializer.AutoCreateMissingTypes)
            {
                return AutoCreateMissingDeserializeType(typeId, assemblyName, typeFullName, reader, ctx);
            }
            else if (TypeService.TryGetTypeByName(typeFullName, out type, ctx.Serializer.CustomLookupAssemblies) == false)
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
                        var readResult = ReadPropertyItem(reader, ctx);

                        IValueItem receivedStructure = null;
                        if (readResult.MetaInfoFlags.HasFlag(ItemMetaInfoFlags.TypeInfo) || readResult.MetaInfoFlags.HasFlag(ItemMetaInfoFlags.CollectionTypeInfo))
                        {
                            receivedStructure = ReadTypeMetaInfo(reader, readResult.PropertyTypeId, ctx, false);
                        }

                        IValueItem nameMatchingItem = result.Items.Where(item => item.Name == readResult.PropertyName).FirstOrDefault();

                        if (nameMatchingItem != null)
                        {
                            bool typeMatch = readResult.ItemType == nameMatchingItem.Type;
                            bool typeIdMatch = readResult.PropertyTypeId == nameMatchingItem.TypeId;
                            bool nullableMatch = readResult.TypeFlags.HasFlag(ItemTypeFlags.Nullable) == nameMatchingItem.TypeFlags.HasFlag(ItemTypeFlags.Nullable);
                            bool fromNotNullable = !readResult.TypeFlags.HasFlag(ItemTypeFlags.Nullable) && nameMatchingItem.TypeFlags.HasFlag(ItemTypeFlags.Nullable); ;

                            if (typeMatch
                                && typeIdMatch
                                && nullableMatch)
                            {
                                // Property has exactly the same attributes > no conversion required
                                tolerantLayoutStructure.AddTolerantLayoutProperty(ctx, readResult.PropertyName);
                            }
                            else if (typeMatch
                                      && typeIdMatch
                                      && !nullableMatch
                                      && fromNotNullable)
                            {
                                // Property changed only from not nullable to nullable
                                tolerantLayoutStructure.AddTolerantLayoutConverterProperty(nameMatchingItem, ctx, readResult.PropertyName, readResult.ItemType, readResult.PropertyTypeId, readResult.TypeFlags, v => v);
                            }
                            else if (!typeMatch
                                && !typeIdMatch
                                && (nullableMatch || fromNotNullable))
                            {
                                // Only property type changed
                                // Check if type can be converted without loosing data
                                Func<object, object> converter = null;
                                if (readResult.ItemType == ItemType.Int32
                                    && nameMatchingItem.Type == ItemType.Int64)
                                {
                                    // Read incoming Int32 and convert to local Int64 property
                                    converter = v => (long)(int)v;
                                }
                                else if (readResult.ItemType == ItemType.Int16
                                    && nameMatchingItem.Type == ItemType.Int32)
                                {
                                    // Read incoming Int16 and convert to local Int32 property
                                    converter = v => (int)(short)v;
                                }
                                else if (readResult.ItemType == ItemType.Byte
                                    && nameMatchingItem.Type == ItemType.Int16)
                                {
                                    // Read incoming Byte and convert to local Int16 property
                                    converter = v => (short)(byte)v;
                                }
                                else if (readResult.ItemType == ItemType.Double
                                    && nameMatchingItem.Type == ItemType.Decimal)
                                {
                                    // Read incoming Double and convert to local Decimal property
                                    converter = v => (decimal)(double)v;
                                }
                                else if (readResult.ItemType == ItemType.Char
                                    && nameMatchingItem.Type == ItemType.String)
                                {
                                    // Read incoming Char and convert to local String property
                                    converter = v => new string(new char[] { (char)v });
                                }
                                else if (readResult.ItemType == ItemType.String
                                    && nameMatchingItem.Type == ItemType.StringHash)
                                {
                                    // Read incoming string as StringHash result
                                    converter = v => v?.ToString();
                                }

                                if (converter != null)
                                {
                                    tolerantLayoutStructure.AddTolerantLayoutConverterProperty(nameMatchingItem, ctx, readResult.PropertyName, readResult.ItemType, readResult.PropertyTypeId, readResult.TypeFlags, converter);
                                }
                                else
                                    lastNoMatchPropertyInfo = $"Last Property: {readResult.PropertyName}; Type: {readResult.ItemType} <> {nameMatchingItem.Type}; PropertyTypeId: {readResult.PropertyTypeId} <> {nameMatchingItem.TypeId}";
                            }
                            else
                                lastNoMatchPropertyInfo = $"Last Unknown Property: {readResult.PropertyName}; Type: {readResult.ItemType}; PropertyTypeId: {readResult.PropertyTypeId}";
                        }
                        else
                        {
                            // no name matching property found (removed, renamed or old local version) > create dummy item
                            tolerantLayoutStructure.AddTolerantLayoutDummyProperty(ctx, readResult.PropertyName, readResult.ItemType, readResult.PropertyTypeId, readResult.TypeFlags);
                        }
                    }

                    tolerantLayoutStructure.FinalizeTolerantLayoutStructure();
                    if (typeId == tolerantLayoutStructure.TypeId)
                    {
                        // Tolerant mapping was successful
                        // Register only type ID in global cache
                        ctx.Serializer.RegisterTolerantTypeMapping(tolerantLayoutStructure);

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

        private static void SkipSingleObjectMetaTypeInfo(IStreamReader reader)
        {
            reader.SkipString(); // skip assembly name
            reader.SkipString(); // skip type name

            SkipTypeMetaInfoOnlyProperties(reader);
        }

        private static void SkipTypeMetaInfoOnlyProperties(IStreamReader reader)
        {
            short itemCount = reader.ReadInt16();

            for (int i = 0; i < itemCount; i++)
            {
                SkipPropertyMetaInfo(reader);
            }
        }


        private static (ItemType ItemType, uint PropertyTypeId, string PropertyName, ItemTypeFlags TypeFlags, ItemMetaInfoFlags MetaInfoFlags) ReadPropertyItem(IStreamReader reader, ISerializeContext ctx)
        {
            var itemType = (ItemType)reader.ReadInt16();            // ItemType Enum
            var propertyTypeId = reader.ReadUInt32();               // TypeId (Type Hash)
            var propertyName = reader.ReadString();                 // Property Name
            var typeFlags = (ItemTypeFlags)reader.ReadUInt8();      // Type flags
            var metaInfo = (ItemMetaInfoFlags)reader.ReadUInt8();   // Meta info flags

            return (itemType, propertyTypeId, propertyName, typeFlags, metaInfo);
        }

        private static void SkipPropertyMetaInfo(IStreamReader reader)
        {
            reader.SkipInt16();         // ItemType Enum 
            reader.SkipUInt32();        // TypeId (Type Hash)
            reader.SkipString();        // Property Name
            reader.SkipUInt8();         // Type flags
            var metaInfoFlags = (ItemMetaInfoFlags)reader.ReadUInt8();         // Meta Info flags
            if (metaInfoFlags.HasFlag(ItemMetaInfoFlags.TypeInfo) || metaInfoFlags.HasFlag(ItemMetaInfoFlags.CollectionTypeInfo))
                SkipTypeMetaInfo(reader, false);
        }

        internal static void WriteObjectTypeMetaInfo(IStreamWriter writer, ITypeStructure typeItem, ISerializeContext ctx, bool writeTypeMetaPrefix)
        {
            long metaInfoLengthWriterPos = -1;
            if (writeTypeMetaPrefix)
                metaInfoLengthWriterPos = WriteTypeMetaInfoHeaderStart(writer);

            writer.WriteUInt8(SingleTypeDescr);

            Type type = typeItem.RuntimeType;

            writer.WriteString(type.Assembly.GetName().Name);

            writer.WriteString(type.FullName);

            short itemCount = (short)typeItem.Items.Count;
            writer.WriteInt16(itemCount);


            for (int i = 0; i < itemCount; i++)
            {
                var memberItem = typeItem.Items[i];

                writer.WriteInt16((short)memberItem.Type);      // ItemType Enum
                writer.WriteUInt32(memberItem.TypeId);          // TypeId (Type Hash)
                writer.WriteString(memberItem.Name);            // Property Name
                writer.WriteUInt8((byte)memberItem.TypeFlags);  // ItemType flags

                ItemMetaInfoFlags metaInfoFlags = ItemMetaInfoFlags.None;
                if (memberItem is ComplexStructure cs
                    && cs.IsObjectType == false
                    && ctx.IsWriteTypeMetaInfoRequired(memberItem.TypeId))
                {
                    metaInfoFlags |= ItemMetaInfoFlags.TypeInfo;
                }
                else if (memberItem is CollectionItems ci
                    && ci.ItemStructure is ComplexStructure csItem
                    && csItem.IsObjectType == false
                    && ctx.IsWriteTypeMetaInfoRequired(memberItem.TypeId))
                {
                    metaInfoFlags |= ItemMetaInfoFlags.CollectionTypeInfo;
                }

                writer.WriteUInt8((byte)metaInfoFlags);         // Meta Info flags

                if (metaInfoFlags.HasFlag(ItemMetaInfoFlags.TypeInfo))
                    WriteObjectTypeMetaInfo(writer, (ITypeStructure)memberItem, ctx, false);
                else if (metaInfoFlags.HasFlag(ItemMetaInfoFlags.CollectionTypeInfo))
                    WriteCollectionTypeMetaInfo(writer, (CollectionItems)memberItem, ctx, false);
            }

            if (writeTypeMetaPrefix)
                WriteTypeMetaInfoHeaderEnd(writer, metaInfoLengthWriterPos);
        }

        #endregion

        internal static void SkipTypeMetaInfo(IStreamReader reader, bool readSizeHeader)
        {
            if (readSizeHeader)
            {
                // read header size and skip
                var headerInfo = ReadTypeMetaInfoHeaderStart(reader);
                reader.SkipBytes(headerInfo.MetaInfoSize);
            }
            else
            {
                byte metaTypeVersion = reader.ReadUInt8();

                if (metaTypeVersion == SingleTypeDescr)
                {
                    SkipSingleObjectMetaTypeInfo(reader);
                }
                else if (metaTypeVersion == CollectionTypeDescr)
                {
                    SkipCollectionTypeMetaInfo(reader);
                }
                else if (metaTypeVersion == EnumTypeDescr)
                {
                    SkipEnumTypeInfo(reader);
                }
                else
                {
                    // different future version - todo: warn log
                    //throw new NotSupportedException($"Type meta information version {metaTypeVersion} not supported!");
                }
            }
        }




        #region Enum type info

        /// <summary>
        /// Write enum type meta infos
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="type"></param>
        internal static void WriteEnumTypeMetaInfo(IStreamWriter writer, EnumItem source)
        {
            if (source.EnumType.IsEnum == false)
                throw new InvalidOperationException($"Given type must be an enum! Received: {source.EnumType.FullName}");

            long metaInfoLengthPosition = WriteTypeMetaInfoHeaderStart(writer);
            writer.WriteUInt8(EnumTypeDescr);

            writer.WriteString(source.EnumType.Assembly.GetName().Name);
            writer.WriteString(source.EnumType.FullName);

            writer.WriteInt16((short)source.UnderlyingItemType);

            WriteTypeMetaInfoHeaderEnd(writer, metaInfoLengthPosition);
        }

        internal static IValueItem ReadEnumTypeMetaInfo(IStreamReader reader, ISerializeContext ctx)
        {
            // expected: EnumTypeDescr already read
            string assemblyName = reader.ReadString();
            string enumTypeFullName = reader.ReadString();

            ItemType underlyingItemType = (ItemType)reader.ReadInt16();

            if (underlyingItemType == ItemType.Int32)
            {
                if (TypeService.TryGetTypeByName(enumTypeFullName, out Type type))
                {
                    return ctx.GetByType(type);
                }
                else if (ctx.Serializer.AutoCreateMissingTypes)
                {
                    var fallbackItem = ctx.GetByType(typeof(int));  // fallback to int
                    return fallbackItem;
                }
                else
                    throw new TypeAccessException($"Enum type {enumTypeFullName} not found!");
            }
            else
                throw new InvalidOperationException($"Non default enum types not implemented yet! Underlying type: {underlyingItemType}");
        }

        internal static void SkipEnumTypeInfo(IStreamReader reader)
        {
            // expected: EnumTypeDescr already read
            reader.SkipString();
            reader.SkipString();
            reader.SkipInt16();
        }


        #endregion


        #region Collection type info

        internal static void WriteCollectionTypeMetaInfo(IStreamWriter writer, CollectionItems collection, ISerializeContext ctx, bool writeTypeMetaPrefix)
        {
            long metaInfoLengthPosition = -1;
            if (writeTypeMetaPrefix)
            {
                metaInfoLengthPosition = WriteTypeMetaInfoHeaderStart(writer);
            }
            writer.WriteUInt8(CollectionTypeDescr);

            Type type = collection.RuntimeType;

            writer.WriteString(type.Assembly.GetName().Name);
            writer.WriteString(type.FullName);

            writer.WriteUInt8((byte)collection.TypeFlags);
            writer.WriteUInt32(collection.ItemStructure.TypeId);    // collection item type ID

            if (collection.ItemStructure is ComplexStructure cs
                && cs.IsObjectType == false)
            {
                writer.WriteBool(true);                 // type meta info appended flag
                WriteObjectTypeMetaInfo(writer, cs, ctx, false);
            }
            else
                writer.WriteBool(false);                 // type meta info appended flag

            if (writeTypeMetaPrefix)
                WriteTypeMetaInfoHeaderEnd(writer, metaInfoLengthPosition);
        }

        private static IValueItem ReadCollectionMetaTypeInfo(IStreamReader reader, uint typeId, ISerializeContext ctx)
        {
            Type type;
            string assemblyName = reader.ReadString();
            string typeFullName = reader.ReadString();

            ItemTypeFlags typeFlags = (ItemTypeFlags)reader.ReadUInt8();

            uint itemTypeId = reader.ReadUInt32();

            bool containsTypeMetaInfo = reader.ReadBool();
            IValueItem itemStructure = null;
            if (containsTypeMetaInfo)
            {
                itemStructure = ReadTypeMetaInfo(reader, itemTypeId, ctx, false);
            }

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
            else if (ctx.Serializer.AutoCreateMissingTypes)
            {
                if (typeFlags.HasFlag(ItemTypeFlags.Collection))
                {
                    // return dummy list holder
                    //var collectionObjRead = ctx.GetByType(typeof(IList<object>));
                    var collectionObjRead = new CollectionItems(typeof(IList<object>), null,null,null, ctx);
                    collectionObjRead.TypeId = typeId;  // fake type id with generic list
                    ctx.Serializer.RegisterTolerantTypeMapping(collectionObjRead);

                    return collectionObjRead;
                }
                else
                    throw new NotSupportedException("Auto implement collection type not yet supported!");
            }
            else
            {
                throw new TypeAccessException($"Collection type {typeFullName} not found!");
            }
        }

        private static void SkipCollectionTypeMetaInfo(IStreamReader reader)
        {
            reader.SkipString(); // skip assembly name
            reader.SkipString(); // skip type name
            reader.SkipUInt8();  // skip type flags
            reader.SkipUInt32(); // skip item type id
            var containsItemMetaType = reader.ReadBool();
            if (containsItemMetaType)
                SkipTypeMetaInfo(reader, false);
        }

        #endregion


        internal static uint CreateItemTypeFlagsHashForTypeIdCalculation(ItemTypeFlags itemTypeFlags, uint typeCode)
        {
            if (itemTypeFlags > 0)
                typeCode = Hashing.CreateHash((uint)itemTypeFlags, typeCode);

            return typeCode;
        }


        #region Auto Create Type


        private static IValueItem AutoCreateMissingDeserializeType(uint expectedTypeId, string orgAssemblyName, string typeFullName, IStreamReader reader, ISerializeContext ctx)
        {
            var existingStructure = ctx.Serializer.GetByTypeId(expectedTypeId);
            if (existingStructure != null)
            {
                SkipTypeMetaInfoOnlyProperties(reader);
                return existingStructure;
            }

            autoCreateMissingObjectTypeCount++;

            if (ctx.Serializer.AutoImplementMissingTypeMaxCount < autoCreateMissingObjectTypeCount)
                throw new InvalidOperationException($"Max threshold for auto create missing object count reached! Count: {autoCreateMissingObjectTypeCount}; Current type: {typeFullName}");

            short propertyCount = reader.ReadInt16();
            if (ctx.Serializer.AutoImplementMissingTypeMaxCount < propertyCount)
                throw new InvalidOperationException($"Max threshold for auto create missing object property count reached! Requested property count: {propertyCount}; Max count: {ctx.Serializer.AutoImplementMissingTypeMaxPropertyCount}; type: {typeFullName}");

            string typeName = typeFullName.Split('.').Last();

            string assemblyName = $"IOCTalk.AutoGenerated.{orgAssemblyName}";

            StringBuilder code = new StringBuilder();
            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine();
            code.Append("namespace ");
            code.Append(assemblyName);
            code.AppendLine();
            code.AppendLine("{");
            code.Append($"   public class {typeName}AutoGenerated");

            code.AppendLine();
            code.AppendLine("   {");
            code.AppendLine();

            bool includeOwnAssemblyReference = false;

            for (int i = 0; i < propertyCount; i++)
            {
                var rr = ReadPropertyItem(reader, ctx);

                IValueItem receivedPropertyStructure = null;
                if (rr.MetaInfoFlags.HasFlag(ItemMetaInfoFlags.TypeInfo) || rr.MetaInfoFlags.HasFlag(ItemMetaInfoFlags.CollectionTypeInfo))
                {
                    receivedPropertyStructure = ReadTypeMetaInfo(reader, rr.PropertyTypeId, ctx, false);
                }

                // add property code
                Type propType = ValueItem.GetRuntimeType(rr.ItemType);

                string codeTypeName = null;
                if (propType == null)
                {
                    propType = typeof(object);
                }

                if (rr.ItemType == ItemType.Enum)
                {
                    propType = typeof(AutoGenDummyImplementations.AutoGenDummyEnum);
                    includeOwnAssemblyReference = true;
                }

                codeTypeName = TypeService.GetSourceCodeTypeName(propType);
                if (rr.TypeFlags.HasFlag(ItemTypeFlags.Collection))
                {
                    if (rr.TypeFlags.HasFlag(ItemTypeFlags.Array))
                        codeTypeName = $"{codeTypeName}[]";
                    else
                        codeTypeName = $"List<{codeTypeName}>";
                }

                code.AppendLine($"      public {codeTypeName}{(rr.TypeFlags.HasFlag(ItemTypeFlags.Nullable) ? "?" : "")} {rr.PropertyName} {{get; set;}}");
                code.AppendLine();
            }

            // Debug information
            code.AppendLine($"      public uint DebugInfoTypeId => {expectedTypeId};");
            code.AppendLine($"      public string DebugInfoTypeFullName => \"{typeFullName}\";");


            code.AppendLine();
            code.AppendLine("   }");
            code.AppendLine("}");

            Type type = null;
            if (includeOwnAssemblyReference)
                type = TypeService.ImplementDynamicTypeWithTypeReference(code.ToString(), assemblyName, typeof(TypeMetaStructure));
            else
                type = TypeService.ImplementDynamicType(code.ToString(), assemblyName);

            ComplexStructure autoCreateStructure = new ComplexStructure(type, null, null, null, ctx);
            autoCreateStructure.TypeId = expectedTypeId;    // reset type id because of object only properties

            // Register auto created type
            ctx.Serializer.RegisterTolerantTypeMapping(autoCreateStructure);

            return autoCreateStructure;
        }

        #endregion
    }
}
