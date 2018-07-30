using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Value type JSON structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public sealed class StructureValueType : AbstractStructure
    {
        #region StructureValueType fields
        // ----------------------------------------------------------------------------------------
        // StructureValueType fields
        // ----------------------------------------------------------------------------------------

        private Type type;
        private bool useCultureSensitiveConvert;
        private bool isNullableType;
        private Type underlyingNullableType;
        private IJsonTypeStructure nullableUnderlyingTypeConverter;
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureValueType constructors
        // ----------------------------------------------------------------------------------------
        // StructureValueType constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureValueType"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        public StructureValueType(string key, Type type, bool isArrayItem)
            : base(key, isArrayItem)
        {
            this.type = type;
            this.useCultureSensitiveConvert = typeof(double).Equals(type)
                                        || typeof(decimal).Equals(type)
                                        || typeof(Single).Equals(type);

            this.underlyingNullableType = Nullable.GetUnderlyingType(type);
            this.isNullableType = underlyingNullableType != null;
            if (isNullableType)
            {
                this.nullableUnderlyingTypeConverter = Structure.DetermineStructure(underlyingNullableType, key, null, isArrayItem);
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureValueType properties
        // ----------------------------------------------------------------------------------------
        // StructureValueType properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureValueType methods
        // ----------------------------------------------------------------------------------------
        // StructureValueType methods
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (isNullableType)
            {
                if (obj == null)
                {
                    if (keyExpected)
                    {
                        sb.Append(Structure.QuotationMark);
                        sb.Append(Key);
                        sb.Append(Structure.QuotationColonNullValue);
                    }
                }
                else
                {
                    nullableUnderlyingTypeConverter.Serialize(sb, obj, context);
                }
            }
            else
            {
                if (keyExpected)
                {
                    sb.Append(Structure.QuotationMark);
                    sb.Append(Key);
                    sb.Append(Structure.QuotationColonSeparator);
                }

                if (useCultureSensitiveConvert)
                {
                    sb.Append(Convert.ChangeType(obj, typeof(string), CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(obj);
                }
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
            int endValueIndex = json.IndexOfAny(Structure.EndValueChars, startValueIndex);

            currentReadIndex = endValueIndex;

            string stringValue = json.Substring(startValueIndex, endValueIndex - startValueIndex);

            if (isNullableType)
            {
                if (stringValue == Structure.NullValue)
                {
                    return null;
                }
                else
                {
                    // reset start index
                    currentReadIndex = startValueIndex - keyLength;
                    return nullableUnderlyingTypeConverter.Deserialize(json, ref currentReadIndex, context);
                }
            }
            else
            {
                return Convert.ChangeType(stringValue, type, CultureInfo.InvariantCulture);
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }


}
