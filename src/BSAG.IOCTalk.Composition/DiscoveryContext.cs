using BSAG.IOCTalk.Common.Interface.Container;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition
{
    public class DiscoveryContext : IDiscoveryContext
    {
        private Type requestType;
        private Type injectTargetType;

        public DiscoveryContext(Type requestType, Type injectTargetType)
        {
            this.requestType = requestType;
            this.injectTargetType = injectTargetType;
        }

        public Type RequestType => requestType;

        public Type InjectTargetType => injectTargetType;
    }
}
