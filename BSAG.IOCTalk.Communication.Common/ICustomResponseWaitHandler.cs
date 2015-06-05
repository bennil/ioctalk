using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Communication.Common
{
    /// <summary>
    /// Custom response wait handler
    /// </summary>
    public interface ICustomResponseWaitHandler
    {
        /// <summary>
        /// Waits for response.
        /// </summary>
        /// <param name="state">The state.</param>
        void WaitForResponse(InvokeState state);
    }
}
