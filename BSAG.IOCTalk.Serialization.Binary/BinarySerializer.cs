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

namespace BSAG.IOCTalk.Serialization.Binary
{

    public class BinarySerializer : ITypeResolver, ISerializeContext
    {
        private static Dictionary<Type, IValueItem> globalStructureMapping = new Dictionary<Type, IValueItem>();
        private static Dictionary<uint, IValueItem> globalStructureMappingById = new Dictionary<uint, IValueItem>();
        private Dictionary<Type, IValueItem> differentTargetTypes = new Dictionary<Type, IValueItem>();
        private HashSet<uint> publishedMetaInfosPerInstance = new HashSet<uint>();

        private IUnknowTypeResolver unknowTypeResolver;
        private static object lockObj = new object();

        static BinarySerializer()
        {
            // register value type mappings
            RegisterTypeMapping(typeof(bool), new BoolItem(null, null, null));
            RegisterTypeMapping(typeof(char), new CharItem(null, null, null));

            RegisterTypeMapping(typeof(double), new DoubleItem(null, null, null));

            RegisterTypeMapping(typeof(byte), new ByteItem(null, null, null));
            RegisterTypeMapping(typeof(Int16), new Int16Item(null, null, null));
            RegisterTypeMapping(typeof(Int32), new Int32Item(null, null, null));
            RegisterTypeMapping(typeof(Int64), new Int64Item(null, null, null));
            RegisterTypeMapping(typeof(string), new StringItem(null, null, null));

            RegisterTypeMapping(typeof(TimeSpan), new TimeSpanItem(null, null, null));
        }

        public BinarySerializer(IUnknowTypeResolver unknowTypeResolver)
        {
            this.unknowTypeResolver = unknowTypeResolver;
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
            IValueItem structure = GetByType(type);

            structure.WriteValue(writer, this, obj);
        }

        private static void RegisterTypeMapping(Type type, IValueItem structure)
        {
            lock (lockObj)
            {
                globalStructureMapping[type] = structure;
                globalStructureMappingById.Add(structure.TypeId, structure);
            }
        }

        public object Deserialize(byte[] messageBytes, object contextObject)
        {
            var reader = new StreamReader(messageBytes);
            return Deserialize(reader, contextObject);
        }

        public object Deserialize(IStreamReader reader, object contextObject)
        {
            uint typeId = reader.ReadUInt32();
            IValueItem structure;
            if (!globalStructureMappingById.TryGetValue(typeId, out structure))
            {
                // Unknown type > Read exptected meta type information
                structure = TypeMetaStructure.ReadContentTypeMetaInfo(reader, typeId, this);
            }

            if (structure is ComplexStructure)
            {
                return ((ComplexStructure)structure).ReadValue(reader, this, false);
            }
            else
            {
                return structure.ReadValue(reader, this);
            }
        }



        public IValueItem GetByType(Type type)
        {
            IValueItem result;
            if (globalStructureMapping.TryGetValue(type, out result))
            {
                return result;
            }

            var newItemStructure = ValueItem.CreateValueItem(type, null, null, null, this);
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

        public Type DetermineTargetType(Type type)
        {
            Type result = unknowTypeResolver.DetermineTargetType(type);

            if (result == null)
            {
                throw new InvalidOperationException($"Cannot determine implementation object type for interface: \"{type}\"");
            }

            return result;
        }



        public IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType)
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

            if (diffType != null)
            {
                if (diffType != defaultInterfaceType)
                {
                    ComplexStructure differentTargetStructure = (ComplexStructure)GetByType(diffType);
                    differentTargetStructure.CheckDifferentType = false;
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
    }
}
