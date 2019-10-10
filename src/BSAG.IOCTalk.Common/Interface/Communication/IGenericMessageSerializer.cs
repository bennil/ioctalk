using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Specifies a <see cref="IGenericMessage"/> serializer
    /// </summary>
    public interface IGenericMessageSerializer
    {
        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        void RegisterContainerHost(IGenericContainerHost containerHost);

        /// <summary>
        /// Serializes the <see cref="IGenericMessage"/> to string.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        string SerializeToString(IGenericMessage message, object contextObject);

        /// <summary>
        /// Deserializes from string to a <see cref="IGenericMessage"/>.
        /// </summary>
        /// <param name="messageString">The message string.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        IGenericMessage DeserializeFromString(string messageString, object contextObject);


        /// <summary>
        /// Serializes to byte array.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        byte[] SerializeToBytes(IGenericMessage message, object contextObject);


        /// <summary>
        /// Deserializes from byte array.
        /// </summary>
        /// <param name="messageBytes">The message bytes.</param>
        /// <returns></returns>
        IGenericMessage DeserializeFromBytes(byte[] messageBytes, object contextObject);

        /// <summary>
        /// Deserializes from byte array.
        /// </summary>
        /// <param name="messageBytes">The message bytes.</param>
        /// <returns></returns>
        IGenericMessage DeserializeFromBytes(ArraySegment<byte> messageBytesSegment, object contextObject);


        /// <summary>
        /// Gets or sets a value indicating whether this instance is missing fields in source data allowed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is missing fields in source data allowed; otherwise, <c>false</c>.
        /// </value>
        bool IsMissingFieldsInSourceDataAllowed { get; set; }


        /// <summary>
        /// Gets the serializer raw message format.
        /// </summary>
        RawMessageFormat MessageFormat { get; }
    }
}
