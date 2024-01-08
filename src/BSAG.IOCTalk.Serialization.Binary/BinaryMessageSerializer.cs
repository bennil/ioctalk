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
using BSAG.IOCTalk.Communication.Common;
using System.Buffers;
using BSAG.IOCTalk.Common.Session;

namespace BSAG.IOCTalk.Serialization.Binary
{
    public class BinaryMessageSerializer : IGenericMessageSerializer, IUnknowContextTypeResolver, IMessageStreamSerializer
    {
        private const string GenericMessagePayloadPropertyName = "Payload";

        private IGenericContainerHost containerHost;
        private BinarySerializer serializer;
        private static readonly Type objectArrayType = typeof(object[]);
        private ConcurrentDictionary<int, ParameterInfo[]> methodParameterCache = new ConcurrentDictionary<int, ParameterInfo[]>();

        ConcurrentDictionary<int, SessionSerializerContext> serializeSessionContext = new ConcurrentDictionary<int, SessionSerializerContext>();

        public bool IsMissingFieldsInSourceDataAllowed { get; set; } = true;


        public BinaryMessageSerializer()
        {
            serializer = new BinarySerializer(this);
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

        public BinarySerializer Serializer => serializer;

        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        public void RegisterContainerHost(IGenericContainerHost containerHost)
        {
            this.containerHost = containerHost;

            // Register message type
            serializer.GetByType(typeof(IGenericMessage), new SerializationContext(serializer, false));
        }

        public IGenericMessage DeserializeFromBytes(byte[] messageBytes, object contextObject)
        {
            SessionSerializerContext sessionCtx = GetOrCreateSessionContext(0);
            sessionCtx.DeserializeContext.Reset(contextObject);
            return (IGenericMessage)serializer.Deserialize(messageBytes, messageBytes.Length, sessionCtx.DeserializeContext);
        }

        public IGenericMessage DeserializeFromBytes(byte[] messageBytesBuffer, int messageLength, object contextObject, int sessionId)
        {
            SessionSerializerContext sessionCtx = GetOrCreateSessionContext(sessionId);
            sessionCtx.DeserializeContext.Reset(contextObject);
            return (IGenericMessage)serializer.Deserialize(messageBytesBuffer, messageLength, sessionCtx.DeserializeContext);
        }


        public IGenericMessage DeserializeFromBytes(ArraySegment<byte> messageBytesSegment, object contextObject)
        {
            throw new NotImplementedException();
            //return (IGenericMessage)serializer.Deserialize(messageBytesSegment, contextObject);
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




        public void Serialize(IStreamWriter writer, IGenericMessage message, object contextObject, int sessionId)
        {
            SessionSerializerContext sessionCtx = GetOrCreateSessionContext(sessionId);
            sessionCtx.SerializeContext.Reset(contextObject);
            serializer.Serialize(writer, message, typeof(IGenericMessage), sessionCtx.SerializeContext);
        }

        public IGenericMessage Deserialize(IStreamReader reader, object contextObject, int sessionId)
        {
            SessionSerializerContext sessionCtx = GetOrCreateSessionContext(sessionId);
            sessionCtx.DeserializeContext.Reset(contextObject);
            return (IGenericMessage)serializer.Deserialize(reader, sessionCtx.DeserializeContext);
        }

        private SessionSerializerContext GetOrCreateSessionContext(int sessionId)
        {
            SessionSerializerContext sessionCtx;
            if (serializeSessionContext.TryGetValue(sessionId, out sessionCtx) == false)
            {
                sessionCtx = new SessionSerializerContext(serializer);
                serializeSessionContext[sessionId] = sessionCtx;
            }

            return sessionCtx;
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
            if (context.TryGetDifferentTargetType(sourceType, out specialTarget)
                && specialTarget != null)
            {
                return specialTarget;
            }

            // check contextual target type
            {
                if (sourceType.Equals(typeof(Communication.Common.GenericMessage)))
                {
                    return null;    // no special interface
                }
                else if (context.ParentObject is IGenericMessage message)
                {
                    resultType = DetermineSpecialInterfaceTypeMessageContext(context, message);
                }
                else if (context.ParentParentObject is IGenericMessage message2
                    && message2.Payload == context.ParentObject)
                {
                    resultType = DetermineSpecialInterfaceTypeMessageContext(context, message2);
                }
            }

            var exposedType = containerHost?.GetExposedSubInterfaceForType(sourceType);
            Type exposedInterface = null;
            if (exposedType != null)
            {
                if (resultType != null)
                {
                    // check if exposed special type is in interface hierarchy of expected result interface
                    if (resultType.IsAssignableFrom(exposedType) == true)
                    {
                        resultType = exposedType;
                    }
                    // else: sourceType class implements result and exposed interface but both interfaces have no derivation connection > do not expose special interface in this case
                }
                else
                    resultType = exposedType;
            }
            else if (resultType != null && resultType.IsInterface)
            {
                // check if derived result interface is exposed
                exposedInterface = containerHost?.GetExposedSubInterfaceForType(resultType);
                if (exposedInterface != null)
                {
                    // check if exposed special type is in interface hierarchy of expected result interface
                    if (resultType.IsAssignableFrom(exposedInterface) == true
                        && exposedInterface.IsAssignableFrom(sourceType))
                    {
                        resultType = exposedInterface;
                    }
                }
            }

            if (resultType != null)
            {
                bool cacheDifference = exposedType is null && exposedInterface is null;
                return context.RegisterDifferentTargetType(sourceType, defaultInterfaceType, resultType, cacheDifference);
            }
            else
            {
                return null;
            }
        }

        private Type DetermineSpecialInterfaceTypeMessageContext(ISerializeContext context, IGenericMessage message)
        {
            Type resultType = null;

            if (context.ChildLevel <= 2)
            {
                if (context.ExternalContext is IInvokeMethodInfo invokeInfo)
                {
                    // serialize context
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

                            if (context.ChildLevel <= 1)
                            {
                                if (context.ArrayIndex == 0
                                    || !context.ArrayIndex.HasValue)
                                {
                                    if (invokeInfo.IsAsyncAwaitRemoteMethod)
                                        resultType = TypeService.GetAsyncAwaitResultType(invokeInfo.InterfaceMethod.ReturnType);
                                    else
                                        resultType = invokeInfo.InterfaceMethod.ReturnType;
                                }
                                else
                                {
                                    // determine out parameter type
                                    resultType = invokeInfo.OutParameters[context.ArrayIndex.Value - 1].ParameterType.GetElementType();
                                }
                            }
                            break;
                    }
                }
                else if (context.ExternalContext is ISession session)
                {
                    // deserialize context

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
                                    if (invokeState.MethodSource.IsAsyncAwaitRemoteMethod)
                                        resultType = TypeService.GetAsyncAwaitResultType(invokeState.Method.ReturnType);
                                    else
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

            return resultType;
        }

        public void DisposeSession(int sessionId)
        {
            serializeSessionContext.TryRemove(sessionId, out _);
        }
    }
}
