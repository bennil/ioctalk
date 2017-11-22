using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Logging
{
    /// <summary>
    /// Technical logging interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Log debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        void Debug(string message);

        /// <summary>
        /// Log info message.
        /// </summary>
        /// <param name="message">The message.</param>
        void Info(string message);

        /// <summary>
        /// Log warn message.
        /// </summary>
        /// <param name="message">The message.</param>
        void Warn(string message);

        /// <summary>
        /// Log error message.
        /// </summary>
        /// <param name="message">The message.</param>
        void Error(string message);
    }
}
