using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Xml.Linq;

namespace BSAG.IOCTalk.Communication.Common.Factory
{
    /// <summary>
    /// App config section parser
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 1/23/2015 4:04:26 PM.
    ///  </para>
    /// </remarks>
    public class AppConfigSection : IConfigurationSectionHandler
    {
        #region fields


        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>AppConfigSection</c>.
        /// </summary>
        public AppConfigSection()
        {
        }

        #endregion

        #region properties
        #endregion

        #region methods

        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        /// <param name="parent">Parent object.</param>
        /// <param name="configContext">Configuration context object.</param>
        /// <param name="section">Section XML node.</param>
        /// <returns>
        /// Returns the configuration section XML as XDocument object.
        /// </returns>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            return XDocument.Parse(section.OuterXml);
        }

        #endregion
    }
}
