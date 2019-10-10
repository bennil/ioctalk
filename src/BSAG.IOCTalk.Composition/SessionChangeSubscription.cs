using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Composition
{
    public class SessionChangeSubscription
    {
        public SessionChangeSubscription(Type serviceType)
        {
            this.ServiceInterface = serviceType;
        }

        public Type ServiceInterface { get; private set; }

        internal List<SessionChangeSubscriptionItem> InvokeList { get; private set; } = new List<SessionChangeSubscriptionItem>();

        //public List<object> InvokeListSource { get; private set; } = new List<object>();

        internal void Invoke(object sourceService, IContract sessionContract, ISession session)
        {
            foreach (var invokeItem in InvokeList)
            {
                // call subscribed action
                object[] callParams = new object[invokeItem.Parameters.Length];
                callParams[0] = sourceService;  // first one is always bind to the subscription itself

                // collect additional session instances (session event is bind to first service interface)
                for (int i = 1; i < invokeItem.Parameters.Length; i++)
                {
                    var param = invokeItem.Parameters[i];

                    if (param.ParameterType == typeof(int))
                    {
                        callParams[i] = session.SessionId;
                    }
                    else if (param.ParameterType == typeof(string))
                    {
                        callParams[i] = session.Description;
                    }
                    else if (sessionContract.TryGetSessionInstance(param.ParameterType, out object additionalObj))
                    {
                        callParams[i] = additionalObj;
                    }
                }

                // check assembly load problems (can be removed if roslyn MetadataReference problem is solved)
                for (int i = 0; i < invokeItem.Parameters.Length; i++)
                {
                    var param = invokeItem.Parameters[i];
                    object value = callParams[i];

                    if (value != null)
                    {
                        var valueType = value.GetType();
                        if (!param.ParameterType.IsAssignableFrom(valueType))
                        {
                            var implementedInterfaceType = valueType.GetInterface(param.ParameterType.FullName);

                            if (implementedInterfaceType == null)
                            {
                                throw new InvalidOperationException($"Service proxy does not implement the target interface {param.ParameterType.FullName}!");
                            }
                            else
                            {
                                if (param.ParameterType.AssemblyQualifiedName == implementedInterfaceType.AssemblyQualifiedName)
                                {
                                    throw new InvalidOperationException($"Assembly load problem for \"{param.ParameterType.FullName}\"! The assembly \"{param.ParameterType.AssemblyQualifiedName}\" is loaded twice resulting in different types for the same interface! HashCode exptected interface: {param.ParameterType.GetHashCode()}; HashCode implemented interface: {implementedInterfaceType.GetHashCode()}. Implementing own proxy implementation can be a workaround for this problem.");
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Service proxy target interface implementation of \"{param.ParameterType.FullName}\" does not have the same assembly! Expected assembly: {param.ParameterType.AssemblyQualifiedName} - Proxy implemented assembly reference: {implementedInterfaceType.AssemblyQualifiedName}");
                                }
                            }
                        }
                    }
                }


                invokeItem.Callback.DynamicInvoke(callParams);


                //if (invokeItem.Parameters.Length == 1)
                //{
                //    //bool result = invokeItem.Parameters[0].ParameterType.IsAssignableFrom(sourceService.GetType());
                //    //var interf = sourceService.GetType().GetInterfaces()[0];
                //    //bool result2 = invokeItem.Parameters[0].ParameterType.IsAssignableFrom(interf);
                //    //bool result3 = invokeItem.Parameters[0].ParameterType == interf;

                //    invokeItem.Callback.DynamicInvoke(sourceService);
                //}
                //else if (invokeItem.Parameters.Length == 2
                //    && invokeItem.Parameters[1].ParameterType == typeof(int))
                //{
                //    invokeItem.Callback.DynamicInvoke(sourceService, session.SessionId);
                //}
                //else if (invokeItem.Parameters.Length == 3
                //    && invokeItem.Parameters[1].ParameterType == typeof(int)
                //    && invokeItem.Parameters[2].ParameterType == typeof(string))
                //{
                //    invokeItem.Callback.DynamicInvoke(sourceService, session.SessionId, session.Description);
                //}
                //else
                //{
                //    throw new InvalidOperationException($"Invalid callback subscription!");

                //    //object[] callParams = new object[invokeItem.Parameters.Length];
                //    //for (int i = 0; i < invokeItem.Parameters.Length; i++)
                //    //{
                //    //    var param = invokeItem.Parameters[i];
                //    //}
                //}
            }
        }


        internal void AddDelegate(Delegate sessionDelegate, ParameterInfo[] parameters, ISession targetSessionOnlyContext)
        {
            InvokeList.Add(new SessionChangeSubscriptionItem(sessionDelegate, parameters, targetSessionOnlyContext));
        }

        internal void RemoveDelegate(object serviceInstance, ISession session)
        {
            for (int i = 0; i < InvokeList.Count;)
            {
                var inv = InvokeList[i];
                if (inv.Callback.Target == serviceInstance)
                {
                    InvokeList.RemoveAt(i);
                }
                else if (session != null && session.Equals(inv.TargetSessionOnlyContext))
                {
                    // remove session only related local target instance subscription
                    InvokeList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

    }
}
