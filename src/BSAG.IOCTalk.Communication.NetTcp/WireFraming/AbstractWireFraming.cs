using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Communication.NetTcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Communication.NetTcp.WireFraming
{
    public abstract class AbstractWireFraming
    {
        public virtual int MaxMessageSize { get; set; } = 10_000_000;     // 10 MB max

        public abstract bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messagePayload, out RawMessageFormat rawMessageFormat);


        public abstract void CreateTransportMessageStart(IBufferWriter<byte> writer);

        public abstract void CreateTransportMessageEnd(IBufferWriter<byte> writer, int payloadSize, Span<byte> startMessageRef);


        internal ILogger Logger { get; set; }


        internal abstract void Init(IGenericCommunicationService parent);
    }
}
