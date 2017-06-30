using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Integer JSON type structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public class StructureInt : AbstractStructure
    {
        #region StructureInt fields
        // ----------------------------------------------------------------------------------------
        // StructureInt fields
        // ----------------------------------------------------------------------------------------

        private const char MinusChar = '-';
        private const char PlusChar = '+';
        private const char BlankChar = ' ';
        private const char MinNumChar = '0';
        private const char MaxNumChar = '9';

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureInt constructors
        // ----------------------------------------------------------------------------------------
        // StructureInt constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureInt</c> class.
        /// </summary>
        public StructureInt(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureInt properties
        // ----------------------------------------------------------------------------------------
        // StructureInt properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureInt methods
        // ----------------------------------------------------------------------------------------
        // StructureInt methods
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(Key);
                sb.Append(Structure.QuotationColonSeparator);
            }
            sb.Append(obj);
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
            int endValueIndex = json.IndexOfAny(Structure.EndValueChars, startValueIndex);

            currentReadIndex = endValueIndex;

            string stringValue = json.Substring(startValueIndex, endValueIndex - startValueIndex);

            int result = 0;
            int startIndex = 0;
            if (stringValue.Length > 0)
            {
                if (stringValue[0] == BlankChar)
                {
                    // remove start blanks
                    startIndex++;

                    for (int i = 1; i < stringValue.Length; i++)
                    {
                        if (stringValue[i] == BlankChar)
                        {
                            startIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (stringValue[startIndex] == MinusChar)
                {
                    for (int i = startIndex + 1; i < stringValue.Length; i++)
                    {
                        char c = stringValue[i];

                        if (c == BlankChar)
                            continue;   // ignore trailing blanks

                        if (c < MinNumChar || c > MaxNumChar)
                            throw new FormatException($"Unable to cast the value \"{stringValue}\" to Int32!");

                        result = result * 10 - (stringValue[i] - '0');
                    }
                }
                else
                {
                    int i = startIndex;
                    if (stringValue[startIndex] == PlusChar)
                    {
                        i = startIndex + 1;
                    }

                    for (; i < stringValue.Length; i++)
                    {
                        char c = stringValue[i];

                        if (c == BlankChar)
                            continue;   // ignore trailing blanks

                        if (c < MinNumChar || c > MaxNumChar)
                            throw new FormatException($"Unable to convert the string \"{stringValue}\" to Int32!");

                        result = result * 10 + (c - '0');
                    }
                }
            }
            else
            {
                throw new FormatException($"Unable to convert an empty string to Int32!");
            }
            return result;
        }
        // ----------------------------------------------------------------------------------------
        #endregion

     
    }

}
