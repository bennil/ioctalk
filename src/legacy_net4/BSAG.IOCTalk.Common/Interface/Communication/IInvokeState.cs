using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using BSAG.IOCTalk.Common.Interface.Reflection;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Contains all invoke request/response mapping objects.
    /// </summary>
    public interface IInvokeState
    {
        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        /// <value>
        /// The request message.
        /// </value>
        IGenericMessage RequestMessage { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        MethodInfo Method { get; set; }


        /// <summary>
        /// Gets or sets the wait handle.
        /// </summary>
        /// <value>
        /// The wait handle.
        /// </value>
        ManualResetEventSlim WaitHandle { get; set; }

        /// <summary>
        /// Gets or sets the return object.
        /// </summary>
        /// <value>
        /// The return object.
        /// </value>
        object ReturnObject { get; set; }


        /// <summary>
        /// Gets or sets the out parameter values.
        /// </summary>
        /// <value>
        /// The out parameter values.
        /// </value>
        object[] OutParameterValues { get; set; }
        

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the method source.
        /// </summary>
        /// <value>
        /// The method source.
        /// </value>
        IInvokeMethodInfo MethodSource { get; set; }
    }
}
