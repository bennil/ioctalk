using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Exceptions;
using BSAG.IOCTalk.Common.Attributes;

namespace BSAG.IOCTalk.Common.Exceptions
{
    /// <summary>
    /// The wrapper contains serialized exception informations.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 2/18/2015 4:52:06 PM.
    ///  </para>
    /// </remarks>
    public class ExceptionWrapper : IExceptionWrapper
    {
        #region fields

        /// <summary>
        /// Remote Invoke Exception Data dictionary key
        /// </summary>
        public const string RemoteInvokeExceptionKey = "RemoteInvokeException";

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>ExceptionWrapper</c>.
        /// </summary>
        public ExceptionWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionWrapper"/> class.
        /// </summary>
        /// <param name="ex">The source exception.</param>
        public ExceptionWrapper(Exception ex)
        {
            Type exType = ex.GetType();
            this.Name = exType.Name;
            this.TypeName = exType.FullName;
            this.Message = ex.Message;
            this.Text = ex.ToString();
        }


        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the exception name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full name of the type.
        /// </summary>
        /// <value>
        /// The full name of the type.
        /// </value>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the exception text (ex.ToString).
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the message message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }


        // Historically held a BinaryFormatter blob of the original exception. The field is
        // retained on the wire contract for compatibility with peers that still send it, but
        // the value is discarded on receive and never produced on send. BinaryFormatter cannot
        // safely deserialize untrusted bytes (RCE via well-known gadget chains), so the blob
        // must never reach Deserialize().
        [Obsolete("BinaryFormatter exception payload is no longer transmitted or consumed. Value is ignored.", false)]
        public byte[] BinaryData
        {
            get => null;
            set { /* intentionally discarded */ }
        }

        #endregion

        #region methods

        /// <summary>
        /// Try exception serialization. No-op: original exception object is no longer transmitted.
        /// </summary>
        public bool TrySerializeException(Exception ex)
        {
            return false;
        }

        /// <summary>
        /// Try deserialize the binary wrapped exception. No-op: BinaryFormatter is unsafe on
        /// untrusted input. Callers should fall back to <see cref="NonSerializableRemoteException"/>
        /// constructed from the wrapper's Name/TypeName/Message/Text fields.
        /// </summary>
        public bool TryDeserializeException(out Exception exception)
        {
            exception = null;
            return false;
        }

        /// <summary>
        /// Returns the exception text.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Text;
        }


        /// <summary>
        /// Adds the remote invoke identification to the given exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        public static void AddRemoteInvokeIdentification(Exception ex)
        {
            ex.Data[RemoteInvokeExceptionKey] = true;
        }

        #endregion
    }
}
