using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// DateTime JSON type structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-07-14
    /// </remarks>
    public class StructureDateTime : AbstractStructure
    {
        #region StructureDateTime fields
        // ----------------------------------------------------------------------------------------
        // StructureDateTime fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureDateTime constructors
        // ----------------------------------------------------------------------------------------
        // StructureDateTime constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureDateTime</c> class.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isArrayItem"></param>
        public StructureDateTime(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureDateTime properties
        // ----------------------------------------------------------------------------------------
        // StructureDateTime properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureDateTime methods
        // ----------------------------------------------------------------------------------------
        // StructureDateTime methods
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Serializes the specified date time value.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(Key);
                sb.Append(Structure.QuotationColonSeparator);
            }
            DateTime dateTimeValue = (DateTime)obj;

            sb.Append(Structure.QuotationMark);
            if (dateTimeValue.Millisecond > 0)
            {
                string strTime = dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture).TrimEnd('0');
                sb.Append(strTime);
            }
            else
            {
                sb.Append(dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
            }
            sb.Append(Structure.QuotationMark);
        }

        /// <summary>
        /// Deserializes the specified json date time string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override object Deserialize(string json, ref int currentReadIndex, SerializationContext context)
        {
            int startValueIndex = currentReadIndex + keyLength;

            if (json[startValueIndex] == Structure.CharQuotationMark)
            {
                startValueIndex++;

                int endValueIndex = json.IndexOf(Structure.QuotationMark, startValueIndex);

                currentReadIndex = endValueIndex + 1;

                string dateTimeStr = json.Substring(startValueIndex, endValueIndex - startValueIndex);

                return DateTimeParseExactInvariantCulture(dateTimeStr);
            }
            else
            {
                throw new InvalidOperationException("Unexptected JSON TimeSpan value!");
            }
        }


        /// <summary>
        /// Converts the given fixed format string representation of a date and time to its System.DateTime equivalent.
        /// This is way faster than the default parse method.
        /// 
        /// Supported formats:
        /// yyyy-MM-dd HH:mm:ss
        /// yyyy-MM-dd HH:mm:ss.f
        /// to
        /// yyyy-MM-dd HH:mm:ss.fffffff
        /// </summary>
        /// <param name="dateTimeStr">The date time STR.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        private static DateTime DateTimeParseExactInvariantCulture(string dateTimeStr, int offset = 0)
        {
            int year = 0;
            year = year * 10 + (dateTimeStr[offset] - '0');
            year = year * 10 + (dateTimeStr[++offset] - '0');
            year = year * 10 + (dateTimeStr[++offset] - '0');
            year = year * 10 + (dateTimeStr[++offset] - '0');

            offset += 2;

            int month = 0;
            month = month * 10 + (dateTimeStr[offset] - '0');
            month = month * 10 + (dateTimeStr[++offset] - '0');

            offset += 2;

            int day = 0;
            day = day * 10 + (dateTimeStr[offset] - '0');
            day = day * 10 + (dateTimeStr[++offset] - '0');

            offset += 2;

            int hour = 0;
            hour = hour * 10 + (dateTimeStr[offset] - '0');
            hour = hour * 10 + (dateTimeStr[++offset] - '0');

            offset += 2;

            int minute = 0;
            minute = minute * 10 + (dateTimeStr[offset] - '0');
            minute = minute * 10 + (dateTimeStr[++offset] - '0');

            offset += 2;

            int second = 0;
            second = second * 10 + (dateTimeStr[offset] - '0');
            second = second * 10 + (dateTimeStr[++offset] - '0');

            // check if datetime string contains milliseconds
            if (dateTimeStr.Length > offset + 2
                && dateTimeStr[++offset] == '.')
            {
                int milliseconds = 0;
                bool parseTicksPart = true;
                int millisecondIndex = 0;
                for (; millisecondIndex < 3; millisecondIndex++)
                {
                    offset++;

                    if (offset >= dateTimeStr.Length)
                    {
                        parseTicksPart = false;
                        break;
                    }

                    char c = dateTimeStr[offset];

                    if (char.IsNumber(c))
                    {
                        milliseconds = milliseconds * 10 + (c - '0');
                    }
                    else
                    {
                        parseTicksPart = false;
                        break;
                    }
                }
                if (millisecondIndex == 1)
                {
                    milliseconds *= 100;
                }
                else if (millisecondIndex == 2)
                {
                    milliseconds *= 10;
                }

                DateTime result = new DateTime(year, month, day, hour, minute, second, milliseconds);

                if (parseTicksPart)
                {
                    int ticksPart = 0;
                    int ticksMultiplicator = 1000;
                    for (int ticksPartIndex = 0; ticksPartIndex < 4; ticksPartIndex++)
                    {
                        offset++;

                        if (offset >= dateTimeStr.Length)
                        {
                            break;
                        }

                        char c = dateTimeStr[offset];

                        if (char.IsNumber(c))
                        {
                            ticksPart = ticksPart * 10 + (c - '0');

                            if (ticksPartIndex > 0)
                            {
                                ticksMultiplicator /= 10;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    int ticks = ticksPart * ticksMultiplicator;
                    result = result.AddTicks(ticks);
                }
                return result;
            }
            else
            {
                return new DateTime(year, month, day, hour, minute, second);
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
