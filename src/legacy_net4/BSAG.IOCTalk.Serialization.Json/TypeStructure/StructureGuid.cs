using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Guid json serialization
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-09-23
    /// </remarks>
    public class StructureGuid : AbstractStructure
    {
        #region StructureGuid fields
        // ----------------------------------------------------------------------------------------
        // StructureGuid fields
        // ----------------------------------------------------------------------------------------
        
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureGuid constructors
        // ----------------------------------------------------------------------------------------
        // StructureGuid constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureGuid</c> class.
        /// </summary>
        public StructureGuid(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureGuid properties
        // ----------------------------------------------------------------------------------------
        // StructureGuid properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureGuid methods
        // ----------------------------------------------------------------------------------------
        // StructureGuid methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Serializes the specified object.
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
            sb.Append(Structure.QuotationMark);
            sb.Append(obj);
            sb.Append(Structure.QuotationMark);
        }

        /// <summary>
        /// Deserializes the specified json.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override object Deserialize(string json, ref int currentReadIndex, SerializationContext context)
        {
            int startValueIndex = currentReadIndex + keyLength;
            if (json[startValueIndex] == Structure.CharQuotationMark)
            {
                startValueIndex++;
                int endValueIndex = json.IndexOf(Structure.QuotationMark, startValueIndex);

                currentReadIndex = endValueIndex + 1;

                string stringValue = json.Substring(startValueIndex, endValueIndex - startValueIndex);

                return Guid.Parse(stringValue);               
            }
            else
            {
                throw new InvalidCastException("Invalid char \"" + json[startValueIndex] + "\"! Quotation mark expected!");
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
