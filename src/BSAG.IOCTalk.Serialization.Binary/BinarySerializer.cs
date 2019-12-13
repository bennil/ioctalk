using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Communication;
using Bond.IO.Safe;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Serialization.Binary.Stream;
using BSAG.IOCTalk.Serialization.Binary.Utils;

namespace BSAG.IOCTalk.Serialization.Binary
{

    public class BinarySerializer //: ITypeResolver
    {
        private static Dictionary<Type, IValueItem> globalStructureMapping = new Dictionary<Type, IValueItem>();
        private static Dictionary<uint, IValueItem> globalStructureMappingById = new Dictionary<uint, IValueItem>();
        private Dictionary<Type, IValueItem> differentTargetTypes = new Dictionary<Type, IValueItem>();
        private HashSet<uint> publishedMetaInfosPerInstance = new HashSet<uint>();
        private Dictionary<Type, List<string>> stringHashItemProperties = new Dictionary<Type, List<string>>();
        private Dictionary<uint, string> stringHashValues;

        private IUnknowContextTypeResolver unknowTypeResolver;
        private static object lockObj = new object();

        private SerializationContext serializeContext;
        private SerializationContext deserializeContext;


        static BinarySerializer()
        {
            // register value type mappings
            RegisterValueTypeMappings();
        }


        public BinarySerializer(IUnknowContextTypeResolver unknowTypeResolver)
        {
            this.unknowTypeResolver = unknowTypeResolver;

            serializeContext = new SerializationContext(this, this.unknowTypeResolver, false);
            deserializeContext = new SerializationContext(this, unknowTypeResolver, true);
        }


        private static void RegisterValueTypeMappings()
        {
            RegisterTypeMapping(typeof(bool), new BoolItem(null, null, null));

            RegisterTypeMapping(typeof(double), new DoubleItem(null, null, null));
            RegisterTypeMapping(typeof(decimal), new DecimalItem(null, null, null));

            RegisterTypeMapping(typeof(byte), new ByteItem(null, null, null));
            RegisterTypeMapping(typeof(Int16), new Int16Item(null, null, null));
            RegisterTypeMapping(typeof(Int32), new Int32Item(null, null, null));
            RegisterTypeMapping(typeof(Int64), new Int64Item(null, null, null));

            RegisterTypeMapping(typeof(char), new CharItem(null, null, null));
            RegisterTypeMapping(typeof(string), new StringItem(null, null, null));

            RegisterTypeMapping(typeof(TimeSpan), new TimeSpanItem(null, null, null));
            RegisterTypeMapping(typeof(DateTime), new DateTimeItem(null, null, null));
            RegisterTypeMapping(typeof(Guid), new GuidItem(null, null, null));
        }



        public byte[] Serialize<T>(T valueObj, object contextObject)
        {
            Type type = typeof(T);
            //todo: can be optimized with static generic caching
            return Serialize(valueObj, type, contextObject);
        }

        public void Serialize<T>(IStreamWriter writer, T valueObj, object contextObject)
        {
            Type type = typeof(T);
            //todo: can be optimized with static generic caching
            Serialize(writer, valueObj, type, contextObject);
        }

        public byte[] Serialize(object obj, object contextObject)
        {
            Type type = obj.GetType();

            return Serialize(obj, type, contextObject);
        }

        public byte[] Serialize(object obj, Type type, object contextObject)
        {
            var writer = new StreamWriter(512);

            Serialize(writer, obj, type, contextObject);

            return writer.Data.ToArray();
        }

        public void Serialize(IStreamWriter writer, object obj, Type type, object contextObject)
        {
            serializeContext.Reset(contextObject);

            IValueItem structure = GetByType(type, serializeContext);

            structure.WriteValue(writer, serializeContext, obj);
        }

