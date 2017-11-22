using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Enumeration JSON type structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public class StructureEnum : AbstractStructure
    {
        #region StructureEnum fields
        // ----------------------------------------------------------------------------------------
        // StructureEnum fields
        // ----------------------------------------------------------------------------------------
        private Type enumType;
        //private IJsonTypeStructure stringEnumSerializer;
        private IJsonTypeStructure intEnumSerializer;
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureEnum constructors
        // ----------------------------------------------------------------------------------------
        // StructureEnum constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureEnum</c> class.
        /// </summary>
        public StructureEnum(string key, Type enumType, bool isArrayItem)
            : base(key, isArrayItem)
        {
            this.enumType = enumType;
            this.intEnumSerializer = new StructureInt(key, isArrayItem);
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureEnum properties
        // ----------------------------------------------------------------------------------------
        // StructureEnum properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureEnum methods
        // ----------------------------------------------------------------------------------------
        // StructureEnum methods
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
            sb.Append((int)obj);
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

            //if (char.IsNumber(json[startValueIndex]))     // string compatibility removed because of performance reasons
            //{
                // Enum number value
                int enumNumberValue = (int)intEnumSerializer.Deserialize(json, ref currentReadIndex, context);
                return Enum.ToObject(enumType, enumNumberValue);
            //}
            //else
            //{
            //    // String enum representation
            //    if (stringEnumSerializer == null)
            //        stringEnumSerializer = Structure.DetermineStructure(typeof(int), this.key, null, this.isArrayItem);

            //    string enumString = (string)stringEnumSerializer.Deserialize(json, ref currentReadIndex, context);

            //    return Enum.Parse(enumType, enumString);
            //}
        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
