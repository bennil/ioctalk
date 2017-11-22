using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Reflection;
using System.Reflection;
using BSAG.IOCTalk.Common.Attributes;

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

            // determine remote invoke behaviour
            DetermineRemoteInvokeBehaviour(interfaceMethod);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeMethodInfo"/> class.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="methodName">Name of the method.</param>
        public InvokeMethodInfo(Type interfaceType, string methodName)
            : this (interfaceType, methodName, null)
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

            if (implementationMethod != null)
            {
                DetermineRemoteInvokeBehaviour(implementationMethod);
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
        /// Gets a value indicating whether this instance is async remote invoke.
        /// If <c>true</c> IOC Talk will call the method non-blocking and activate the automatic message flow control who only expects a response if necessary (buffer full).
        /// This is only valid on methods with return type "void". It can be specified with the <see cref="RemoteInvokeBehaviourAttribute"/> on the interface method.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is async remote invoke; otherwise, <c>false</c>.
        /// </value>
        public bool IsAsyncRemoteInvoke
        {
            get { return isAsyncRemoteInvoke; }
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

        // ----------------------------------------------------------------------------------------
        #endregion

        #region InvokeMethodInfo methods
        // ----------------------------------------------------------------------------------------
        // InvokeMethodInfo methods
        // ----------------------------------------------------------------------------------------

        private void DetermineRemoteInvokeBehaviour(MethodInfo methodInfo)
        {
            object[] invokeBehaviourAttributes = methodInfo.GetCustomAttributes(typeof(RemoteInvokeBehaviourAttribute), true);
            if (invokeBehaviourAttributes.Length > 0)
            {
                RemoteInvokeBehaviourAttribute remoteInvokeBehv = (RemoteInvokeBehaviourAttribute)invokeBehaviourAttributes[0];

                if (methodInfo.ReturnType == typeof(void) && this.outParameters == null)
                {
                    this.isAsyncRemoteInvoke = remoteInvokeBehv.IsAsyncRemoteInvoke;
                }
            }
        }

        ///// <summary>
        ///// Creates a unique key for caching.
        ///// </summary>
        ///// <param name="interfaceType">Type of the interface.</param>
        ///// <param name="methodName">Name of the method.</param>
        ///// <param name="parameterTypes">The parameter types.</param>
        ///// <returns></returns>
        //public static string CreateKey(Type interfaceType, string methodName)
        //{
        //    return CreateKey(interfaceType, methodName, null);
        //}

        ///// <summary>
        ///// Creates a unique key for caching.
        ///// </summary>
        ///// <param name="interfaceType">Type of the interface.</param>
        ///// <param name="methodName">Name of the method.</param>
        ///// <param name="parameterTypes">The parameter types.</param>
        ///// <param name="implementationType">Type of the implementation.</param>
        ///// <returns></returns>
        //public static string CreateKey(Type interfaceType, string methodName, Type implementationType)
        //{
        //    return CreateKey(interfaceType.FullName, methodName, implementationType);
        //}

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

        // ----------------------------------------------------------------------------------------
        #endregion






    }

}
