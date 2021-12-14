using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Communication.Common
{
    /// <summary>
    /// Common communication interface
    /// </summary>
    public interface ICommunicationBaseServiceSupport : IGenericCommunicationService
    {
        /// <summary>
        /// Sends the generic message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="receiverSessionId">The receiver session id.</param>
        /// <param name="context">The context.</param>
        void SendMessage(IGenericMessage message, int receiverSessionId, object context);

        /// <summary>
        /// Sends the generic message async.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="receiverSessionId">The receiver session id.</param>
        /// <param name="context">The context.</param>
        ValueTask SendMessageAsync(IGenericMessage message, int receiverSessionId, object context);

        /// <summary>
        /// Determines whether [is async send currently possible] depending on the current load situation.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>
        ///   <c>true</c> if [is async send currently possible] [the specified session]; otherwise, <c>false</c>.
        /// </returns>
        bool IsAsyncVoidSendCurrentlyPossible(ISession session);
    }
}
