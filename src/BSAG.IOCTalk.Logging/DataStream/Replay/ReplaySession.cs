using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Session;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Logging.DataStream.Replay
{
    public class ReplaySession : Session
    {
        public ReplaySession(IGenericCommunicationService communicationService, int sessionId, string description, object underlyingCommunicationObject) : base(communicationService, sessionId, description, underlyingCommunicationObject)
        {
        }
    }
}
