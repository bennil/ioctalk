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
            xUnitLogger.WriteLine("DEBUG: " + message);
        }

        void ILogger.Info(string message)
        {
            xUnitLogger.WriteLine("INFO: " + message);
        }

        void ILogger.Warn(string message)
        {
            xUnitLogger.WriteLine("WARN: " + message);
        }

        void ILogger.Error(string message)
        {
            xUnitLogger.WriteLine("ERROR: " + message);
            throw new Exception(message);
        }
    }
}
