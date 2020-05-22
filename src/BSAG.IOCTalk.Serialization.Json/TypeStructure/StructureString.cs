using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// String JSON type structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public class StructureString : AbstractStructure
    {
        #region StructureString fields
        // ----------------------------------------------------------------------------------------
        // StructureString fields
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureString constructors
        // ----------------------------------------------------------------------------------------
        // StructureString constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureString"/> class.
        /// </summary>
        /// <param name="key"></param>
        public StructureString(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureString properties
        // ----------------------------------------------------------------------------------------
        // StructureString properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureString methods
        // ----------------------------------------------------------------------------------------
        // StructureString methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Serializes the specified sb.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(Key);
                if (obj == null)
                {
                    sb.Append(Structure.QuotationColonNullValue);
                }
                else
                {
                    sb.Append(Structure.QuotationColonQuotationSeparator);
                    AppendString(sb, obj);
                    sb.Append(Structure.QuotationMark);
                }
            }
            else
            {
                sb.Append(Structure.QuotationMark);
                AppendString(sb, obj);
                sb.Append(Structure.QuotationMark);
            }
        }

        private static void AppendString(StringBuilder sb, object obj)
        {
            if (obj is string)
            {
                string stringValue = (string)obj;

                // add escape chars
                stringValue = stringValue.Replace(Structure.QuotationMark, Structure.QuotationMarkEscaped);

                sb.Append(stringValue);
            }
            else
            {
                sb.Append(obj);
            }
        }


        /// <summary>
        /// Deserializes the specified json.
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
                bool replaceEscapeChars = false;
                while (json[endValueIndex - 1] == Structure.CharEscape)
                {
                    // escaped quotation mark

                    // check if value ends with the escape char
                    if (json[endValueIndex + 1] == Structure.CharRightBrace
                        || (json[endValueIndex + 1] == Structure.CharComma
                            && json.Length > endValueIndex + 3
                            && json[endValueIndex + 2] == Structure.CharQuotationMark
                            )
                       )
                    {
                        break;  // end reached skip replacement
                        //todo: check deserialize problems -> escape \ as well?
                    }

                    // read further to find string ending
                    endValueIndex = json.IndexOf(Structure.QuotationMark, endValueIndex + 1);

                    replaceEscapeChars = true;
                }

                currentReadIndex = endValueIndex + 1;

                string stringValue = json.Substring(startValueIndex, endValueIndex - startValueIndex);

                if (replaceEscapeChars)
                {
                    stringValue = stringValue.Replace(Structure.QuotationMarkEscaped, Structure.QuotationMark);
                }

                return stringValue;
            }
            else if (json[startValueIndex] == 'n'
                    && json[startValueIndex + 1] == 'u'
                    && json[startValueIndex + 2] == 'l'
                    && json[startValueIndex + 3] == 'l')
            {
                currentReadIndex = startValueIndex + 4;
                return null;
            }
            else
            {
                throw new InvalidOperationException("Unexptected JSON String value!");
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }


}
