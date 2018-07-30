using System.Xml.Linq;

namespace BSAG.IOCTalk.Common.Interface.Config
{
    /// <summary>
    /// Interface provides a XML configuration.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 3/25/2015 1:53:52 PM.
    ///  </para>
    /// </remarks>
    public interface IXmlConfig
    {
        #region properties

        /// <summary>
        /// Gets or sets the xml configuration.
        /// </summary>
        XDocument Config { get; set; }

        #endregion properties
    }
}