using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace BSAG.IOCTalk.Communication.Tcp.Utils
{
    /// <summary>
    /// XML configuration helper
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 12/16/2014 11:17:33 AM.
    ///  </para>
    /// </remarks>
    public static class XmlConfigHelper
    {
        #region fields


        #endregion

        #region methods



        /// <summary>
        /// Gets the config parameter value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paramPath">The param path.</param>
        /// <returns></returns>
        public static T GetConfigParameterValue<T>(this XElement xmlElement, params string[] paramPath)
        {
            XElement lastElement = xmlElement;
            foreach (var param in paramPath)
            {
                var configElement = lastElement.Element(param);

                if (configElement == null)
                {
                    throw new KeyNotFoundException(string.Format("The TCP configuration XML Element \"{0}\" was not found in \"{1}\"", param, lastElement));
                }
                else
                {
                    lastElement = configElement;
                }
            }

            return (T)GetTargetTypeValue(lastElement.Value, typeof(T));
        }


        /// <summary>
        /// Gets the target type value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="desiredType">Type of the desired.</param>
        /// <returns></returns>
        public static object GetTargetTypeValue(object value, Type desiredType)
        {
            if (!desiredType.IsAssignableFrom(value.GetType()))
            {
                var underlyingType = Nullable.GetUnderlyingType(desiredType);
                if (underlyingType != null)
                {
                    // handle nullable types
                    if (value == null || value.ToString().Length == 0)
                    {
                        value = null;
                    }
                    else
                    {
                        value = GetTargetTypeValue(value, underlyingType);
                    }
                }
                else
                {
                    if (desiredType.Equals(typeof(TimeSpan)))
                    {
                        value = TimeSpan.Parse(value.ToString());
                    }
                    else if (desiredType.IsEnum)
                    {
                        string strEnumValue = value.ToString();

                        short enumShortValue;
                        if (short.TryParse(strEnumValue, out enumShortValue))
                        {
                            value = Enum.ToObject(desiredType, enumShortValue);
                        }
                        else
                        {
                            value = Enum.Parse(desiredType, strEnumValue);
                        }
                    }
                    else if (desiredType.Equals(typeof(bool)))
                    {
                        value = Boolean.Parse(value.ToString().ToLower());
                    }
                    else
                    {
                        value = Convert.ChangeType(value, desiredType, CultureInfo.InvariantCulture);
                    }
                }
            }

            return value;
        }
        #endregion
    }
}
