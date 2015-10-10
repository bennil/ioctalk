using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// TimeSpan JSON serialization
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-17
    /// </remarks>
    public class StructureTimeSpan : StructureString
    {
        #region StructureTimeSpan fields
        // ----------------------------------------------------------------------------------------
        // StructureTimeSpan fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureTimeSpan constructors
        // ----------------------------------------------------------------------------------------
        // StructureTimeSpan constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureTimeSpan</c> class.
        /// </summary>
        /// <param name="key"></param>
        public StructureTimeSpan(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureTimeSpan properties
        // ----------------------------------------------------------------------------------------
        // StructureTimeSpan properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureTimeSpan methods
        // ----------------------------------------------------------------------------------------
        // StructureTimeSpan methods
        // ----------------------------------------------------------------------------------------

        public override object Deserialize(string json, ref int currentReadIndex, SerializationContext context)
        {
            int startValueIndex = currentReadIndex + keyLength;

            if (json[startValueIndex] == Structure.CharQuotationMark)
            {
                startValueIndex++;

                int endValueIndex = json.IndexOf(Structure.QuotationMark, startValueIndex);

                currentReadIndex = endValueIndex + 1;

                string timeSpanStr = json.Substring(startValueIndex, endValueIndex - startValueIndex);

                return TimeParseExactInvariantCulture(timeSpanStr);
            }
            else
            {
                throw new InvalidOperationException("Unexptected JSON TimeSpan value!");
            }
        }


        /// <summary>
        /// Converts the given fixed format string representation of a time to its System.TimeSpan equivalent.
        /// This is way faster than the default parse method.
        /// 
        /// Supported formats:
        /// HH:mm:ss
        /// HH:mm:ss.f
        /// to
        /// HH:mm:ss.fffffff
        /// </summary>
        /// <param name="timeStr">The time STR.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        private static TimeSpan TimeParseExactInvariantCulture(string timeStr, int offset = 0)
        {
            long ticks = 0;

            // check days
            if (timeStr[offset + 2] != ':')
            {
                int days = 0;
                for (int i = 0; i < 7; i++)
                {

                    if (offset >= timeStr.Length)
                    {
                        break;
                    }

                    char c = timeStr[offset];

                    if (char.IsNumber(c))
                    {
                        days = days * 10 + (c - '0');
                    }
                    else if (c == '.')
                    {
                        break;
                    }
                    else
                    {
                        throw new InvalidCastException("Invalid time string: \"" + timeStr + "\"!");
                    }

                    offset++;
                }

                ticks += days * TimeSpan.TicksPerDay;
                offset++;
            }

            int hour = 0;
            hour = hour * 10 + (timeStr[offset] - '0');
            hour = hour * 10 + (timeStr[++offset] - '0');

            ticks += hour * TimeSpan.TicksPerHour;

            offset += 2;

            int minute = 0;
            minute = minute * 10 + (timeStr[offset] - '0');
            minute = minute * 10 + (timeStr[++offset] - '0');

            ticks += minute * TimeSpan.TicksPerMinute;

            offset += 2;

            int second = 0;
            second = second * 10 + (timeStr[offset] - '0');
            second = second * 10 + (timeStr[++offset] - '0');

            ticks += second * TimeSpan.TicksPerSecond;

            // check if datetime string contains milliseconds
            if (timeStr.Length > offset + 2
                && timeStr[++offset] == '.')
            {
                int milliseconds = 0;
                bool parseMicroseconds = true;
                for (int i = 0; i < 3; i++)
                {
                    offset++;

                    if (offset >= timeStr.Length)
                    {
                        parseMicroseconds = false;
                        break;
                    }

                    char c = timeStr[offset];

                    if (char.IsNumber(c))
                    {
                        milliseconds = milliseconds * 10 + (c - '0');
                    }
                    else
                    {
                        parseMicroseconds = false;
                        break;
                    }
                }

                ticks += milliseconds * TimeSpan.TicksPerMillisecond;

                if (parseMicroseconds)
                {
                    int microSeconds = 0;
                    bool parseTicks = true;
                    for (int i = 0; i < 3; i++)
                    {
                        offset++;

                        if (offset >= timeStr.Length)
                        {
                            parseTicks = false;
                            break;
                        }

                        char c = timeStr[offset];

                        if (char.IsNumber(c))
                        {
                            microSeconds = microSeconds * 10 + (c - '0');
                        }
                        else
                        {
                            parseTicks = false;
                            break;
                        }
                    }

                    ticks += microSeconds * StructureDateTime.TicksPerMicrosecond;

                    if (parseTicks)
                    {
                        if (offset < timeStr.Length)
                        {
                            int ticksPart = 0;
                            offset++;

                            char c = timeStr[offset];

                            if (char.IsNumber(c))
                            {
                                ticksPart = ticksPart * 10 + (c - '0');
                            }

                            ticks += ticksPart;
                        }
                    }
                }
            }

            return new TimeSpan(ticks);
        }

        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
