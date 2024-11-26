using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSharedRegistration<InterfaceType>
    {
        protected LocalShareContext source;

        internal LocalSharedRegistration(LocalShareContext source)
        {
            this.source = source;
        }

        /// <summary>
        /// Intercepts the service registration (circuit breaker)
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSharedRegistrationIntercept<InterfaceType> InterceptWithImplementation<InterceptImplementation>()
            where InterceptImplementation : class, InterfaceType
        {
            return InterceptWithImplementationInternal(typeof(InterceptImplementation));
        }

        public LocalSharedRegistrationIntercept<InterfaceType> InterceptWithImplementation(Type implementationType)
        {
            if (typeof(InterfaceType).IsAssignableFrom(implementationType) == false)
                throw new InvalidOperationException($"Intercept implementation type {implementationType.FullName} must implement the interface {typeof(InterfaceType).FullName}");

            return InterceptWithImplementationInternal(implementationType);
        }

        private LocalSharedRegistrationIntercept<InterfaceType> InterceptWithImplementationInternal(Type implementationType) 
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddInterceptionType(implementationType);

            return new LocalSharedRegistrationIntercept<InterfaceType>(source);
        }
    }
}
