using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;

namespace BSAG.IOCTalk.Serialization.Binary
{
    public class BinaryMessageSerializer : IGenericMessageSerializer, IUnknowTypeResolver, IMessageStreamSerializer
    {
        private IGenericContainerHost containerHost;
        private BinarySerializer serializer;

        public bool IsMissingFieldsInSourceDataAllowed { get; set; } = true;


        public BinaryMessageSerializer()
        {
            serializer = new BinarySerializer(this);
        }

        /// <summary>
        /// Gets the serializer raw message format.
        /// </summary>
        /// <value>The message format.</value>
        public RawMessageFormat MessageFormat
        {
            get
            {
                return RawMessageFormat.Binary;
            }
        }


        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        public void RegisterContainerHost(IGenericContainerHost containerHost)
        {
            this.containerHost = containerHost;

            // Register message type
            serializer.GetByType(typeof(IGenericMessage));
        }

        public IGenericMessage DeserializeFromBytes(byte[] messageBytes, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(messageBytes, contextObject);
        }

        public IGenericMessage DeserializeFromString(string messageString, object contextObject)
        {
            throw new NotImplementedException();
        }


        public byte[] SerializeToBytes(IGenericMessage message, object contextObject)
        {
            return serializer.Serialize<IGenericMessage>(message, contextObject);
        }

        public string SerializeToString(IGenericMessage message, object contextObject)
        {
            throw new NotImplementedException();
        }


        public Type DetermineTargetType(Type interfaceType)
        {
            if (interfaceType.Equals(typeof(IGenericMessage)))
            {
                return typeof(Communication.Common.GenericMessage);
            }
            return containerHost.GetInterfaceImplementationType(interfaceType.FullName);
        }

        public void Serialize(IStreamWriter writer, IGenericMessage message, object contextObject)
        {
            serializer.Serialize<IGenericMessage>(writer, message, contextObject);
        }

        public IGenericMessage Deserialize(IStreamReader reader, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(reader, contextObject);
        }
    }
}
