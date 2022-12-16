using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Interface IMessageStreamSerializer
    /// </summary>
    public interface IMessageStreamSerializer
    {
        /// <summary>
        /// Serializes given message to stream.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="message">The message.</param>
        /// <param name="contextObject">The context object.</param>
        void Serialize(IStreamWriter writer, IGenericMessage message, object contextObject, int sessionId);


        /// <summary>
        /// Deserialize buffered message from stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns>IEnumerable&lt;IGenericMessage&gt;.</returns>
        IGenericMessage Deserialize(IStreamReader reader, object contextObject, int sessionId);
    }
}
