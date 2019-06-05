using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class RemoteServiceRegistration<InterfaceType>
    {
        private Type interfaceType;
        private TalkCompositionHost source;

        public RemoteServiceRegistration(TalkCompositionHost source)
        {
            this.interfaceType = typeof(InterfaceType);
            this.source = source;            
        }

        public RemoteServiceRegistration<InterfaceType> AsAsync(string methodName)
        {
            //todo: handle / include command parameters
            source.RegisterAsyncMethod<InterfaceType>(methodName);

            return this;
        }

    }
}
