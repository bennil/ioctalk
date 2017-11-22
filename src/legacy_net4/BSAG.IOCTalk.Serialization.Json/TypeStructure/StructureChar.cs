using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Character JSON structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-08-01
    /// </remarks>
    public sealed class StructureChar : AbstractStructure
    {
        #region StructureChar fields
        // ----------------------------------------------------------------------------------------
        // StructureChar fields
        // ----------------------------------------------------------------------------------------

        private const string EscapeBeginString = "\\u";


        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureChar constructors
        // ----------------------------------------------------------------------------------------
        // StructureChar constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureChar"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isArrayItem"></param>
        public StructureChar(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureChar properties
        // ----------------------------------------------------------------------------------------
        // StructureChar properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureChar methods
        // ----------------------------------------------------------------------------------------
        // StructureChar methods
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(Key);
                sb.Append(Structure.QuotationColonSeparator);
            }

            char c = (char)obj;


            sb.Append(Structure.QuotationMark);
            if (char.IsControl(c))
            {
                // Escape special char
                int charIntVal = (int)c;

                sb.Append(EscapeBeginString);
                sb.Append(charIntVal.ToString("0000"));
            }
            else
            {
                sb.Append(c);
            }
            sb.Append(Structure.QuotationMark);
        }



        /// <summary>
        /// Deserializes the specified json string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
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

                if (stringValue.StartsWith(EscapeBeginString))
                {
                    string charIntValueStr = stringValue.Substring(EscapeBeginString.Length, 4);
                    int charIntValue = int.Parse(charIntValueStr);

                    return (char)charIntValue;
                }
                else if (stringValue.Length == 1)
                {
                    return stringValue[0];
                }
                else if (stringValue.Length == 0)
                {
                    return default(char);
                }
                else
                {
                    throw new InvalidCastException("Invalid char value: \"" + stringValue + "\"!");
                }
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
