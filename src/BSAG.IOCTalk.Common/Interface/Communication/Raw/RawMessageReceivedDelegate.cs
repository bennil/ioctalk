using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Common.Interface.Communication.Raw
{
    /// <summary>
    /// Delegate to process received raw messages.
    /// The raw message instance will be reused after the call.
    /// </summary>
    /// <param name="rawMessage">The raw message.</param>
    public delegate ValueTask RawMessageReceivedDelegate(IRawMessage rawMessage);
}
