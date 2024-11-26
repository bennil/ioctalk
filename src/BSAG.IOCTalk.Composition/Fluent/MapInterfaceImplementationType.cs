using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class MapInterfaceImplementationType<InterfaceType>
    {
        LocalShareContext source;
        TypeHierachy typeHierachy;
        internal MapInterfaceImplementationType(LocalShareContext source, TypeHierachy typeHierachy)
        {
            this.source = source;
            this.typeHierachy = typeHierachy;
        }

        public MapInterfaceImplementationType<InterfaceType> MapAdditionalMultiImportImplementation<InterceptImplementation>()
            where InterceptImplementation : class, InterfaceType
        {
            if (source.IsMultiShared(typeof(InterfaceType)) == false)
                throw new InvalidOperationException($"The interface {typeof(InterfaceType).FullName} is not registered for various implementations. Did you miss a {nameof(LocalShareContext.RegisterLocalSharedServices)} call?");

            typeHierachy.AddBreakoutType(typeof(InterceptImplementation));

            return this;
        }
    }
}
