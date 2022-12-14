using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace BSAG.IOCTalk.Communication.NetTcp
{
        /// <summary>
    /// ConnectionStateChangedEventArgs
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 
    /// </remarks>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        private Client client = null;

        #region ConnectionClosedEventArgs constructors
        // ----------------------------------------------------------------------------------------
        // ConnectionClosedEventArgs constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <c>ConnectionClosedEventArgs</c>.
        /// </summary>
        public ConnectionStateChangedEventArgs(Client client)
        {
            this.client = client;
        }

        // ----------------------------------------------------------------------------------------
        #endregion
        
        #region ConnectionClosedEventArgs properties
        // ----------------------------------------------------------------------------------------
        // ConnectionClosedEventArgs properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gibt die betroffene Socket zurück.
        /// </summary>
        public Client Client
        {
            get
            {
                return this.client;
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion        
    }

}
