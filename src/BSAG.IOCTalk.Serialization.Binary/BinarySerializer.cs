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
using System.Reflection;
using System.Buffers;

namespace BSAG.IOCTalk.Serialization.Binary
{

    public class BinarySerializer //: ITypeResolver
    {
        private static Dictionary<Type, IValueItem> globalStructureMapping = new Dictionary<Type, IValueItem>();
        private static Dictionary<uint, IValueItem> globalStructureMappingById = new Dictionary<uint, IValueItem>();


        private IUnknowContextTypeResolver unknowTypeResolver;
        private static object lockObj = new object();



        static BinarySerializer()
        {
            // register value type mappings
            RegisterValueTypeMappings();
        }


        public BinarySerializer(IUnknowContextTypeResolver unknowTypeResolver)
        {
            this.unknowTypeResolver = unknowTypeResolver;
        }


        public bool AutoCreateMissingTypes { get; set; } = false;

        public Assembly[] CustomLookupAssemblies { get; set; }

        public IUnknowContextTypeResolver UnknowContextTypeResolver => unknowTypeResolver;

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
            RegisterTypeMapping(typeof(DateTimeOffset), new DateTimeOffsetItem(null, null, null));
            RegisterTypeMapping(typeof(Guid), new GuidItem(null, null, null));

            RegisterTypeMapping(typeof(UInt32), new UInt32Item(null, null, null));
            RegisterTypeMapping(typeof(UInt64), new UInt64Item(null, null, null));

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
            var serializeContext = new SerializationContext(this, false, contextObject);
            Serialize(writer, valueObj, type, serializeContext);
        }


        public byte[] Serialize(object obj, object contextObject)
        {
            Type type = obj.GetType();

            return Serialize(obj, type, contextObject);
        }

        public byte[] Serialize(object obj, Type type, object contextObject)
        {
            var writer = new StreamWriter(512);
            var serializeContext = new SerializationContext(this, false, contextObject);

            Serialize(writer, obj, type, serializeContext);

            return writer.Data.ToArray();
        }

        //public void Serialize(IStreamWriter writer, object obj, Type type, object contextObject)
        public void Serialize(IStreamWriter writer, object obj, Type type, ISerializeContext context)
        {
            if (context is null)
                throw new NullReferenceException("Serialization Context must be provided!");

            IValueItem structure = GetByType(type, context);

            structure.WriteValue(writer, context, obj);
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

        public object Deserialize(byte[] messageBytes, ISerializeContext deserializeContext)
        {
            if (deserializeContext is null)
                throw new NullReferenceException("Deserialization Context must be provided!");

            var reader = new StreamReader(messageBytes);
            return Deserialize(reader, deserializeContext);
        }

        public object Deserialize(byte[] messageBytes, int length, ISerializeContext deserializeContext)
        {
            if (deserializeContext is null)
                throw new NullReferenceException("Deserialization Context must be provided!");

            var reader = new StreamReader(messageBytes, length);
            return Deserialize(reader, deserializeContext);
        }

        public object Deserialize(ArraySegment<byte> messageBytesSegement, ISerializeContext deserializeContext)
        {
            if (deserializeContext is null)
                throw new NullReferenceException("Deserialization Context must be provided!");

            var reader = new StreamReader(messageBytesSegement.Array, messageBytesSegement.Offset, messageBytesSegement.Count);
            return Deserialize(reader, deserializeContext);
        }

        public object Deserialize(IStreamReader reader, ISerializeContext deserializeContext)
        {
            if (deserializeContext is null)
                throw new NullReferenceException("Deserialization Context must be provided!");

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







    }
}
