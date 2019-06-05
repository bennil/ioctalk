using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace BSAG.IOCTalk.Common.Reflection
{
    /// <summary>
    /// Class FastMethodInfo.
    /// source: http://stackoverflow.com/questions/10313979/methodinfo-invoke-performance-issue
    /// </summary>
    public class FastMethodInfo
    {
        private delegate object ReturnValueDelegate(object instance, object[] arguments);
        private delegate void VoidDelegate(object instance, object[] arguments);

        private bool containsOutParams = false;
        private MethodInfo mInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastMethodInfo"/> class.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        public FastMethodInfo(MethodInfo methodInfo)
        {
            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];

                var paramType = parameterInfo.ParameterType;
                //todo: behaviour changed in .net core 2.1 -> find new solution -> solution pending for method out parameter support !!!
                if (parameterInfo.IsOut)
                {
                    mInfo = methodInfo;
                    containsOutParams = true;
                    return;     // out params not supported by fast path use MethodInfo instead

                    ////  System.ArgumentException : Type must not be ByRef
                    //var indexExpr = Expression.Constant(i); //, paramType.GetElementType());
                    //var expr1 = Expression.ArrayIndex(argumentsExpression, indexExpr);

                    ////Expression.ArrayAccess(argumentExpressions, indexExpr);
                    //var contypeCode = Expression.Constant(Type.GetTypeCode(paramType));
                    //var t1 = Expression.Convert(expr1, paramType);

                    ////if (parameterInfo.ParameterType.IsByRef)
                    ////{
                    ////paramType = paramType.GetElementType();
                    //var paramExpression = Expression.Parameter(parameterInfo.ParameterType, parameterInfo.Name);
                    //argumentExpressions.Add(paramExpression);
                    ////}
                }
                else
                {
                    argumentExpressions.Add(Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), paramType));
                }
            }
            var callExpression = Expression.Call(!methodInfo.IsStatic ? Expression.Convert(instanceExpression, methodInfo.ReflectedType) : null, methodInfo, argumentExpressions);
            if (callExpression.Type == typeof(void))
            {
                var voidDelegate = Expression.Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).Compile();
                Delegate = (instance, arguments) => { voidDelegate(instance, arguments); return null; };
            }
            else
                Delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)), instanceExpression, argumentsExpression).Compile();
        }

        private ReturnValueDelegate Delegate { get; }

        /// <summary>
        /// Invokes the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>System.Object.</returns>
        public object Invoke(object instance, params object[] arguments)
        {
            if (containsOutParams)
            {
                return mInfo.Invoke(instance, arguments);
            }
            else
            {
                return Delegate(instance, arguments);
            }
        }
    }
}
