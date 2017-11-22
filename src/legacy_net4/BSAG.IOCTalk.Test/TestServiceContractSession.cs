using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Test.Common;
using System.ComponentModel.Composition;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Test
{
    public class TestServiceContractSession : ISessionStateChanged
    {
        public TestServiceContractSession()
        {
        }


        /// <summary>
        /// Gets or sets the hello world service.
        /// </summary>
        /// <value>
        /// The hello world service.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IHelloWorldService HelloWorldService { get; set; }


        /// <summary>
        /// Gets or sets the performance monitor service.
        /// </summary>
        /// <value>
        /// The performance monitor service.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IPerformanceMonitorService PerformanceMonitorService { get; set; }

        /// <summary>
        /// Gets or sets the performance monitor client notification.
        /// </summary>
        /// <value>
        /// The performance monitor client notification.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IPerformanceMonitorClientNotification PerformanceMonitorClientNotification { get; set; }


        /// <summary>
        /// Gets or sets the client message notification.
        /// </summary>
        /// <value>
        /// The client message notification.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IClientMessageNotification ClientMessageNotification { get; set; }


        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        /// <value>
        /// The session.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public ISession Session { get; set; }



        public void OnSessionCreated(ISession session)
        {
        }

        public void OnSessionTerminated(ISession session)
        {
        }
    }
}
