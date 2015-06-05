using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common
{
    /// <summary>
    /// TODO summary description of interface...
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/4/2014 10:15:20 AM.
    ///  </para>
    /// </remarks>
    public interface IPerfSubscribeResponse
    {        
        #region properties

        int SubscsrbeId { get; set; }

        DateTime Time { get; set; }

        #endregion

        #region methods
        #endregion
    }
}
