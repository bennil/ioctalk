using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSharedRegistrationIntercept<InterfaceType> : LocalSharedRegistration<InterfaceType>
    {
        internal LocalSharedRegistrationIntercept(LocalShareContext source)
            : base(source)
        {
        }

        /// <summary>
        /// Adds an additional implementation at the same level used when the parent intercept implementation requests multiple instances of the interface type (inject constructor collection/array interface).
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSharedRegistrationMultiImportBranch<InterfaceType> AddMultiImportBranchImplementation<InterceptImplementation>()
        where InterceptImplementation : class, InterfaceType
        {
            return AddMultiImportBranchImplementationInternal(typeof(InterceptImplementation));
        }

        /// <summary>
        /// Adds an additional implementation at the same level used when the parent intercept implementation requests multiple instances of the interface type (inject constructor collection/array interface).
        /// </summary>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public LocalSharedRegistrationMultiImportBranch<InterfaceType> AddMultiImportBranchImplementation(Type implementationType)
        {
            if (typeof(InterfaceType).IsAssignableFrom(implementationType) == false)
                throw new InvalidOperationException($"Multi import branch implementation type {implementationType.FullName} must implement the interface {typeof(InterfaceType).FullName}");

            return AddMultiImportBranchImplementationInternal(implementationType);
        }

        private LocalSharedRegistrationMultiImportBranch<InterfaceType> AddMultiImportBranchImplementationInternal(Type implementationType)
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddBreakoutType(implementationType);

            return new LocalSharedRegistrationMultiImportBranch<InterfaceType>(source);
        }
    }
}
