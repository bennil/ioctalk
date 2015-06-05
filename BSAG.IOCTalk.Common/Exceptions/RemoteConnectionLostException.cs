using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace BSAG.IOCTalk.Common.Exceptions
{
        /// <summary>
    /// Remote connection lost exception
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-09-16
    /// </remarks>
    public class RemoteConnectionLostException : Exception
    {
        #region RemoteConnectionLostException fields
        // ----------------------------------------------------------------------------------------
        // RemoteConnectionLostException fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region RemoteConnectionLostException constructors
        // ----------------------------------------------------------------------------------------
        // RemoteConnectionLostException constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>RemoteConnectionLostException</c> class.
        /// </summary>
        public RemoteConnectionLostException(IInvokeState lostInvokeRequest)
            : base("Remote connection lost!")
        {
            this.InvokeState = lostInvokeRequest;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region RemoteConnectionLostException properties
        // ----------------------------------------------------------------------------------------
        // RemoteConnectionLostException properties
        // ----------------------------------------------------------------------------------------
        
        /// <summary>
        /// Gets the state of the invoke.
        /// </summary>
        /// <value>
        /// The state of the invoke.
        /// </value>
        public IInvokeState InvokeState { get; private set; }

        // ----------------------------------------------------------------------------------------
        #endregion
        
        #region RemoteConnectionLostException methods
        // ----------------------------------------------------------------------------------------
        // RemoteConnectionLostException methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
