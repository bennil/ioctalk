using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    /// <summary>
    /// Fluent registration state after a multi import branch implementation was added.
    /// Only in this state the branch can be encapsulated with an interception implementation.
    /// </summary>
    public class LocalSessionRegistrationMultiImportBranch<InterfaceType> : LocalSessionRegistrationIntercept<InterfaceType>
    {
        internal LocalSessionRegistrationMultiImportBranch(TalkCompositionHost source)
            : base(source)
        {
        }

        /// <summary>
        /// Intercepts (encapsulates) the last added multi import branch implementation (AddMultiImportBranchImplementation).
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSessionRegistrationMultiImportBranch<InterfaceType> InterceptMultiImportWithImplementation<InterceptImplementation>()
        where InterceptImplementation : class, InterfaceType
        {
            return InterceptMultiImportWithImplementationInternal(typeof(InterceptImplementation));
        }

        /// <summary>
        /// Intercepts (encapsulates) the last added multi import branch implementation (AddMultiImportBranchImplementation).
        /// </summary>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public LocalSessionRegistrationMultiImportBranch<InterfaceType> InterceptMultiImportWithImplementation(Type implementationType)
        {
            if (typeof(InterfaceType).IsAssignableFrom(implementationType) == false)
                throw new InvalidOperationException($"Intercept implementation type {implementationType.FullName} must implement the interface {typeof(InterfaceType).FullName}");

            return InterceptMultiImportWithImplementationInternal(implementationType);
        }

        private LocalSessionRegistrationMultiImportBranch<InterfaceType> InterceptMultiImportWithImplementationInternal(Type implementationType)
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddBreakoutInterceptionType(implementationType);

            return this;
        }
    }
}
