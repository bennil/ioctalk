using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;

namespace BSAG.IOCTalk.Test.Common
{
    public interface IClientMessageNotification
    {
        /// <summary>
        /// Notifies the message.
        /// </summary>
        /// <param name="clientSession">The client session (requires a BSAG.IOCTalk.Common reference).</param>
        /// <param name="message">The message.</param>
        void NotifyMessage(string message);
    }
}
