using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.Common
{
    public interface IPerformanceData
    {
        /// <summary>
        /// Gets the measure type.
        /// </summary>
        MeasureType Type { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        decimal Value { get; set; }
        
        /// <summary>
        /// Gets or sets the unity.
        /// </summary>
        /// <value>
        /// The unit.
        /// </value>
        string Unity { get; set; }
    }
}
