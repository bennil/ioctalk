using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Specifies the supported invoke thread models on the receiver side.
    /// </summary>
    public enum InvokeThreadModel
    {
        /// <summary>
        /// One caller thread for sequential method invokes will be created for each communication interface (service or client).
        /// If a call awaits a remote invoke response other pending calls will be processed in the meantime. This prevents a deadlock if the remote method executes nested synchronous back calls.
        /// </summary>
        CallerThread = 0,
        
        /// <summary>
        /// Method calls will be processed in the receiver thread (depending on the threading model and the dependency between threads this can provoke deadlocks. Especially in nested both side calls.).
        /// </summary>
        ReceiverThread = 1,

        /// <summary>
        /// Every method call will be processed in a separate task (thread).
        /// </summary>
        IndividualTask = 2,
    }
}
