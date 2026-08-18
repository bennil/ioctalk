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
    public class LocalSharedRegistrationMultiImportBranch<InterfaceType> : LocalSharedRegistrationIntercept<InterfaceType>
    {
        internal LocalSharedRegistrationMultiImportBranch(LocalShareContext source)
            : base(source)
        {
        }

        /// <summary>
        /// Intercepts (encapsulates) the last added multi import branch implementation (AddMultiImportBranchImplementation).
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSharedRegistrationMultiImportBranch<InterfaceType> InterceptMultiImportWithImplementation<InterceptImplementation>()
        where InterceptImplementation : class, InterfaceType
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddBreakoutInterceptionType(typeof(InterceptImplementation));

            return this;
        }
    }
}
