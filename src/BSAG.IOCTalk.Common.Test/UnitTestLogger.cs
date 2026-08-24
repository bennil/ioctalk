using BSAG.IOCTalk.Common.Interface.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Common.Test
{
    public class UnitTestLogger : ILogger
    {
        private ITestOutputHelper xUnitLogger;

        public UnitTestLogger(ITestOutputHelper xUnitLogger)
        {
            this.xUnitLogger = xUnitLogger;
        }

        void ILogger.Debug(string message)
        {
            WriteLine("DEBUG: " + message);
        }

        void ILogger.Info(string message)
        {
            WriteLine("INFO: " + message);
        }

        void ILogger.Warn(string message)
        {
            WriteLine("WARN: " + message);
        }

        void ILogger.Error(string message)
        {
            if (WriteLine("ERROR: " + message))
            {
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Writes to the xunit output if a test is still active.
        /// Background threads (e.g. tcp auto reconnect) can log after the test has already completed.
        /// In that case xunit throws an InvalidOperationException which must not crash the test host process.
        /// </summary>
        /// <returns>true if the test is still active; otherwise false</returns>
        private bool WriteLine(string message)
        {
            try
            {
                xUnitLogger.WriteLine(message);
                return true;
            }
            catch (InvalidOperationException)
            {
                // no active test anymore > fallback console output only
                Console.WriteLine(message);
                return false;
            }
        }
    }
}
