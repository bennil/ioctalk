using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Exceptions;
using BSAG.IOCTalk.Common.Reflection;

namespace BSAG.IOCTalk.Communication.Common
{
    /// <summary>
    /// Default IGenericMessage message implementation
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 09.07.2013
    /// </remarks>
    public class GenericMessage : IGenericMessage
    {
        #region GenericMessage fields
        // ----------------------------------------------------------------------------------------
        // GenericMessage fields
        // ----------------------------------------------------------------------------------------

        private object payload = null;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region GenericMessage constructors
        // ----------------------------------------------------------------------------------------
        // GenericMessage constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>GenericMessage</c> class.
        /// </summary>
        public GenericMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage"/> class.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        public GenericMessage(long requestId, MethodInfo method, object[] parameters)
            : this(requestId, method, parameters, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage"/> class with the given invoke method request values.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="responseExptected">if set to <c>true</c> [response exptected].</param>
        public GenericMessage(long requestId, MethodInfo method, object[] parameters, bool responseExptected)
        {
            this.Type = responseExptected ? MessageType.MethodInvokeRequest : MessageType.AsyncMethodInvokeRequest;
            this.RequestId = requestId;
            this.Target = TypeService.GetSourceCodeTypeName(method.DeclaringType);
            this.Name = method.Name;
            this.Payload = parameters;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage"/> class.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="invokeMethodInfo">The invoke method info.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="responseExptected">if set to <c>true</c> [response exptected].</param>
        public GenericMessage(long requestId, IInvokeMethodInfo invokeMethodInfo, object[] parameters, bool responseExptected)
        {            
            this.Type = responseExptected ? MessageType.MethodInvokeRequest : MessageType.AsyncMethodInvokeRequest;
            this.RequestId = requestId;
            this.Target = TypeService.GetSourceCodeTypeName(invokeMethodInfo.InterfaceMethod.DeclaringType);

            this.Name = invokeMethodInfo.QualifiedMethodName;
            this.Payload = parameters;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage"/> class with the given invoke method response values.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="returnObject">The return object.</param>
        public GenericMessage(long requestId, object returnObject)
        {
            this.Type = MessageType.MethodInvokeResponse;
            this.RequestId = requestId;
            this.Payload = returnObject;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericMessage"/> class with the given invoke method response values.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="exception">The exception.</param>
        public GenericMessage(long requestId, Exception exception)
        {
            this.Type = MessageType.Exception;
            this.RequestId = requestId;
            this.Payload = new ExceptionWrapper(exception);
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region GenericMessage properties
        // ----------------------------------------------------------------------------------------
        // GenericMessage properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the message type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public MessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the request id.
        /// </summary>
        /// <value>
        /// The request id.
        /// </value>
        public long RequestId { get; set; }


        /// <summary>
        /// Gets or sets the target interface.
        /// </summary>
        /// <value>
        /// The interface.
        /// </value>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the name of the request (method).
        /// </summary>
        /// <value>
        /// The name of the request.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        public object Payload
        {
            get
            {
                return payload;
            }
            set
            {
                payload = value;
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region GenericMessage methods
        // ----------------------------------------------------------------------------------------
        // GenericMessage methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} - {1} - ReqId: {2}", Type, Name, RequestId);
        }



        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
