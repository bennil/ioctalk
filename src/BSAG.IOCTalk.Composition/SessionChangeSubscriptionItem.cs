using BSAG.IOCTalk.Common.Interface.Session;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Composition
{
    internal class SessionChangeSubscriptionItem
    {
        public SessionChangeSubscriptionItem(Delegate sessionDelegate, ParameterInfo[] parameters, ISession targetSessionOnlyContext)
        {
            Callback = sessionDelegate;
            Parameters = parameters;
            TargetSessionOnlyContext = targetSessionOnlyContext;
        }

        public Delegate Callback { get; private set; }

        public ParameterInfo[] Parameters { get; private set; }

        /// <summary>
        /// Gets the target callback session only context.
        /// This means the target delegate lifetime of this "session changed subscription" is also limited to the actual session - lifetime wise.
        /// It's used if a local interface implementation is registered as session instance and subscribes (over constructor out) a session created event also bound to the same session related context.
        /// </summary>
        public ISession TargetSessionOnlyContext { get; private set; }
    }
}
