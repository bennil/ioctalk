using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using BSAG.IOCTalk.Common.Interface.Container;

namespace BSAG.IOCTalk.Common.Interface.Reflection
{
    /// <summary>
    /// Caches reflection information for a method invoke
    /// </summary>
    public interface IInvokeMethodInfo
    {
        /// <summary>
        /// Gets the interface method.
        /// </summary>
        MethodInfo InterfaceMethod { get; }

        /// <summary>
        /// Gets the parameter infos.
        /// </summary>
        ParameterInfo[] ParameterInfos { get; }

        /// <summary>
        /// Gets the out parameters.
        /// </summary>
        ParameterInfo[] OutParameters { get; }

        /// <summary>
        /// Gets the implementation method.
        /// </summary>
        MethodInfo ImplementationMethod { get; }

        /// <summary>
        /// Gets the method name including the type parameters.
        /// </summary>
        /// <value>
        /// The name of the qualified method.
        /// </value>
        string QualifiedMethodName { get; }


        /// <summary>
        /// Gets a value indicating no return value method
        /// </summary>
        bool IsVoidReturnMethod { get; }

        /// <summary>
        /// Gets a value indicating an async/await method return
        /// </summary>
        bool IsAsyncAwaitRemoteMethod { get; }


        /// <summary>
        /// Gets a custom request timeout
        /// </summary>
        TimeSpan? CustomTimeout { get; }

        /// <summary>
        /// Invokes the instance method.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>System.Object.</returns>
        object Invoke(object instance, object[] parameters);


        /// <summary>
        /// Ioctalk will call the remote method without awaiting the response. The method will return immediately without blocking. This can be a great performance gain for mass remote calls.
        /// To avoid flooding the receiver underlying communication implements a control flow (IsAsyncVoidSendCurrentlyPossible) to issue a sync call if the receiver needs more time to process.
        /// This is only valid on methods with return type "void".
        /// Async void calls do not propagate back thrown exceptions. Exceptions will only occur on receiver side (see error logging).        
        /// </summary>
        /// <param name="containerHost"></param>
        /// <returns></returns>
        bool IsAsyncVoidRemoteInvoke(IGenericContainerHost containerHost);
    }
}
