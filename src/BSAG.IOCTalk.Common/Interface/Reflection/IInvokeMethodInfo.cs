using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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
        /// Gets a value indicating whether this instance is async remote invoke.
        /// If <c>true</c> IOC Talk will call the method non-blocking and activate the automatic message flow control who only expects a response if necessary (buffer full).
        /// This is only valid on methods with return type "void". It can be specified with the <see cref="RemoteInvokeBehaviourAttribute"/> on the interface method.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is async remote invoke; otherwise, <c>false</c>.
        /// </value>
        bool IsAsyncRemoteInvoke { get; }

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
    }
}
