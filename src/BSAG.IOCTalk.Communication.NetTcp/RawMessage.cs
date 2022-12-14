using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;

namespace BSAG.IOCTalk.Communication.NetTcp
{
    /// <summary>
    /// IRawMessage implementation
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 12/16/2014 3:44:07 PM.
    ///  </para>
    /// </remarks>
    public class RawMessage : IRawMessage
    {
        #region fields
        
        private RawMessageFormat format;
        private int readLength;
        private byte[] data;
        private int sessionId;
        #endregion

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RawMessage"/> class.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        public RawMessage(int sessionId)
        {
            this.sessionId = sessionId;
        }

        /// <summary>
        /// Creates and initializes an instance of the class <c>RawMessage</c>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="data">The data.</param>
        /// <param name="readLength">Length of the read.</param>
        /// <param name="sessionId">The session id.</param>
        public RawMessage(RawMessageFormat format, byte[] data, int readLength, int sessionId)
        {
            this.format = format;
            this.data = data;
            this.readLength = readLength;
            this.sessionId = sessionId;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the message format.
        /// </summary>
        public RawMessageFormat MessageFormat
        {
            get { return format; }
            set { format = value; }
        }

        /// <summary>
        /// Gets or sets the current read length.
        /// </summary>
        public int Length
        {
            get
            {
                return readLength;
            }
            set
            {
                readLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public int SessionId
        {
            get { return sessionId; }
        }

        #endregion

        #region methods
        #endregion

    }
}
