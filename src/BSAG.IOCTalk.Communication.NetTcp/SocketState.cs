using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO.Pipelines;

namespace BSAG.IOCTalk.Communication.NetTcp
{
    /// <summary>
    /// The SocketState class holds the read buffer for a client.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 07.09.2010
    /// </remarks>
    public class SocketState
    {
        #region SocketState fields
        // ----------------------------------------------------------------------------------------
        // SocketState fields
        // ----------------------------------------------------------------------------------------

        ///// <summary>
        ///// Shared read buffer
        ///// </summary>
        //public byte[] readBuffer;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SocketState constructors
        // ----------------------------------------------------------------------------------------
        // SocketState constructors
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketState"/> class.
        /// </summary>
        public SocketState()
        {
            ReceivePipe = new Pipe();
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SocketState properties
        // ----------------------------------------------------------------------------------------
        // SocketState properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public Client Client { get; set; }


        public Pipe ReceivePipe { get; private set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region SocketState methods
        // ----------------------------------------------------------------------------------------
        // SocketState methods
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
