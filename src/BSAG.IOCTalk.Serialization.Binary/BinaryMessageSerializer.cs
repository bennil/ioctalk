using BSAG.IOCTalk.Common.Interface.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Reflection;
using System.Reflection;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Serialization.Binary
{
    public class BinaryMessageSerializer : IGenericMessageSerializer, IUnknowContextTypeResolver, IMessageStreamSerializer
    {
        private const string GenericMessagePayloadPropertyName = "Payload";

        private IGenericContainerHost containerHost;
        private BinarySerializer serializer;
        private static readonly Type objectArrayType = typeof(object[]);
        private ConcurrentDictionary<int, ParameterInfo[]> methodParameterCache = new ConcurrentDictionary<int, ParameterInfo[]>();

        public bool IsMissingFieldsInSourceDataAllowed { get; set; } = true;


        public BinaryMessageSerializer()
        {
            serializer = new BinarySerializer(this);
            serializer.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Target));
            serializer.RegisterStringHashProperty(typeof(IGenericMessage), nameof(IGenericMessage.Name));
        }

        /// <summary>
        /// Gets the serializer raw message format.
        /// </summary>
        /// <value>The message format.</value>
        public RawMessageFormat MessageFormat
        {
            get
            {
                return RawMessageFormat.Binary;
            }
        }


        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        public void RegisterContainerHost(IGenericContainerHost containerHost)
        {
            this.containerHost = containerHost;

            // Register message type
            serializer.GetByType(typeof(IGenericMessage), new SerializationContext(serializer, this, false));
        }

        public IGenericMessage DeserializeFromBytes(byte[] messageBytes, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(messageBytes, contextObject);
        }


        public IGenericMessage DeserializeFromBytes(ArraySegment<byte> messageBytesSegment, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(messageBytesSegment, contextObject);
        }

        public IGenericMessage DeserializeFromString(string messageString, object contextObject)
        {
            throw new NotImplementedException();
        }


        public byte[] SerializeToBytes(IGenericMessage message, object contextObject)
        {
            return serializer.Serialize<IGenericMessage>(message, contextObject);
        }

        public string SerializeToString(IGenericMessage message, object contextObject)
        {
            throw new NotImplementedException();
        }




        public void Serialize(IStreamWriter writer, IGenericMessage message, object contextObject)
        {
            serializer.Serialize<IGenericMessage>(writer, message, contextObject);
        }

        public IGenericMessage Deserialize(IStreamReader reader, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(reader, contextObject);
        }


        public Type DetermineTargetType(Type sourceType, ISerializeContext context)
        {
            if (sourceType.IsInterface)
            {
                if (sourceType.Equals(typeof(IGenericMessage)))
                {
                    return typeof(Communication.Common.GenericMessage);
                }
                else
                {
                    return containerHost.GetInterfaceImplementationType(sourceType.FullName);
                }
            }
            else
            {
                //// reverse lookup interfaces
                //if (sourceType.Equals(typeof(Communication.Common.GenericMessage)))
                //{
                //    return typeof(IGenericMessage);
                //}
                //else if (context.ParentObject is IGenericMessage)
                //{
                //    IGenericMessage message = (IGenericMessage)context.ParentObject;

                //    if (context.ExternalContext is IInvokeMethodInfo)
                //    {
                //        // serialize context
                //        IInvokeMethodInfo invokeInfo = (IInvokeMethodInfo)context.ExternalContext;

                //        switch (message.Type)
                //        {
                //            case MessageType.MethodInvokeRequest:
                //            case MessageType.AsyncMethodInvokeRequest:
                //                if (context.ArrayIndex.HasValue)
                //                {
                //                    var paramInfo = invokeInfo.ParameterInfos[context.ArrayIndex.Value];

                //                    if (paramInfo.IsOut)
                //                    {
                //                        // determine out parameter type
                //                        return paramInfo.ParameterType.GetElementType();
                //                    }
                //                    else
                //                    {
                //                        return paramInfo.ParameterType;
                //                    }
                //                }
                //                break;
                //            //else
                //            //{
                //            //    return objectArrayType;
                //            //}

                //            case MessageType.MethodInvokeResponse:


                //                if (context.ArrayIndex == 0
                //                    || !context.ArrayIndex.HasValue)
                //                {
                //                    return invokeInfo.InterfaceMethod.ReturnType;
                //                }
                //                else
                //                {
                //                    // determine out parameter type
                //                    return invokeInfo.OutParameters[context.ArrayIndex.Value - 1].ParameterType.GetElementType();
                //                }
                //        }
                //    }
                //    else if (context.ExternalContext is ISession)
                //    {
                //        // deserialize context
                //        ISession session = (ISession)context.ExternalContext;

                //        switch (message.Type)
                //        {
                //            case MessageType.AsyncMethodInvokeRequest:
                //            case MessageType.MethodInvokeRequest:

                //                if (context.ArrayIndex.HasValue)
                //                {
                //                    int cacheKey = message.Target.GetHashCode();
                //                    cacheKey = cacheKey * 23 + message.Name.GetHashCode();

                //                    ParameterInfo[] parameters;
                //                    if (!methodParameterCache.TryGetValue(cacheKey, out parameters))
                //                    {
                //                        Type interfaceServiceType;
                //                        if (TypeService.TryGetTypeByName(message.Target, out interfaceServiceType))
                //                        {
                //                            MethodInfo mi = TypeService.GetMethodByName(interfaceServiceType, message.Name);
                //                            if (mi == null)
                //                            {
                //                                throw new InvalidOperationException(string.Format("The method: \"{0}\" is not specified in the interface: \"{1}\"", message.Name, interfaceServiceType.AssemblyQualifiedName));
                //                            }

                //                            parameters = mi.GetParameters();

                //                            methodParameterCache[cacheKey] = parameters;
                //                        }
                //                        else
                //                        {
                //                            throw new TypeLoadException(string.Format("The interface \"{0}\" could not be found!", message.Target));
                //                        }
                //                    }

                //                    return parameters[context.ArrayIndex.Value].ParameterType;
                //                }
                //                else
                //                {
                //                    return objectArrayType;
                //                }

                //            case MessageType.MethodInvokeResponse:

                //                IInvokeState invokeState;
                //                if (session.PendingRequests.TryGetValue(message.RequestId, out invokeState))
                //                {
                //                    if (invokeState.OutParameterValues != null)
                //                    {
                //                        if (context.ArrayIndex.HasValue)
                //                        {
                //                            if (context.ArrayIndex == 0)
                //                            {
                //                                return invokeState.Method.ReturnType;
                //                            }
                //                            else
                //                            {
                //                                Type type = invokeState.MethodSource.OutParameters[context.ArrayIndex.Value - 1].ParameterType;
                //                                type = type.GetElementType();
                //                                return type;
                //                            }
                //                        }
                //                        else
                //                        {
                //                            return objectArrayType;
                //                        }
                //                    }
                //                    else if (context.ArrayIndex == 0
                //                        || !context.ArrayIndex.HasValue)
                //                    {
                //                        // first arrar item contains method return type 
                //                        // or payload only inlcudes return object
                //                        return invokeState.Method.ReturnType;
                //                    }
                //                }
                //                else
                //                {
                //                    throw new InvalidOperationException("Pending request ID: " + message.RequestId + " message not found!");
                //                }
                //                break;

                //        }
                //    }
                //    else if ((message.Type == MessageType.MethodInvokeRequest
                //            || message.Type == MessageType.AsyncMethodInvokeRequest)
                //            && !context.ArrayIndex.HasValue)
                //    {
                //        return objectArrayType;
                //    }

                //}
            }


            return null;
        }


        public IValueItem DetermineSpecialInterfaceType(Type sourceType, Type defaultInterfaceType, ISerializeContext context)
        {
            // reverse lookup interfaces
            Type resultType = null;
            IValueItem specialTarget;
            if (serializer.TryGetDifferentTargetType(sourceType, out specialTarget)
                && specialTarget != null)
            {
                return specialTarget;
            }

            // check contextual target type
            {
                if (sourceType.Equals(typeof(Communication.Common.GenericMessage)))
                {
                    resultType = typeof(IGenericMessage);
                }
                else if (context.ParentObject is IGenericMessage)
                {
                    IGenericMessage message = (IGenericMessage)context.ParentObject;

                    if (context.ExternalContext is IInvokeMethodInfo)
                    {
                        // serialize context
                        IInvokeMethodInfo invokeInfo = (IInvokeMethodInfo)context.ExternalContext;

                        switch (message.Type)
                        {
                            case MessageType.MethodInvokeRequest:
                            case MessageType.AsyncMethodInvokeRequest:
                                if (context.ArrayIndex.HasValue)
                                {
                                    var paramInfo = invokeInfo.ParameterInfos[context.ArrayIndex.Value];

                                    if (paramInfo.IsOut)
                                    {
                                        // determine out parameter type
                                        resultType = paramInfo.ParameterType.GetElementType();
                                    }
                                    else
                                    {
                                        resultType = paramInfo.ParameterType;
                                    }
                                }
                                break;
                            //else
                            //{
                            //    return objectArrayType;
                            //}

                            case MessageType.MethodInvokeResponse:


                                if (context.ArrayIndex == 0
                                    || !context.ArrayIndex.HasValue)
                                {
                                    resultType = invokeInfo.InterfaceMethod.ReturnType;
                                }
                                else
                                {
                                    // determine out parameter type
                                    resultType = invokeInfo.OutParameters[context.ArrayIndex.Value - 1].ParameterType.GetElementType();
                                }
                                break;
                        }
                    }
                    else if (context.ExternalContext is ISession)
                    {
                        // deserialize context
                        ISession session = (ISession)context.ExternalContext;

                        switch (message.Type)
                        {
                            case MessageType.AsyncMethodInvokeRequest:
                            case MessageType.MethodInvokeRequest:

                                if (context.ArrayIndex.HasValue)
                                {
                                    int cacheKey = message.Target.GetHashCode();
                                    cacheKey = cacheKey * 23 + message.Name.GetHashCode();

                                    ParameterInfo[] parameters;
                                    if (!methodParameterCache.TryGetValue(cacheKey, out parameters))
                                    {
                                        Type interfaceServiceType;
                                        if (TypeService.TryGetTypeByName(message.Target, out interfaceServiceType))
                                        {
                                            MethodInfo mi = TypeService.GetMethodByName(interfaceServiceType, message.Name);
                                            if (mi == null)
                                            {
                                                throw new InvalidOperationException(string.Format("The method: \"{0}\" is not specified in the interface: \"{1}\"", message.Name, interfaceServiceType.AssemblyQualifiedName));
                                            }

                                            parameters = mi.GetParameters();

                                            methodParameterCache[cacheKey] = parameters;
                                        }
                                        else
                                        {
                                            throw new TypeLoadException(string.Format("The interface \"{0}\" could not be found!", message.Target));
                                        }
                                    }

                                    resultType = parameters[context.ArrayIndex.Value].ParameterType;
                                }
                                else
                                {
                                    resultType = objectArrayType;
                                }
                                break;

                            case MessageType.MethodInvokeResponse:

                                IInvokeState invokeState;
                                if (session.PendingRequests.TryGetValue(message.RequestId, out invokeState))
                                {
                                    if (invokeState.OutParameterValues != null)
                                    {
                                        if (context.ArrayIndex.HasValue)
                                        {
                                            if (context.ArrayIndex == 0)
                                            {
                                                resultType = invokeState.Method.ReturnType;
                                            }
                                            else
                                            {
                                                Type type = invokeState.MethodSource.OutParameters[context.ArrayIndex.Value - 1].ParameterType;
                                                type = type.GetElementType();
                                                resultType = type;
                                            }
                                        }
                                        else
                                        {
                                            resultType = objectArrayType;
                                        }
                                    }
                                    else if (context.ArrayIndex == 0
                                        || !context.ArrayIndex.HasValue)
                                    {
                                        // first arrar item contains method return type 
                                        // or payload only inlcudes return object
                                        resultType = invokeState.Method.ReturnType;
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException("Pending request ID: " + message.RequestId + " message not found!");
                                }
                                break;

                        }
                    }
                    else if ((message.Type == MessageType.MethodInvokeRequest
                            || message.Type == MessageType.AsyncMethodInvokeRequest)
                            && !context.ArrayIndex.HasValue)
                    {
                        resultType = objectArrayType;
                    }
                }
            }

            if (resultType != null)
            {
                return serializer.RegisterDifferentTargetType(sourceType, defaultInterfaceType, resultType, context);
            }
            else
            {
                return serializer.DetermineSpecialInterfaceType(sourceType, defaultInterfaceType, context); ;
            }
        }


    }
}
