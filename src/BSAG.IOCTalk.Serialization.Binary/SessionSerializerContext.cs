using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Binary
{
    internal class SessionSerializerContext
    {

        public SessionSerializerContext(BinarySerializer serializer, object externalContext) 
        {
            SerializeContext = new SerializationContext(serializer, false, externalContext);
            DeserializeContext = new SerializationContext(serializer, true, externalContext);

            SerializeContext.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Target));
            SerializeContext.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Name));

            DeserializeContext.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Target));
            DeserializeContext.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Name));
        }

        public SerializationContext SerializeContext { get; private set; }

        public SerializationContext DeserializeContext { get; private set; }

    }
}
