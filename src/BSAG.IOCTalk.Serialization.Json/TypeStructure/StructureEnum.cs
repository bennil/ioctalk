using BSAG.IOCTalk.Common.Reflection;
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
        Type enumType;
        Type underlyingEnumType;
        bool isDefaultUnderlyingType;
        IJsonTypeStructure intEnumSerializer;
        IJsonTypeStructure otherUnderlyingTypeSerializer;
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
            this.underlyingEnumType = Enum.GetUnderlyingType(enumType);
            this.intEnumSerializer = new StructureInt(key, isArrayItem);

            this.isDefaultUnderlyingType = underlyingEnumType.Equals(typeof(int));
            if (isDefaultUnderlyingType == false)
            {
                otherUnderlyingTypeSerializer = Structure.DetermineStructure(underlyingEnumType, key, null, isArrayItem);
            }
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
            if (isDefaultUnderlyingType)
            {
                if (keyExpected)
                {
                    sb.Append(Structure.QuotationMark);
                    sb.Append(Key);
                    sb.Append(Structure.QuotationColonSeparator);
                }

                sb.Append((int)obj);
            }
            else
            {
                object underlyingNumericValueType = Convert.ChangeType(obj, underlyingEnumType);
                otherUnderlyingTypeSerializer.Serialize(sb, underlyingNumericValueType, context);
                //sb.Append(underlyingEnumType);
            }
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

            // Enum number value expected
            if (isDefaultUnderlyingType)
            {
                int enumNumberValue = (int)intEnumSerializer.Deserialize(json, ref currentReadIndex, context);
                return Enum.ToObject(enumType, enumNumberValue);
            }
            else
            {

                object otherTypeNumber = otherUnderlyingTypeSerializer.Deserialize(json, ref currentReadIndex, context);
                return Enum.ToObject(enumType, otherTypeNumber);
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
