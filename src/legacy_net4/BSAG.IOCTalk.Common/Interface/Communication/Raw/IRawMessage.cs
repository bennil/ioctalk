using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication.Raw
{
    /// <summary>
    /// RAW message interface
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 12/16/2014 3:39:07 PM.
    ///  </para>
    /// </remarks>
    public interface IRawMessage
    {
        #region properties

        /// <summary>
        /// Gets the message format.
        /// </summary>
        RawMessageFormat MessageFormat { get; set; }

        /// <summary>
        /// Gets or sets the current read length.
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        byte[] Data { get; set; }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        int SessionId { get; }

        #endregion

        #region methods
        #endregion
    }
}
