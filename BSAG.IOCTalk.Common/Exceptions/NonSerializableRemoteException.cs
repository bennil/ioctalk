using System;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Common.Exceptions
{
    /// <summary>
    /// The NonSerializableRemoteException is used if the original exception could not be serialized.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-08-21
    /// </remarks>
    public class NonSerializableRemoteException : Exception
    {
        #region NonSerializableRemoteException fields

        // ----------------------------------------------------------------------------------------
        // NonSerializableRemoteException fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------

        #endregion NonSerializableRemoteException fields

        #region NonSerializableRemoteException constructors

        // ----------------------------------------------------------------------------------------
        // NonSerializableRemoteException constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>NonSerializableRemoteException</c> class.
        /// </summary>
        public NonSerializableRemoteException(IInvokeState invokeState, string message)
            : base(message)
        {
            this.InvokeState = invokeState;
            ExceptionWrapper.AddRemoteInvokeIdentification(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonSerializableRemoteException"/> class.
        /// </summary>
        /// <param name="invokeState">State of the invoke.</param>
        /// <param name="message">The message.</param>
        /// <param name="name">The name.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="messageOnly">The message only.</param>
        public NonSerializableRemoteException(IInvokeState invokeState, string message, string name, string typeName, string messageOnly)
            : this(invokeState, message)
        {
            this.Name = name;
            this.TypeName = typeName;
            this.MessageOnly = messageOnly;
        }

        // ----------------------------------------------------------------------------------------

        #endregion NonSerializableRemoteException constructors

        #region NonSerializableRemoteException properties

        // ----------------------------------------------------------------------------------------
        // NonSerializableRemoteException properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the state of the invoke.
        /// </summary>
        /// <value>
        /// The state of the invoke.
        /// </value>
        public IInvokeState InvokeState { get; private set; }

        /// <summary>
        /// Gets or sets the exception name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the full name of the type.
        /// </summary>
        /// <value>
        /// The full name of the type.
        /// </value>
        public string TypeName { get; private set; }

        /// <summary>
        /// Gets or sets the plain message without stack trace.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string MessageOnly { get; private set; }

        // ----------------------------------------------------------------------------------------

        #endregion NonSerializableRemoteException properties

        #region NonSerializableRemoteException methods

        // ----------------------------------------------------------------------------------------
        // NonSerializableRemoteException methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------

        #endregion NonSerializableRemoteException methods
    }
}