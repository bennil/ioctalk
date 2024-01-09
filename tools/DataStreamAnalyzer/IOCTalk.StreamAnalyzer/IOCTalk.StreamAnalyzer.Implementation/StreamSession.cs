using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IOCTalk.StreamAnalyzer.Implementation
{
    /// <summary>
    /// Specifies an IOC-Talk data stream session.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 11/24/2015 9:58:06 AM.
    ///  </para>
    /// </remarks>
    public class StreamSession
    {
        #region fields


        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>StreamSession</c>.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="sessionInfo">The session info.</param>
        public StreamSession(int sessionId, string sessionInfo, TimeSpan createdAt, RawMessageFormat format)
        {
            this.SessionId = sessionId;

            string descrPattern = "Description";
            int descrIndex = sessionInfo.IndexOf(descrPattern);
            if (descrIndex > 0)
            {
                string shortVersion = sessionInfo.Substring(descrIndex + descrPattern.Length + 1);
                sessionInfo = sessionInfo.Substring(18, descrIndex - 18) + shortVersion;
            }

            this.SessionInfo = sessionInfo;
            this.IncomingSyncCalls = new Dictionary<long, MethodInvokeRoundtrip>();
            this.OutgoingSyncCalls = new Dictionary<long, MethodInvokeRoundtrip>();
            this.FlowRates = new List<FlowRate>();
            this.CreatedAt = createdAt;
            this.Format = format;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public int SessionId { get; private set; }

        /// <summary>
        /// Gets the session info.
        /// </summary>
        public string SessionInfo { get; private set; }


        /// <summary>
        /// Gets the incoming synchronous calls.
        /// </summary>
        public IDictionary<long, MethodInvokeRoundtrip> IncomingSyncCalls { get; private set; }

        /// <summary>
        /// Gets the outgoing synchronous calls.
        /// </summary>
        public IDictionary<long, MethodInvokeRoundtrip> OutgoingSyncCalls { get; private set; }


        /// <summary>
        /// Gets the flow rates.
        /// </summary>
        /// <value>The flow rates.</value>
        public List<FlowRate> FlowRates { get; private set; }

        public long TotalPayloadByteCount { get; set; }

        public double TotalPayloadMegabytes => (TotalPayloadByteCount / 1024d) / 1024d;

        /// <summary>
        /// Gets or sets the pending flow rate.
        /// </summary>
        /// <value>The pending flow rate.</value>
        internal FlowRate PendingFlowRate { get; set; }


        /// <summary>
        /// Gets or sets the incoming sync call count.
        /// </summary>
        /// <value>
        /// The incoming sync call count.
        /// </value>
        public int IncomingSyncCallCount { get; set; }

        /// <summary>
        /// Gets or sets the outgoing sync call count.
        /// </summary>
        /// <value>
        /// The outgoing sync call count.
        /// </value>
        public int OutgoingSyncCallCount { get; set; }

        public TimeSpan? OutgoingSyncCallMinDuration { get; set; }
        public TimeSpan? OutgoingSyncCallMaxDuration { get; set; }
        public TimeSpan? OutgoingSyncCallAvgDuration => OutgoingSyncCallTotalDuration > TimeSpan.Zero ? OutgoingSyncCallTotalDuration / OutgoingSyncCallCount : null;
        public TimeSpan OutgoingSyncCallTotalDuration { get; set; } = TimeSpan.Zero;


        /// <summary>
        /// Gets or sets the incoming async call count.
        /// </summary>
        /// <value>
        /// The incoming async call count.
        /// </value>
        public int IncomingAsyncCallCount { get; set; }

        /// <summary>
        /// Gets or sets the outgoing async call count.
        /// </summary>
        /// <value>
        /// The outgoing async call count.
        /// </value>
        public int OutgoingAsyncCallCount { get; set; }


        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        /// <value>
        /// The created at.
        /// </value>
        public TimeSpan CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the terminated at.
        /// </summary>
        /// <value>
        /// The terminated at.
        /// </value>
        public TimeSpan? TerminatedAt { get; set; }


        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public RawMessageFormat Format { get; set; }


        /// <summary>
        /// Gets or sets the last send request identifier.
        /// </summary>
        /// <value>The last send request identifier.</value>
        public long LastSendRequestId { get; set; }

        /// <summary>
        /// Gets or sets the last receive request identifier.
        /// </summary>
        /// <value>The last receive request identifier.</value>
        public long LastReceiveRequestId { get; set; }

        #endregion

        #region methods

        #endregion
    }
}
