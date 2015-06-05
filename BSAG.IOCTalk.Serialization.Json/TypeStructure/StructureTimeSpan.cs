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

                return TimeSpan.Parse(timeSpanStr);
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
