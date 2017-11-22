using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Boolean JSON structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public class StructureBool : AbstractStructure
    {
        #region StructureBool fields
        // ----------------------------------------------------------------------------------------
        // StructureBool fields
        // ----------------------------------------------------------------------------------------

        private const char TrueStartChar = 't';
        private const char TrueStartCharUpperCase = 'T';

        private const char FalseStartChar = 'f';
        private const char FalseStartCharUpperCase = 'F';

        public const string TrueString = "true";
        public const string FalseString = "false";

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureBool constructors
        // ----------------------------------------------------------------------------------------
        // StructureBool constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureBool</c> class.
        /// </summary>
        /// <param name="key"></param>
        public StructureBool(string key, bool isArrayItem)
            : base(key, isArrayItem)
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureBool properties
        // ----------------------------------------------------------------------------------------
        // StructureBool properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureBool methods
        // ----------------------------------------------------------------------------------------
        // StructureBool methods
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            bool value = (bool)obj;

            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(key);
                sb.Append(Structure.QuotationColonSeparator);
            }

            if (value)
            {
                sb.Append(TrueString);
            }
            else
            {
                sb.Append(FalseString);
            }
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

            char startBoolValueChar = json[startValueIndex];


            switch (startBoolValueChar)
            {
                case TrueStartChar:
                case TrueStartCharUpperCase:
                    currentReadIndex = startValueIndex + 4;  // jump over value

                    return true;

                case FalseStartChar:
                case FalseStartCharUpperCase:
                    currentReadIndex = startValueIndex + 5;  // jump over value

                    return false;

                default:
                    throw new InvalidOperationException(string.Format("Unexpected boolean value! First character: \"{0}\"", startBoolValueChar));
            }

        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
