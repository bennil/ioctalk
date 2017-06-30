using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication.Raw
{
    /// <summary>
    /// Defines the raw message formats
    /// </summary>
    public enum RawMessageFormat
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// JSON format (UTF-8 encoded)
        /// </summary>
        JSON = 1,

        /// <summary>
        /// Binary format (similar to Microsoft Bond)
        /// </summary>
        Binary = 2,

        /// <summary>
        /// Incomplete encapsulation control data slice
        /// </summary>
        IncompleteControlDataSlice = 100,
    }
}
