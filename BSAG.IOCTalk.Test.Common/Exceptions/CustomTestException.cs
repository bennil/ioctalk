using System;
using System.Runtime.Serialization;

namespace BSAG.IOCTalk.Test.Common.Exceptions
{
    [Serializable]
    public class CustomTestException : Exception
    {
        #region fields

        private const string PropertyNameErrorCode = "ErrorCode";
        private const string PropertyNameCustomExceptionData = "CustomExceptionData";

        #endregion fields

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestException"/> class.
        /// </summary>
        public CustomTestException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CustomTestException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public CustomTestException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        ///
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected CustomTestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetInt32(PropertyNameErrorCode);
            CustomExceptionData = info.GetString(PropertyNameCustomExceptionData);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTestException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="customExceptionData">The custom exception data.</param>
        public CustomTestException(string message, int errorCode, string customExceptionData)
            : base(message)
        {
            this.ErrorCode = errorCode;
            this.CustomExceptionData = customExceptionData;
        }

        #endregion constructors

        #region properties

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>
        /// The error code.
        /// </value>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the custom exception data.
        /// </summary>
        /// <value>
        /// The custom exception data.
        /// </value>
        public string CustomExceptionData { get; set; }

        #endregion properties

        #region methods

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). </exception>
        ///
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/>
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/>
        ///   </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(PropertyNameErrorCode, ErrorCode);
            info.AddValue(PropertyNameCustomExceptionData, CustomExceptionData);

            base.GetObjectData(info, context);
        }

        #endregion methods
    }
}