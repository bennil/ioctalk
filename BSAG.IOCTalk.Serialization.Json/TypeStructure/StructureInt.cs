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

            return int.Parse(stringValue);
        }
        // ----------------------------------------------------------------------------------------
        #endregion

     
    }

}
