using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.Common
{
    public static class TestUtils
    {
        /// <summary>
        /// Gets the current unit test.
        /// </summary>
        /// <returns></returns>
        public static UnitTest GetCurrentUnitTest()
        {
            int unitTestCmdIndex;
            string cmd = Environment.CommandLine;
            if (cmd != null
                && (unitTestCmdIndex = cmd.IndexOf("-UnitTest")) >= 0)
            {
                int enumStartIndex = cmd.IndexOf(" ", unitTestCmdIndex) + 1;
                int enumEndIndex = cmd.IndexOf(" ", enumStartIndex + 1);
                if (enumEndIndex < 0)
                {
                    // end reached
                    enumEndIndex = cmd.Length;
                }

                string testEnumStr = cmd.Substring(enumStartIndex, enumEndIndex - enumStartIndex);
                return (UnitTest)Enum.Parse(typeof(UnitTest), testEnumStr);
            }
            return UnitTest.PerformanceMonitorTest;
        }
    }
}
