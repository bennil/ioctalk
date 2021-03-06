﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Specifies a generic message format
    /// </summary>
    public interface IGenericMessage
    {
        /// <summary>
        /// Gets the message type.
        /// </summary>
        MessageType Type { get; set; }

        
        /// <summary>
        /// Gets the request id.
        /// </summary>
        /// <value>
        /// The request id.
        /// </value>
        long RequestId { get; set; }


        /// <summary>
        /// Gets the target interface.
        /// </summary>
        /// <value>
        /// The interface.
        /// </value>
        string Target { get; set; }

        /// <summary>
        /// Gets the name of the request (method).
        /// </summary>
        /// <value>
        /// The name of the request.
        /// </value>
        string Name { get; set; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        object Payload { get; set; }
    }
}
