using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.Communication.WebSocketFraming
{
    /// <summary>
    /// !!!! This is only a copy of the orignal in BSAG.IOCTalk.Communication.NetTcp
    /// todo: move to BSAG.IOCTalk.Communication.Common if library is upgraded to .net standard 2.1
    /// </summary>
    public abstract class AbstractWireFraming
    {
        public virtual int MaxMessageSize { get; set; } = 10_000_000;     // 10 MB max

        public abstract bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messagePayload, out RawMessageFormat rawMessageFormat);


        public abstract void CreateTransportMessageStart(IBufferWriter<byte> writer);

        public abstract void CreateTransportMessageEnd(IBufferWriter<byte> writer, int payloadSize, Span<byte> startMessageRef);


        public ILogger Logger { get; set; }


        public abstract void Init(IGenericCommunicationService parent);
    }
}
