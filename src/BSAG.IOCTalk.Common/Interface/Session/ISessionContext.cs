using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Session
{
    public interface ISessionContext
    {
        IGenericCommunicationService CommunicationService { get; }

        ISession Session { get; }
    }
}
