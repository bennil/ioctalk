using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Xml.Linq;

namespace BSAG.IOCTalk.Common.Interface.Logging
{
    /// <summary>
    /// Data stream logging interface
    /// </summary>
    public interface IDataStreamLogger : ISessionStateChanged, IDisposable
    {
        /// <summary>
        /// Inits the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="loggerName">Name of the logger.</param>
        /// <param name="configXml">The config XML.</param>
        void Init(IGenericCommunicationService source, string loggerName, XElement configXml);


        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        /// <param name="encodeBase64">if set to <c>true</c> [encode base64].</param>
        void LogStreamMessage(int sessionId, bool isReceive, byte[] messageData, bool encodeBase64);


        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataSegement">The message data.</param>
        /// <param name="encodeBase64">if set to <c>true</c> [encode base64].</param>
        void LogStreamMessage(int sessionId, bool isReceive, ArraySegment<byte> messageDataSegement, bool encodeBase64);

        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataString">The message data string.</param>
        void LogStreamMessage(int sessionId, bool isReceive, string messageDataString);
    }
}
