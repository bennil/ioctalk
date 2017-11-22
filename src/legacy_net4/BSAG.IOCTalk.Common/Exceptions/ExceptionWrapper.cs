using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
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

            TrySerializeException(ex);
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


        /// <summary>
        /// Gets or sets the serialized exception binary data.
        /// </summary>
        /// <value>
        /// The binary data.
        /// </value>
        public byte[] BinaryData { get; set; }

        #endregion

        #region methods

        /// <summary>
        /// Try exception serialization.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public bool TrySerializeException(Exception ex)
        {
            if (ex.GetType().IsSerializable)
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                MemoryStream mStream = new MemoryStream();
                try
                {
                    binaryFormatter.Serialize(mStream, ex);
                    mStream.Position = 0;
                    byte[] exBinary = new byte[mStream.Length];
                    mStream.Read(exBinary, 0, exBinary.Length);
                    mStream.Close();

                    this.BinaryData = exBinary;
                    return true;
                }
                catch
                {
                    // Local exception can't be serialized
                    this.BinaryData = null;
                }
                finally
                {
                    mStream.Close();
                }
            }
            return false;
        }

        /// <summary>
        /// Try deserialize the binary wrapped exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public bool TryDeserializeException(out Exception exception)
        {
            if (BinaryData != null)
            {
                MemoryStream mStream = null;
                try
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    mStream = new MemoryStream(this.BinaryData);
                    exception = (Exception)binaryFormatter.Deserialize(mStream);
                    mStream.Close();
                    return true;
                }
                catch
                {
                    // Remote exception can't be deserialized
                }
                finally
                {
                    if (mStream != null)
                        mStream.Close();
                }
            }
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
