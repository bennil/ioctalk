using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;

namespace IOCTalk.StreamAnalyzer.Implementation
{
    /// <summary>
    /// Method invoke roundtrip
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 11/24/2015 11:29:15 AM.
    ///  </para>
    /// </remarks>
    public class MethodInvokeRoundtrip
    {
        #region fields


        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>MethodInvokeRoundtrip</c>.
        /// </summary>
        public MethodInvokeRoundtrip()
        {
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the round trip time.
        /// </summary>
        public TimeSpan? RoundTripTime
        {
            get
            {
                if (Request != null
                    && Response != null)
                {
                    return ResponseTime - RequestTime;
                }

                return null;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether this instance is receive.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is receive; otherwise, <c>false</c>.
        /// </value>
        public bool IsReceive { get; set; }

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public IGenericMessage Request { get; set; }

        /// <summary>
        /// Gets or sets the request time.
        /// </summary>
        /// <value>
        /// The request time.
        /// </value>
        public TimeSpan RequestTime { get; set; }


        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public IGenericMessage Response { get; set; }

        /// <summary>
        /// Gets or sets the response time.
        /// </summary>
        /// <value>
        /// The response time.
        /// </value>
        public TimeSpan? ResponseTime { get; set; }
                
        #endregion

        #region methods

        #endregion
    }
}
