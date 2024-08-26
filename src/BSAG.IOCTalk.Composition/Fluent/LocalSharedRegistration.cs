using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSharedRegistration<InterfaceType>
    {
        LocalShareContext source;

        public LocalSharedRegistration(LocalShareContext source)
        {
            this.source = source;
        }

        public LocalSharedRegistration<InterfaceType> InterceptWithImplementation<InterceptImplementation>()
            where InterceptImplementation : class, InterfaceType
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));
            
            typeHierachy.AddInterceptionType(typeof(InterceptImplementation));

            return this;
        }
    }
}
