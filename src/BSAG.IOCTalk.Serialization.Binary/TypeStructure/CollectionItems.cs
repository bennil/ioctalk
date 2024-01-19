using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using System.Reflection;
using BSAG.IOCTalk.Serialization.Binary.Utils;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure
{
    public class CollectionItems : IValueItem, IObjectType
    {
        private uint typeId;
        private Type type;
        private Type itemType;
        private Type targetCollectionType;
        private bool isArray;
        private bool isByteArray;
        private bool implementsIList;
        private MethodInfo specialCollAddMethod;
        private IValueItem itemStructure;
        private Func<object, object> getter;
        private Action<object, object> setter;
        private ItemTypeFlags itemTypeFlags = ItemTypeFlags.Collection | ItemTypeFlags.Nullable;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItems"/> class.
        /// </summary>
        public CollectionItems()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionItems"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public CollectionItems(Type type, string name, Func<object, object> getter, Action<object, object> setter, ITypeResolver typeResolver)
        {
            this.type = type;
            if (name != null)
            {
                this.Name = name;
            }
            else
            {
                this.Name = type.FullName;
            }
            this.isArray = type.IsArray;
            this.setter = setter;
            this.getter = getter;

            if (isArray)
            {
                itemType = type.GetElementType();
                isByteArray = itemType.Equals(typeof(byte));
                itemTypeFlags |= ItemTypeFlags.Array;
            }
            else
            {
                // type is a collection
                // check if collection is generic IEnumerable<T>
                if (type.IsGenericType)
                {
                    Type genericCollectionInterface = type.GetInterface("IEnumerable`1");
                    Type[] genericTypes = type.GetGenericArguments();
                    if (genericTypes.Length == 1)
                    {
                        Type listType = typeof(List<>);
                        targetCollectionType = listType.MakeGenericType(genericTypes);
                        itemType = genericTypes[0];
                    }
                    else if (genericTypes.Length == 0
                        && genericCollectionInterface != null)
                    {
                        genericTypes = genericCollectionInterface.GetGenericArguments();
                        Type listType = typeof(List<>);
                        targetCollectionType = listType.MakeGenericType(genericTypes);
                        itemType = genericTypes[0];
                    }
                    else
                    {
                        throw new NotImplementedException("More than one generic arguments is not supported yet!");
                    }

                    if (name is null)
                    {
                        // typeId contains Version and PublicToken because of default generic Type.FullName e.g.: System.Collections.Generic.IEnumerable`1[[System.Int32, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
                        // remove additional generic argument information to keep typeId consitent between application boundaries
                        int versionIndex = type.FullName.IndexOf(", Version=");
                        if (versionIndex > 0)
                        {
                            string cleanGenericName = type.FullName.Substring(0, versionIndex);
                            if (type.FullName.EndsWith("]]"))
                            {
                                cleanGenericName += "]]";
                            }

                            if (cleanGenericName.Contains(", Version="))
                                throw new InvalidOperationException($"Only one generic argument expected on collections! Clean generic name: {cleanGenericName}; Type fullname: {type.FullName}");

                            this.Name = cleanGenericName;
                        }
                    }
                }
                else
                {
                    var defaultConstructor = type.GetConstructor(System.Type.EmptyTypes);

                    if (defaultConstructor != null)
                    {
                        targetCollectionType = type;
                    }
                    else
                    {
                        // untyped collection
                        targetCollectionType = typeof(ArrayList);
                    }
                    itemType = typeof(object);
                }

                implementsIList = targetCollectionType.GetInterface("IList") != null;
                if (!implementsIList)
                {
                    specialCollAddMethod = targetCollectionType.GetMethod("Add");
                }
            }

            itemStructure = typeResolver.GetByType(itemType);
            this.typeId = CalculateTypeId();
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }




        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>The type.</value>
        public ItemType Type => itemStructure.Type;

        public ItemTypeFlags TypeFlags => itemTypeFlags;

        public IValueItem ItemStructure => itemStructure;

        /// <summary>
        /// Gets a value indicating whether this instance is nullable.
        /// </summary>
        /// <value><c>true</c> if this instance is nullable; otherwise, <c>false</c>.</value>
        public bool IsNullable
        {
            get
            {
                return true;
            }
        }

        public uint TypeId
        {
            get
            {
                return typeId;
            }
            internal set
            {
                typeId = value;
            }
        }

        public bool IsTypePrefixExpected
        {
            get
            {
                return true;
            }
        }

        public Type RuntimeType
        {
            get
            {
                return type;
            }
        }

        public bool IsArray
        {
            get { return isArray; }
        }

        public void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            // Collection structure:
            // TypeID UInt32
            // ContentType Byte
            // Count VarUInt32
            // Items...
            if (getter == null)
            {
                writer.WriteUInt32(this.TypeId); // only write collection type if element is root object

                if (context.IsWriteTypeMetaInfoRequired(this.TypeId))
                {
                    // serialize type meta info at the first time
                    TypeMetaStructure.WriteCollectionTypeMetaInfo(writer, this, context, true);
                }
            }

            if (value == null)
            {
                writer.WriteUInt8(ValueItem.NullValueIdent);
            }
            else
            {
                writer.WriteUInt8(ValueItem.CollectionObjectIdent);

                IEnumerable items;
                if (isArray)
                {
                    Array array = (Array)value;
                    writer.WriteVarUInt32((uint)array.Length);
                    items = array;

                    if (isByteArray)
                    {
                        writer.WriteBytes(new ArraySegment<byte>((byte[])array));
                        return;
                    }
                }
                else if (value is ICollection)
                {
                    ICollection collection = (ICollection)value;
                    writer.WriteVarUInt32((uint)collection.Count);
                    items = collection;
                }
                else
                {
                    items = (IEnumerable)value;

                    uint count = 0;
                    foreach (var item in items)
                    {
                        count++;
                    }
                    writer.WriteVarUInt32(count);
                }

                context.Key = this.Name;
                object oldParentParentObj = context.ParentParentObject;
                object oldParentObj = context.ParentObject;
                context.ParentObject = value;
                context.ParentParentObject = oldParentObj;
                context.ChildLevel++;

                int index = 0;
                foreach (var item in items)
                {
                    context.ArrayIndex = index;
                    itemStructure.WriteValue(writer, context, item);
                    index++;
                }
                context.ChildLevel--;
                context.ArrayIndex = null;
                context.ParentObject = oldParentObj;
                context.ParentParentObject = oldParentParentObj;
            }
        }

        public object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            byte contentType = reader.ReadUInt8();
            context.Key = this.Name;

            if (contentType == ValueItem.CollectionObjectIdent)
            {
                int count = (int)reader.ReadVarUInt32();

                if (isArray)
                {
                    Array array = Array.CreateInstance(itemType, count);

                    if (isByteArray)
                    {
                        return reader.ReadBytes(count).ToArray();
                    }
                    else
                    {

                        for (int i = 0; i < count; i++)
                        {
                            context.ArrayIndex = i;
                            var item = itemStructure.ReadValue(reader, context);
                            array.SetValue(item, i);
                        }
                        context.ArrayIndex = null;
                    }
                    return array;
                }
                else if (implementsIList)
                {
                    IList coll = (IList)TypeService.CreateInstance(targetCollectionType);

                    for (int i = 0; i < count; i++)
                    {
                        context.ArrayIndex = i;
                        var item = itemStructure.ReadValue(reader, context);
                        coll.Add(item);
                    }
                    context.ArrayIndex = null;

                    return coll;
                }
                else
                {

                    object specialColl = TypeService.CreateInstance(targetCollectionType);

                    for (int i = 0; i < count; i++)
                    {
                        context.ArrayIndex = i;
                        var item = itemStructure.ReadValue(reader, context);
                        specialCollAddMethod.Invoke(specialColl, new object[] { item });
                    }
                    context.ArrayIndex = null;

                    return specialColl;

                }
            }
            else if (contentType == ValueItem.NullValueIdent)
            {
                return null;
            }
            else if (contentType == ValueItem.TypeMetaInfo)
            {
                TypeMetaStructure.SkipTypeMetaInfo(reader, true);
                return this.ReadValue(reader, context);
            }
            else
            {
                throw new InvalidOperationException($"Type ident {contentType} not expected!");
            }
        }

        private uint CalculateTypeId()
        {
            uint typeCode = Hashing.CreateHash((uint)Type);
            typeCode = Hashing.CreateHash(itemStructure.TypeId, typeCode);

            typeCode = TypeMetaStructure.CreateItemTypeFlagsHashForTypeIdCalculation(TypeFlags, typeCode);

            if (typeCode <= 150)
            {
                // Reserved type IDs for ItemType enumeration
                typeCode += 151;
            }

            return typeCode;
        }


        /// <summary>
        /// Gets the given value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Object.</returns>
        public object GetItemValue(object instance)
        {
            if (getter != null)
                return getter(instance);
            else
                return instance;
        }

        public void SetItemValue(object instance, object propertyValue)
        {
            if (setter != null)
                setter(instance, propertyValue);
            else
                throw new NotSupportedException("Own replacement not supported!");
        }
    }
}
