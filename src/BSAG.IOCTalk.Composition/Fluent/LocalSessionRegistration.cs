using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSessionRegistration<InterfaceType>
    {
        protected TalkCompositionHost source;

        internal LocalSessionRegistration(TalkCompositionHost source)
        {
            this.source = source;
        }

        public LocalSessionRegistration<InterfaceType> TargetAlsoImplements<AdditionalInterface>()
        {
            if (source.LocalSessionServiceTypeMappings == null)
            {
                source.LocalSessionServiceTypeMappings = new Dictionary<Type, Type>();
            }

            Type sourceType = typeof(InterfaceType);
            Type targetAlsoImplements = typeof(AdditionalInterface);

            if (!targetAlsoImplements.IsInterface)
                throw new InvalidOperationException($"Target type \"{targetAlsoImplements}\" must be an interface!");

            source.LocalSessionServiceTypeMappings[targetAlsoImplements] = sourceType;
            
            //Type[] additionalServiceTypes;
            //if (source.LocalSessionServiceTypeMappings.TryGetValue(sourceType, out additionalServiceTypes))
            //{
            //    List<Type> extendedTypeList = new List<Type>(additionalServiceTypes);
            //    if (!extendedTypeList.Contains(targetAlsoImplements))
            //    {
            //        extendedTypeList.Add(targetAlsoImplements);

            //        source.LocalSessionServiceTypeMappings[sourceType] = extendedTypeList.ToArray();
            //    }
            //}
            //else
            //{
            //    source.LocalSessionServiceTypeMappings[sourceType] = new Type[] { targetAlsoImplements };
            //}

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
