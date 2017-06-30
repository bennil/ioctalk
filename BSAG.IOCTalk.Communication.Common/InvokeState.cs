using System;
using System.Reflection;
using System.Threading;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Reflection;

namespace BSAG.IOCTalk.Communication.Common
{
    /// <summary>
    /// Default method invoke state implementation
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 09.07.2013
    /// </remarks>
    public class InvokeState : IInvokeState
    {
        #region InvokeState fields

        // ----------------------------------------------------------------------------------------
        // InvokeState fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------

        #endregion InvokeState fields

        #region InvokeState constructors

        // ----------------------------------------------------------------------------------------
        // InvokeState constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>InvokeState</c> class.
        /// </summary>
        public InvokeState()
        {
        }

        // ----------------------------------------------------------------------------------------

        #endregion InvokeState constructors

        #region InvokeState properties

        // ----------------------------------------------------------------------------------------
        // InvokeState properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        /// <value>
        /// The request message.
        /// </value>
        public IGenericMessage RequestMessage { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public MethodInfo Method { get; set; }

        /// <summary>
        /// Gets or sets the wait handle.
        /// </summary>
        /// <value>
        /// The wait handle.
        /// </value>
        public ManualResetEventSlim WaitHandle { get; set; }

        /// <summary>
        /// Gets or sets the return object.
        /// </summary>
        /// <value>
        /// The return object.
        /// </value>
        public object ReturnObject { get; set; }

        /// <summary>
        /// Gets or sets the out parameter values.
        /// </summary>
        /// <value>
        /// The out parameter values.
        /// </value>
        public object[] OutParameterValues { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the method source.
        /// </summary>
        /// <value>
        /// The method source.
        /// </value>
        public IInvokeMethodInfo MethodSource { get; set; }

        // ----------------------------------------------------------------------------------------

        #endregion InvokeState properties

        #region InvokeState methods

        // ----------------------------------------------------------------------------------------
        // InvokeState methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------

        #endregion InvokeState methods
    }
}