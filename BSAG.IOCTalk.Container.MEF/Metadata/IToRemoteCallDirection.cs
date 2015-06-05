using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Container.MEF.Metadata
{
    /// <summary>
    /// Marks an export to only be used for outgoing remote service calls (proxy).
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 7/22/2014 9:43:26 AM.
    ///  </para>
    /// </remarks>
    public interface IToRemoteCallDirection
    {
        #region properties

        /// <summary>
        /// Gets a value indicating whether this instance is to remote call direction.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is to remote call direction; otherwise, <c>false</c>.
        /// </value>
        bool IsToRemoteCallDirection { get; }

        #endregion

        #region methods
        #endregion
    }
}
