//using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
//using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BSAG.IOCTalk.Serialization.Binary
//{
//    /// <summary>
//    /// Class SerializationContext.
//    /// </summary>
//    public class SerializationContext : ITypeResolver
//    {
//        private static Dictionary<Type, IValueItem> structureMapping = new Dictionary<Type, IValueItem>();
//        private static Dictionary<int, IValueItem> structureMappingById = new Dictionary<int, IValueItem>();

//        static SerializationContext()
//        {
//            // register value type mappings
//            RegisterTypeMapping(typeof(bool), new BoolItem(null, null, null));
//            RegisterTypeMapping(typeof(char), new CharItem(null, null, null));

//            RegisterTypeMapping(typeof(double), new DoubleItem(null, null, null));

//            RegisterTypeMapping(typeof(int), new Int32Item(null, null, null));
//            RegisterTypeMapping(typeof(string), new StringItem(null, null, null));

//            RegisterTypeMapping(typeof(TimeSpan), new TimeSpanItem(null, null, null));
//        }


//        /// <summary>
//        /// Gets or sets the type of the interface.
//        /// </summary>
//        /// <value>The type of the interface.</value>
//        public Type InterfaceType { get; set; }

//        public IValueItem GetByType(Type type)
//        {
//            IValueItem result;
//            if (structureMapping.TryGetValue(type, out result))
//            {
//                return result;
//            }

//            var newItemStructure = ValueItem.CreateValueItem(type, null, null, null, this);
//            RegisterTypeMapping(type, newItemStructure);

//            return newItemStructure;
//        }

//        public IValueItem GetByTypeId(int typeId)
//        {
//            IValueItem result;
//            if (structureMappingById.TryGetValue(typeId, out result))
//            {
//                return result;
//            }

//            throw new KeyNotFoundException($"Type id \"{typeId}\" not found!");
//        }

//        public Type DetermineTargetType(Type type)
//        {
//            Type result = unknowTypeResolver(context);

//            if (result == null)
//            {
//                throw new InvalidOperationException($"Cannot determine implementation object type for interface: \"{context.InterfaceType}\"");
//            }

//            return result;
//        }

//        internal static void RegisterTypeMapping(Type type, IValueItem structure)
//        {
//            structureMapping[type] = structure;
//            structureMappingById[structure.TypeId] = structure;
//        }

//    }
//}
