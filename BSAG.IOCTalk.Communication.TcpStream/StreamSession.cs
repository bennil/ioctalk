using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Serialization.Binary.Stream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Communication.TcpStream
{
    public class StreamSession : Session
    {
        public StreamSession(IGenericCommunicationService communicationService, int sessionId, string description, IMessageStreamSerializer streamSerializer)
            : base(communicationService, sessionId, description)
        {
            this.StreamSerializer = streamSerializer;
            this.Reader = new StreamReader(new byte[0]);
            this.Writer = new StreamWriter(256);
        }

        public IMessageStreamSerializer StreamSerializer { get; private set; }


        public StreamReader Reader { get; private set; }

        public StreamWriter Writer { get; private set; }
    }
}
