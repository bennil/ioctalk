using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    public interface IDiscoveryContext
    {
        /// <summary>
        /// Gets the requested (interface) type
        /// </summary>
        Type RequestType { get; }

        /// <summary>
        /// Gets the target injection class
        /// </summary>
        Type InjectTargetType { get; }
    }
}
