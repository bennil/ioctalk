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

                return DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new InvalidOperationException("Unexptected JSON TimeSpan value!");
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion


    }

}
