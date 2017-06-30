using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using Bond.IO.Safe;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Serialization.Binary.Utils;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure
{
    public class ComplexStructure : ITypeStructure
    {


        private Type type;
        private Type concreteTargetType;
        private IList<IValueItem> items;
        private Func<object, object> getter;
        private Action<object, object> setter;
        //private bool isReadTypeIdExpected;
        private bool isObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexStructure"/> class.
        /// </summary>
        public ComplexStructure()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexStructure"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public ComplexStructure(Type type, Func<object, object> getter, Action<object, object> setter, ITypeResolver typeResolver)
        {
            this.type = type;
            if (type.IsInterface || type.IsAbstract)
            {
                this.concreteTargetType = typeResolver.DetermineTargetType(type);
            }
            else
            {
                this.concreteTargetType = type;
            }
            this.Name = type.FullName;
            this.getter = getter;
            this.setter = setter;
            this.CheckDifferentType = true;

            this.isObject = type.Equals(typeof(object));

            DetermineStrucutre(typeResolver);

        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }


        /// <summary>
        /// Gets the type identifier (unique type hash code).
        /// </summary>
        /// <value>The type identifier.</value>
        public uint TypeId { get; private set; }

        public IList<IValueItem> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
            }
        }

        /// <summary>
        /// Gets the item type.
        /// </summary>
        /// <value>The type.</value>
        public ItemType Type { get; set; }

        /// <summary>
        /// Gets the runtime type
        /// </summary>
        /// <value>The type of the runtime.</value>
        public Type RuntimeType
        {
            get
            {
                return type;
            }
        }

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

        /// <summary>
        /// Gets a value indicating whether this instance is type prefix expected.
        /// </summary>
        /// <value><c>true</c> if this instance is type prefix expected; otherwise, <c>false</c>.</value>
        public bool IsTypePrefixExpected
        {
            get
            {
                return true;
            }
        }

        
        /// <summary>
        /// Gets a value indicating whether [check different type].
        /// </summary>
        /// <value><c>true</c> if [check different type]; otherwise, <c>false</c>.</value>
        public bool CheckDifferentType { get; internal set; }


        private void DetermineStrucutre(ITypeResolver typeResolver)
        {
            if (type.IsValueType)
            {
                throw new InvalidOperationException($"No value type exptected! Type: {type.FullName}");
            }
            Type = ItemType.ComplexObject;

            items = new List<IValueItem>();
            HashSet<string> existingProperties = new HashSet<string>();

            AddProperties(type, existingProperties, typeResolver);

            if (type.IsInterface)
            {
                // analyze interface properties
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    AddProperties(interfaceType, existingProperties, typeResolver);
                }
            }

            this.TypeId = CalculateTypeId();
        }

        private void AddProperties(Type t, HashSet<string> existingProperties, ITypeResolver typeResolver)
        {
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Public))
            {
                if (existingProperties.Contains(prop.Name))
                {
                    continue;
                }
                if (prop.GetIndexParameters().Length > 0)
                {
                    // ignore index properties
                    continue;
                }
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }
                if (prop.PropertyType.Equals(type))
                {
                    // ignore same nested types because of recursive endless loop
                    continue;
                }

                IValueItem item = ValueItem.CreateValueItem(prop, typeResolver);
                items.Add(item);

                existingProperties.Add(prop.Name);
            }
        }

        private uint CalculateTypeId()
        {                            
            uint typeCode = Hashing.CreateHash(Name);

            typeCode = Hashing.CreateHash((uint)Type, typeCode);

            foreach (var item in items)
            {
                typeCode = Hashing.CreateHash(item.Name, typeCode);
                typeCode = Hashing.CreateHash((uint)item.Type, typeCode);
            }

            if (typeCode <= 150)
            {
                // Reserved type IDs for ItemType enumeration
                typeCode += 151;
            }

            return typeCode;
        }


        public void WriteValue(IStreamWriter writer, ISerializeContext context, object value)
        {
            // Complex structure
            // TypeID UInt32
            // ContentType Byte
            if (isObject)
            {
                // property or collection does not specify a target type
                // determine object type
                if (value != null)
                {
                    Type valType = value.GetType();

                    IValueItem item = context.GetByType(valType);
                    if (!item.IsTypePrefixExpected)
                    {
                        // Write type id for value types as well (unknown structure)
                        writer.WriteUInt32(item.TypeId);

                        writer.WriteUInt8(ValueItem.SingleValueIdent);
                    }

                    item.WriteValue(writer, context, value);
                }
                else
                {
                    // write complex object type id for null value
                    writer.WriteInt32((int)ItemType.ComplexObject);
                    writer.WriteUInt8(ValueItem.NullValueIdent);
                }
            }
            else if (value == null)
            {
                // Write null
                writer.WriteUInt32(this.TypeId);
                writer.WriteUInt8(ValueItem.NullValueIdent);
            }
            else
            {
                IValueItem differentTargetStructure = null;
                if (CheckDifferentType)
                {
                    differentTargetStructure = context.DetermineSpecialInterfaceType(value.GetType(), type);                    
                }

                if (differentTargetStructure != null)
                {
                    // Special target interface type serialization
                    differentTargetStructure.WriteValue(writer, context, value);
                }
                else
                {
                    // Just write type properties
                    writer.WriteUInt32(this.TypeId);

                    if (context.IsWriteTypeMetaInfoRequired(this.TypeId))
                    {
                        // serialize type meta info at the first time
                        TypeMetaStructure.WriteTypeMetaInfo(writer, this);
                    }

                    writer.WriteUInt8(ValueItem.SingleObjectIdent);

                    for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                    {
                        var item = items[itemIndex];

                        var valueItem = item.GetItemValue(value);

                        item.WriteValue(writer, context, valueItem);
                    }
                }
            }
        }


        public object ReadValue(IStreamReader reader, ISerializeContext context)
        {
            return ReadValue(reader, context, true);
        }

        public object ReadValue(IStreamReader reader, ISerializeContext context, bool isReadTypeIdExpected)
        {
            if (isReadTypeIdExpected)
            {
                // read type of property
                uint actualTypeId = reader.ReadUInt32();

                if (TypeId != actualTypeId)
                {
                    if (actualTypeId == (int)ItemType.ComplexObject)
                    {
                        // Special read > only null expected
                        byte contentObjType = reader.ReadUInt8();

                        if (contentObjType == ValueItem.NullValueIdent)
                        {
                            return null;
                        }
                        else
                        {
                            throw new ArgumentException("ComplexObject type ID is only expected with null!");
                        }
                    }

                    var actualStructure = context.GetByTypeId(actualTypeId);

                    if (actualStructure == null)
                    {
                        // type not in local cache > type meta info exptected
                        actualStructure = TypeMetaStructure.ReadContentTypeMetaInfo(reader, actualTypeId, context);
                    }

                    if (actualStructure.IsTypePrefixExpected)
                    {
                        if (actualStructure is ComplexStructure)
                        {
                            // type id already consumed > do not read again
                            return ((ComplexStructure)actualStructure).ReadValue(reader, context, false);
                        }
                        else
                        {
                            return actualStructure.ReadValue(reader, context);
                        }
                    }
                    else
                    {
                        // read content type because of unknown strucutre
                        byte contentObjType = reader.ReadUInt8();

                        if (contentObjType == ValueItem.NullValueIdent)
                        {
                            return null;
                        }
                        else
                        {
                            return actualStructure.ReadValue(reader, context);
                        }
                    }
                }
            }


            byte contentType = reader.ReadUInt8();

            if (contentType == ValueItem.TypeMetaInfo)
            {
                // type meta data already loaded for type id > skip data
                TypeMetaStructure.SkipTypeMetaInfo(reader);
                contentType = reader.ReadUInt8();
            }

            switch (contentType)
            {
                case ValueItem.SingleObjectIdent:
                    object newObject = TypeService.CreateInstance(concreteTargetType);

                    for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                    {
                        var item = items[itemIndex];

                        var itemValue = item.ReadValue(reader, context);
                        item.SetItemValue(newObject, itemValue);
                    }

                    return newObject;

                case ValueItem.NullValueIdent:
                    return null;

                case ValueItem.CollectionObjectIdent:
                    throw new NotImplementedException();

                default:
                    throw new InvalidOperationException($"Type ident {contentType} not expected!");
            }
        }


        /// <summary>
        /// Gets the given value.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>System.Object.</returns>
        public object GetItemValue(object instance)
        {
            if (getter != null)
            {
                return getter(instance);
            }
            else
            {
                return instance;
            }
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