        private static void RegisterTypeMapping(Type type, IValueItem structure)
        {
            try
            {
                lock (lockObj)
                {
                    globalStructureMapping[type] = structure;
                    globalStructureMappingById.Add(structure.TypeId, structure);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Clears the global structure cache.
        /// </summary>
        public static void ClearGlobalStructureCache()
        {
            globalStructureMapping.Clear();
            globalStructureMappingById.Clear();

            RegisterValueTypeMappings();
        }


        /// <summary>
        /// Registers the given tolerant binary layout type mapping structure.
        /// </summary>
        /// <param name="structure">The structure.</param>
        internal static void RegisterTolerantTypeMapping(IValueItem structure)
        {
            lock (lockObj)
            {
                globalStructureMappingById.Add(structure.TypeId, structure);
            }
        }

        public object Deserialize(byte[] messageBytes, object contextObject)
        {
            var reader = new StreamReader(messageBytes);
            return Deserialize(reader, contextObject);
        }

        public object Deserialize(ArraySegment<byte> messageBytesSegement, object contextObject)
        {
            var reader = new StreamReader(messageBytesSegement.Array, messageBytesSegement.Offset, messageBytesSegement.Count);
            return Deserialize(reader, contextObject);
        }

        public object Deserialize(IStreamReader reader, object contextObject)
        {
            deserializeContext.Reset(contextObject);

            uint typeId = reader.ReadUInt32();
            IValueItem structure;
            if (!globalStructureMappingById.TryGetValue(typeId, out structure))
            {
                // Unknown type > Read exptected meta type information
                structure = TypeMetaStructure.ReadContentTypeMetaInfo(reader, typeId, deserializeContext);
            }

            if (structure is ComplexStructure)
            {
                return ((ComplexStructure)structure).ReadValue(reader, deserializeContext, false);
            }
            else
            {
                return structure.ReadValue(reader, deserializeContext);
            }
        }



        public IValueItem GetByType(Type type, ISerializeContext ctx)
        {
            IValueItem result;
            if (globalStructureMapping.TryGetValue(type, out result))
            {
                return result;
            }

            var newItemStructure = ValueItem.CreateValueItem(null, type, null, null, null, ctx);
            RegisterTypeMapping(type, newItemStructure);

            return newItemStructure;
        }

        public IValueItem GetByTypeId(uint typeId)
        {
            IValueItem result;
            if (globalStructureMappingById.TryGetValue(typeId, out result))
            {
                return result;
            }

            return null;
        }

        //public Type DetermineTargetType(Type type)
        //{
        //    Type result = unknowTypeResolver.DetermineTargetType(type);

        //    if (result == null)
        //    {
        //        throw new InvalidOperationException($"Cannot determine implementation object type for interface: \"{type}\"");
        //    }

        //    return result;
        //}



        public IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType, ISerializeContext ctx)
        {
            IValueItem result;
            if (differentTargetTypes.TryGetValue(objectType, out result))
            {
                return result;
            }

            Type diffType = null;
            // check expose sub type attribute
            var exposureAttributes = objectType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
            if (exposureAttributes.Length > 0)
            {
                diffType = ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
            }

            var differentTargetStructure = RegisterDifferentTargetType(objectType, defaultInterfaceType, diffType, ctx);
            return differentTargetStructure;

        }

        internal bool TryGetDifferentTargetType(Type objectType, out IValueItem targetItem)
        {
            return differentTargetTypes.TryGetValue(objectType, out targetItem);
        }

        internal IValueItem RegisterDifferentTargetType(Type objectType, Type defaultInterfaceType, Type diffType, ISerializeContext ctx)
        {
            if (diffType != null)
            {
                if (diffType != defaultInterfaceType)
                {
                    var differentTargetStructure = GetByType(diffType, ctx);
                    if (differentTargetStructure is ComplexStructure)
                    {
                        ((ComplexStructure)differentTargetStructure).CheckDifferentType = false;
                    }
                    differentTargetTypes[objectType] = differentTargetStructure;
                    return differentTargetStructure;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                differentTargetTypes[objectType] = null;
                return null;
            }
        }

        public bool IsWriteTypeMetaInfoRequired(uint typeId)
        {
            if (publishedMetaInfosPerInstance.Contains(typeId))
            {
                return false;
            }
            else
            {
                publishedMetaInfosPerInstance.Add(typeId);
                return true;
            }
        }

        public void RegisterStringHashProperty(Type type, string propertyName)
        {
            if (stringHashValues == null)
                stringHashValues = new Dictionary<uint, string>();

            List<string> props;
            if (!stringHashItemProperties.TryGetValue(type, out props))
            {
                props = new List<string>();
                stringHashItemProperties.Add(type, props);
            }

            if (!props.Contains(propertyName))
                props.Add(propertyName);
        }

        public bool IsStringHashProperty(Type type, string propertyName)
        {
            List<string> props;
            if (stringHashItemProperties.TryGetValue(type, out props))
            {
                return props.Contains(propertyName);
            }
            return false;
        }

        public bool IsWriteHashStringRequired(string stringValue, out uint stringHashCode)
        {
            stringHashCode = Hashing.CreateHash(stringValue);

            if (stringHashValues.ContainsKey(stringHashCode))
            {
                return false;
            }
            else
            {
                RegisterStringHashCodeValue(stringValue, stringHashCode);
                return true;
            }
        }

        internal void RegisterStringHashCodeValue(string stringValue, uint stringHashCode)
        {
            string existingStr;
            if (stringHashValues.TryGetValue(stringHashCode, out existingStr))
            {
                if (!stringValue.Equals(existingStr))
                {
                    throw new InvalidOperationException($"String hash duplicate for {stringHashCode} - cached string: \"{existingStr}\" - new string: \"{stringValue}\"");
                }
            }
            else
            {
                stringHashValues.Add(stringHashCode, stringValue);
            }
        }

        public string GetHashString(uint hashCode)
        {
            string result;
            stringHashValues.TryGetValue(hashCode, out result);
            return result;
        }
    }
}
