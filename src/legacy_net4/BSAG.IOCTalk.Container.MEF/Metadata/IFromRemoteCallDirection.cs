using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Container.MEF.Metadata
{
    /// <summary>
    /// Marks an export to only be used for incoming remote calls.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 7/22/2014 9:41:57 AM.
    ///  </para>
    /// </remarks>
    public interface IFromRemoteCallDirection
    {
        #region properties

        /// <summary>
        /// Gets a value indicating whether this instance is from remote call direction.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is from remote call direction; otherwise, <c>false</c>.
        /// </value>
        bool IsFromRemoteCallDirection { get; }

        #endregion

        #region methods
        #endregion
    }
}
