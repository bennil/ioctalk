using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Logging;
using System.Diagnostics;

namespace BSAG.IOCTalk.Logging
{
    /// <summary>
    /// System.Diagnostics.Trace logger
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/11/2014 11:58:03 AM.
    ///  </para>
    /// </remarks>
    public class TraceLogger : ILogger
    {
        #region fields
        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>TraceLogger</c>.
        /// </summary>
        public TraceLogger()
        {
        }

        #endregion

        #region properties
        #endregion

        #region methods

        /// <summary>
        /// Log debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log info message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log warn message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(string message)
        {
            Trace.TraceWarning(message);
        }

        /// <summary>
        /// Log error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(string message)
        {
            Trace.TraceError(message);
        }

        #endregion


    }
}
