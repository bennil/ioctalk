using BSAG.IOCTalk.Composition.Interception;
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

        internal RemoteServiceRegistration(TalkCompositionHost source)
        {
            this.interfaceType = typeof(InterfaceType);
            this.source = source;            
        }

        /// <summary>
        /// Ioctalk will call the remote method without awaiting the response. The method will return immediately without blocking. This can be a great performance gain for mass remote calls.
        /// To avoid flooding the receiver underlying communication implements a control flow (IsAsyncVoidSendCurrentlyPossible) to issue a sync call if the receiver needs more time to process.
        /// This is only valid on methods with return type "void".
        /// Async void calls do not propagate back thrown exceptions. Exceptions will only occur on receiver side (see error logging).
        /// </summary>
        /// <param name="methodName">The async void method</param>
        /// <returns></returns>
        public RemoteServiceRegistration<InterfaceType> AsAsyncVoid(string methodName)
        {
            source.RegisterAsyncVoidMethod<InterfaceType>(methodName);

            return this;
        }


        /// <summary>
        /// Intercepts the service registration (circuit breaker)
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSessionRegistrationIntercept<InterfaceType> InterceptWithImplementation<InterceptImplementation>()
            where InterceptImplementation : class, InterfaceType
        {
            return InterceptWithImplementationInternal(typeof(InterceptImplementation));
        }

        public LocalSessionRegistrationIntercept<InterfaceType> InterceptWithImplementation(Type implementationType)
        {
            if (typeof(InterfaceType).IsAssignableFrom(implementationType) == false)
                throw new InvalidOperationException($"Intercept implementation type {implementationType.FullName} must implement the interface {typeof(InterfaceType).FullName}");

            return InterceptWithImplementationInternal(implementationType);
        }

        private LocalSessionRegistrationIntercept<InterfaceType> InterceptWithImplementationInternal(Type implementationType)
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddInterceptionType(implementationType);

            return new LocalSessionRegistrationIntercept<InterfaceType>(source);
        }
    }
}
