using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Reflection;
using System.Reflection;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Common.Interface.Container;

namespace BSAG.IOCTalk.Common.Reflection
{
    /// <summary>
    /// Caches reflection information for a method invoke
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-11-21
    /// </remarks>
    public class InvokeMethodInfo : IInvokeMethodInfo
    {
        #region InvokeMethodInfo fields
        // ----------------------------------------------------------------------------------------
        // InvokeMethodInfo fields
        // ----------------------------------------------------------------------------------------

        private MethodInfo interfaceMethod;
        private FastMethodInfo interfaceMethodInvoker;
        private ParameterInfo[] parameterInfos;
        private ParameterInfo[] outParameters;
        private bool isAsyncRemoteInvoke = false;
        private MethodInfo implementationMethod;
        private string qualifiedMethodName;
        private TimeSpan? customTimeout;

        private bool isAsyncVoidRemoteInvoke;
        private bool isAsyncVoidRemoteInvokeResolved;
        private bool isVoidReturnMethod;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeMethodInfo constructors
        // ----------------------------------------------------------------------------------------
        // InvokeMethodInfo constructors
        // ----------------------------------------------------------------------------------------



        /// <summary>
        /// Creates a new instance of the <c>InvokeMethodInfo</c> class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        public InvokeMethodInfo(Type interfaceType, string methodName, Type[] parameterTypes)
        {
            if (parameterTypes != null)
            {
                this.interfaceMethod = interfaceType.GetMethod(methodName, parameterTypes);
            }
            else
            {
                this.interfaceMethod = TypeService.GetMethodByName(interfaceType, methodName);
            }

            if (interfaceMethod == null)
            {
                // try get method from sub interface
                foreach (var subInterface in interfaceType.GetInterfaces())
                {
                    if (parameterTypes != null)
                    {
                        this.interfaceMethod = subInterface.GetMethod(methodName, parameterTypes);
                    }
                    else
                    {
                        this.interfaceMethod = TypeService.GetMethodByName(subInterface, methodName);
                    }

                    if (interfaceType != null)
                        break;
                }
            }

            if (interfaceMethod == null)
            {
                throw new MissingMethodException(interfaceType.FullName, methodName);
            }

            this.interfaceMethodInvoker = new FastMethodInfo(this.interfaceMethod);

            // collect out parameter infos
            List<ParameterInfo> outParameterInfos = null;
            this.parameterInfos = interfaceMethod.GetParameters();
            foreach (var paramInfo in this.parameterInfos)
            {
                if (paramInfo.IsOut)
                {
                    if (outParameterInfos == null)
                        outParameterInfos = new List<ParameterInfo>();

                    outParameterInfos.Add(paramInfo);
                }
            }

            this.qualifiedMethodName = TypeService.GetQualifiedMethodName(interfaceMethod, this.parameterInfos);

            if (outParameterInfos != null)
            {
                this.outParameters = outParameterInfos.ToArray();
            }

            isVoidReturnMethod = interfaceMethod.ReturnType.Equals(typeof(void));
        }

