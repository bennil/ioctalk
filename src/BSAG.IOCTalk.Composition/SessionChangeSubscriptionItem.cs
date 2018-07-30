using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Composition
{
    internal class SessionChangeSubscriptionItem
    {
        public SessionChangeSubscriptionItem(Delegate sessionDelegate, ParameterInfo[] parameters)
        {
            Callback = sessionDelegate;
            Parameters = parameters; 
        }

        public Delegate Callback { get; set; }

        public ParameterInfo[] Parameters { get; set; }
    }
}
