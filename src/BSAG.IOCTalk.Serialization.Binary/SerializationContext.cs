using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary
{
    /// <summary>
    /// Class SerializationContext.
    /// </summary>
    public class SerializationContext : ISerializeContext
    {
        private IUnknowContextTypeResolver unknowContextTypeResolver;

        public SerializationContext(BinarySerializer serializer, IUnknowContextTypeResolver unknowContextTypeResolver, bool isDeserialize)
        {
            this.serializer = serializer;
            this.unknowContextTypeResolver = unknowContextTypeResolver;
            this.IsDeserialize = isDeserialize;
        }


        private BinarySerializer serializer;


        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the external context object.
        /// </summary>
        /// <value>
        /// The context object.
        /// </value>
        public object ExternalContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is deserialize.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deserialize; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeserialize { get; set; }

        /// <summary>
        /// Gets or sets the parent serialization object.
        /// </summary>
        /// <value>
        /// The parent object.
        /// </value>
        public object ParentObject { get; set; }


        /// <summary>
        /// Gets or sets the index of the array.
        /// </summary>
        /// <value>
        /// The index of the array.
        /// </value>
        public int? ArrayIndex { get; set; }

        public BinarySerializer Serializer
        {
            get
            {
                return serializer;
            }
        }

        public IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType)
        {
            return unknowContextTypeResolver.DetermineSpecialInterfaceType(objectType, defaultInterfaceType, this);
            //return serializer.DetermineSpecialInterfaceType(objectType, defaultInterfaceType, this);
        }

        public Type DetermineTargetType(Type interfaceType)
        {
            return unknowContextTypeResolver.DetermineTargetType(interfaceType, this);
        }

        public IValueItem GetByType(Type type)
        {
            return serializer.GetByType(type, this);
        }

        public IValueItem GetByTypeId(uint typeId)
        {
            return serializer.GetByTypeId(typeId);
        }

        public bool IsWriteTypeMetaInfoRequired(uint typeId)
        {
            return serializer.IsWriteTypeMetaInfoRequired(typeId);
        }


        public void Reset(object externalContext)
        {
            this.ExternalContext = externalContext;
            this.Key = null;
            this.ParentObject = null;
            this.ArrayIndex = null;
        }
        
    }
}
