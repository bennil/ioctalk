﻿using BSAG.IOCTalk.Composition.Interception;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSessionRegistrationIntercept<InterfaceType> : LocalSessionRegistration<InterfaceType>
    {
        internal LocalSessionRegistrationIntercept(TalkCompositionHost source) 
            : base(source)
        {
        }

        /// <summary>
        /// Adds an additional implementation at the same level used when the parent intercept implementation requests multiple instances of the interface type (inject constructor collection/array interface).
        /// </summary>
        /// <typeparam name="InterceptImplementation"></typeparam>
        /// <returns></returns>
        public LocalSessionRegistrationIntercept<InterfaceType> AddMultiImportBranchImplementation<InterceptImplementation>()
        where InterceptImplementation : class, InterfaceType
        {
            TypeHierachy typeHierachy = source.GetInterfaceImplementationTypeHierachy(typeof(InterfaceType));

            typeHierachy.AddBreakoutType(typeof(InterceptImplementation));

            return this;
        }
    }
}