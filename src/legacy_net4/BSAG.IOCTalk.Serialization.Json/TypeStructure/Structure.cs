using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// JSON Structure helper methods
    /// </summary>
    public static class Structure
    {
        /// <summary>
        /// Colon separator :
        /// </summary>
        public const string ColonSeparator = ":";
        /// <summary>
        /// Quotation mark
        /// </summary>
        public const string QuotationMark = "\"";
        public const string QuotationMarkEscaped = "\\\"";
        public const string QuotationColonSeparator = "\":";
        public const string QuotationColonQuotationSeparator = "\":\"";
        public const string Comma = ",";
        public const string NullValue = "null";
        public const string QuotationColonNullValue = "\":null";
        public const string TypeMetaTagKey = "#type";
        public const string TypeMetaTagJson = "\"#type\":\"";

        public const char CharQuotationMark = '\"';
        public const char CharLeftBrace = '{';
        public const char CharRightBrace = '}';
        public const char CharLeftSquareBrace = '[';
        public const char CharRightSquareBracet = ']';
        public const char CharColon = ':';
        public const char CharComma = ',';
        public const char CharDot = '.';
        public const char CharEscape = '\\';

        public readonly static char[] EndValueChars = new char[] { ',', '}', ']' };

        /// <summary>
        /// Determines the type structure.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="key">The key.</param>
        /// <param name="context">The context.</param>
        /// <param name="isArrayItem">if set to <c>true</c> [is array item].</param>
        /// <returns></returns>
        public static IJsonTypeStructure DetermineStructure(Type type, string key, SerializationContext context, bool isArrayItem)
        {
            IJsonTypeStructure result;
            if (type.Equals(typeof(string)))
            {
                result = new StructureString(key, isArrayItem);
            }
            else if (type.IsValueType)
            {
                if (type.Equals(typeof(int)))
                {
                    result = new StructureInt(key, isArrayItem);
                }
                else if (type.IsEnum)
                {
                    result = new StructureEnum(key, type, isArrayItem);
                }
                else if (type.Equals(typeof(bool)))
                {
                    result = new StructureBool(key, isArrayItem);
                }
                else if (type.Equals(typeof(TimeSpan)))
                {
                    result = new StructureTimeSpan(key, isArrayItem);
                }
                else if (type.Equals(typeof(DateTime)))
                {
                    result = new StructureDateTime(key, isArrayItem);
                }
                else if (type.Equals(typeof(char)))
                {
                    result = new StructureChar(key, isArrayItem);
                }
                else if (type.Equals(typeof(Guid)))
                {
                    result = new StructureGuid(key, isArrayItem);
                }
                else
                {
                    result = new StructureValueType(key, type, isArrayItem);
                }
            }
            else if (type.IsArray
                    || type.GetInterface("IEnumerable") != null)
            {
                result = new StructureArray(key, type, context, isArrayItem);
            }
            else
            {
                result = new StructureComplexObject(type, key, context, isArrayItem);
            }

            return result;
        }

        /// <summary>
        /// Gets the default type.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="valueStartIndex">Start index of the value.</param>
        /// <returns></returns>
        public static Type GetDefaultType(string json, int valueStartIndex)
        {
            switch (json[valueStartIndex])
            {
                case Structure.CharQuotationMark:
                    // string value
                    return typeof(string);

                case Structure.CharLeftBrace:
                    // Unable to determine complex object default type
                    return null;

                case Structure.CharLeftSquareBrace:
                    return typeof(List<object>);

                default:
                    // value type resolution
                    int endValueIndex = json.IndexOfAny(Structure.EndValueChars, valueStartIndex);

                    string value = json.Substring(valueStartIndex, endValueIndex - valueStartIndex);
                    int dotIndex = value.IndexOf(Structure.CharDot);
                    if (dotIndex > 0)
                    {
                        // decimal type
                        int precision = value.Length - dotIndex - 1;
                        if (precision > 4
                            || value.Length > 16)
                        {
                            return typeof(decimal);
                        }
                        else
                        {
                            return typeof(double);
                        }
                    }
                    else if (value == Structure.NullValue)
                    {
                        return null;
                    }
                    else if (value == StructureBool.TrueString
                            || value == StructureBool.FalseString)
                    {
                        return typeof(bool);
                    }
                    else if (value.Length > 7)
                    {
                        // long
                        return typeof(long);
                    }
                    else
                    {
                        // int
                        return typeof(int);
                    }

            }
        }



        /// <summary>
        /// Serializes null value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sb">The sb.</param>
        internal static void SerializeNull(string key, StringBuilder sb)
        {
            if (key == null)
            {
                sb.Append(Structure.NullValue);
            }
            else
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(key);
                sb.Append(Structure.QuotationColonNullValue);
            }
        }




    }
}