        /// <summary>
        /// Creates a new instance of the <c>InvokeMethodInfo</c> class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        public InvokeMethodInfo(Type interfaceType, string methodName, Type[] parameterTypes, TimeSpan customRequestTimeout)
            : this(interfaceType, methodName, parameterTypes)
        {
            this.customTimeout = customRequestTimeout;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeMethodInfo"/> class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="methodName">Name of the method.</param>
        public InvokeMethodInfo(Type interfaceType, string methodName)
        : this(interfaceType, methodName, null)
        {
            // warning: this constructor does not support multiple parameter signatures (not supported yet)
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeMethodInfo"/> class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        public InvokeMethodInfo(Type interfaceType, string methodName, Type[] parameterTypes, Type implementationType)
            : this(interfaceType, methodName, parameterTypes)
        {
            if (parameterTypes != null)
            {
                this.implementationMethod = implementationType.GetMethod(methodName, parameterTypes);
            }
            else
            {
                this.implementationMethod = TypeService.GetMethodByName(implementationType, methodName);
            }

            if (implementationMethod == null)
            {
                // try to get explicit interface method implementation
                string interfaceMethodName = string.Concat(interfaceType.FullName, ".", methodName);
                var interfaceMap = implementationType.GetInterfaceMap(interfaceType);
                foreach (var targetMethod in interfaceMap.TargetMethods)
                {
                    string qualifiedTargetMethodName = TypeService.GetQualifiedMethodName(targetMethod);

                    if (targetMethod.Name == interfaceMethodName
                        || qualifiedTargetMethodName == interfaceMethodName)
                    {
                        this.implementationMethod = targetMethod;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeMethodInfo"/> class.
        /// </summary>
        /// <param name="interfaceMethodInfo">The method info.</param>
        public InvokeMethodInfo(MethodInfo interfaceMethodInfo)
            : this(interfaceMethodInfo.DeclaringType, interfaceMethodInfo.Name, interfaceMethodInfo.GetParameters().Select<ParameterInfo, Type>(pi => pi.ParameterType).ToArray<Type>())
        {
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeMethodInfo properties
        // ----------------------------------------------------------------------------------------
        // InvokeMethodInfo properties
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Gets the interface method.
        /// </summary>
        public MethodInfo InterfaceMethod
        {
            get { return interfaceMethod; }
        }


        /// <summary>
        /// Gets the implementation method.
        /// </summary>
        public MethodInfo ImplementationMethod
        {
            get { return implementationMethod; }
        }

        /// <summary>
        /// Gets the parameter infos.
        /// </summary>
        public ParameterInfo[] ParameterInfos
        {
            get { return parameterInfos; }
        }

        /// <summary>
        /// Gets the method out parameters.
        /// </summary>
        public ParameterInfo[] OutParameters
        {
            get { return outParameters; }
        }



        /// <summary>
        /// Gets the method name including the type parameters.
        /// </summary>
        /// <value>
        /// The name of the qualified method.
        /// </value>
        public string QualifiedMethodName
        {
            get { return qualifiedMethodName; }
        }

        public TimeSpan? CustomTimeout => customTimeout;

        public bool IsVoidReturnMethod => isVoidReturnMethod;


        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeMethodInfo methods
        // ----------------------------------------------------------------------------------------
        // InvokeMethodInfo methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        ///Creates a unique key for caching.
        /// </summary>
        /// <param name="interfaceTypeName">Name of the interface type.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <returns></returns>
        public static int CreateKey(string interfaceTypeName, string methodName, Type implementationType)
        {
            int keyResult = interfaceTypeName.GetHashCode();
            keyResult = keyResult * 23 + methodName.GetHashCode();
            keyResult = keyResult * 23 + implementationType.GetHashCode();
            return keyResult;
        }

        /// <summary>
        /// Invokes the instance method.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>System.Object.</returns>
        public object Invoke(object instance, object[] parameters)
        {
            return interfaceMethodInvoker.Invoke(instance, parameters);
        }

        /// <summary>
        /// Ioctalk will call the remote method without awaiting the response. The method will return immediately without blocking. This can be a great performance gain for mass remote calls.
        /// To avoid flooding the receiver underlying communication implements a control flow (IsAsyncVoidSendCurrentlyPossible) to issue a sync call if the receiver needs more time to process.
        /// This is only valid on methods with return type "void".
        /// Async void calls do not propagate back thrown exceptions. Exceptions will only occur on receiver side (see error logging).    
        /// </summary>
        /// <param name="containerHost"></param>
        /// <returns></returns>
        public bool IsAsyncVoidRemoteInvoke(IGenericContainerHost containerHost)
        {
            if (!isAsyncVoidRemoteInvokeResolved)
            {
                isAsyncVoidRemoteInvoke = containerHost.IsAsyncVoidRemoteInvoke(InterfaceMethod.DeclaringType, InterfaceMethod.Name);
                isAsyncVoidRemoteInvokeResolved = true;
            }

            return isAsyncVoidRemoteInvoke;
        }

        // ----------------------------------------------------------------------------------------
        #endregion






    }

}
