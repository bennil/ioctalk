using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Exceptions
{
    /// <summary>
    /// Wrapper interface for exception serialization.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 2/18/2015 5:46:22 PM.
    ///  </para>
    /// </remarks>
    public interface IExceptionWrapper
    {
        #region properties

        /// <summary>
        /// Gets or sets the exception name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the full name of the type.
        /// </summary>
        /// <value>
        /// The full name of the type.
        /// </value>
        string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the exception text (ex.ToString).
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the message message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        string Message { get; set; }


        /// <summary>
        /// Gets or sets the serialized exception binary data.
        /// </summary>
        /// <value>
        /// The binary data.
        /// </value>
        byte[] BinaryData { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Try exception serialization.
        /// </summary>
        /// <param name="ex">The exception.</param>
        bool TrySerializeException(Exception ex);

        /// <summary>
        /// Try deserialize the binary wrapped exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        bool TryDeserializeException(out Exception exception);

        #endregion
    }
}
