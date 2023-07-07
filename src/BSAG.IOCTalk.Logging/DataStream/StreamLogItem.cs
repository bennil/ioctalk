using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Logging.DataStream
{
    /// <summary>
    /// Pending stream log data holder
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/9/2014 2:19:40 PM.
    ///  </para>
    /// </remarks>
    public class StreamLogItem
    {
        #region fields

        /// <summary>
        /// Receive indicator char
        /// </summary>
        public const char ReceivedChar = 'R';

        /// <summary>
        /// Send indicator char
        /// </summary>
        public const char SentChar = 'S';

        /// <summary>
        /// Time format definition
        /// </summary>
        public const string TimeFormatString = "HH:mm:ss.fffffff";

        private const char TabChar = '\t';

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>StreamLogItem</c>.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataString">The message data string.</param>
        public StreamLogItem(int sessionId, bool isReceive, string messageDataString)
        {
            this.Time = DateTime.UtcNow;
            this.SessionId = sessionId;
            this.IsReceive = isReceive;
            this.MessageDataString = messageDataString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamLogItem" /> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        /// <param name="encodeBase64">if set to <c>true</c> [encode base64].</param>
        public StreamLogItem(int sessionId, bool isReceive, byte[] messageData, bool encodeBase64)
            : this(sessionId, isReceive, encodeBase64 ? Convert.ToBase64String(messageData) : Encoding.UTF8.GetString(messageData))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamLogItem" /> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        /// <param name="encodeBase64">if set to <c>true</c> [encode base64].</param>
        public StreamLogItem(int sessionId, bool isReceive, byte[] messageData, int msgLength, bool encodeBase64)
            : this(sessionId, isReceive, encodeBase64 ? Convert.ToBase64String(messageData, 0, msgLength) : Encoding.UTF8.GetString(messageData, 0, msgLength))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamLogItem"/> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        public StreamLogItem(int sessionId, bool isReceive, ArraySegment<byte> messageDataSeg, bool encodeBase64)
            : this(sessionId, isReceive, encodeBase64 ? Convert.ToBase64String(messageDataSeg.Array, messageDataSeg.Offset, messageDataSeg.Count) : Encoding.UTF8.GetString(messageDataSeg.Array, messageDataSeg.Offset, messageDataSeg.Count))
        {
        }


        #endregion

        #region properties

        /// <summary>
        /// Gets the time.
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Gets the session.
        /// </summary>
        public int SessionId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is receive.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is receive; otherwise, <c>false</c>.
        /// </value>
        public bool IsReceive { get; private set; }

        /// <summary>
        /// Gets the message data.
        /// </summary>
        public string MessageDataString { get; private set; }

        #endregion

        #region methods

        /// <summary>
        /// Creates the log string.
        /// </summary>
        /// <returns></returns>
        public string CreateLogString()
        {
            StringBuilder sbLogLine = new StringBuilder();

            sbLogLine.Append(Time.ToString(TimeFormatString));
            sbLogLine.Append(TabChar);

            sbLogLine.Append(SessionId);

            sbLogLine.Append(TabChar);
            if (IsReceive)
            {
                sbLogLine.Append(ReceivedChar);
            }
            else
            {
                sbLogLine.Append(SentChar);
            }
            sbLogLine.Append(TabChar);

            sbLogLine.Append(MessageDataString);

            return sbLogLine.ToString();
        }

        #endregion
    }
}
