using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service.MEF
{
    /// <summary>
    /// TODO summary description of class...
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/4/2014 10:16:31 AM.
    ///  </para>
    /// </remarks>
    public class PerfSubscribeResponse : IPerfSubscribeResponse
    {
        #region fields


        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>PerfSubscribeResponse</c>.
        /// </summary>
        public PerfSubscribeResponse()
        {
        }

        #endregion

        #region properties

        public int SubscsrbeId { get; set; }


        public DateTime Time { get; set; }

        #endregion

        #region methods
        #endregion

        
    }
}
