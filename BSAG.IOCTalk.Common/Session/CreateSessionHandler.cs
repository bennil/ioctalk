using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Session
{
    /// <summary>
    /// Handler to create a new session object.
    /// </summary>
    /// <param name="communicationSerivce">The communication serivce.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    public delegate ISession CreateSessionHandler(IGenericCommunicationService communicationSerivce, int sessionId, string description);

}
