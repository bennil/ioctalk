using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Container;
using System.IO;
using System.Globalization;
using System.Collections;
using BSAG.IOCTalk.Serialization.Json.TypeStructure;
using BSAG.IOCTalk.Common.Reflection;
using System.Reflection;
using BSAG.IOCTalk.Common.Interface.Reflection;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Common.Exceptions;

namespace BSAG.IOCTalk.Serialization.Json
{
    /// <summary>
    /// Implements the <see cref="IGenericMessageSerializer"/> interface for json message serialization.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-16
    /// </remarks>
    public class JsonMessageSerializer : IGenericMessageSerializer
    {
        #region JsonMessageSerializer fields
        // ----------------------------------------------------------------------------------------
        // JsonMessageSerializer fields
        // ----------------------------------------------------------------------------------------
        private const string GenericMessagePayloadPropertyName = "Payload";
        private static readonly Type objectArrayType = typeof(object[]);

        private JsonObjectSerializer serializer;
        private IGenericContainerHost containerHost;
        private ConcurrentDictionary<string, ParameterInfo[]> methodParameterCache = new ConcurrentDictionary<string, ParameterInfo[]>();
        private ConcurrentDictionary<Type, Type> resolvedSpecialTypes = new ConcurrentDictionary<Type, Type>();

        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonMessageSerializer constructors
        // ----------------------------------------------------------------------------------------
        // JsonMessageSerializer constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>JsonMessageSerializer</c> class.
        /// </summary>
        public JsonMessageSerializer()
        {
            // Init Serializer
            serializer = new JsonObjectSerializer(UnknownTypeResolver, SpecialTypeResolver);
        }

        static JsonMessageSerializer()
        {
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonMessageSerializer properties
        // ----------------------------------------------------------------------------------------
        // JsonMessageSerializer properties
        // ----------------------------------------------------------------------------------------


        /// <summary>
        /// Gets or sets a value indicating whether this instance is missing fields in source data allowed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is missing fields in source data allowed; otherwise, <c>false</c>.
        /// </value>
        public bool IsMissingFieldsInSourceDataAllowed
        {
            get
            {
                return serializer.IsMissingFieldDataAllowed;
            }
            set
            {
                serializer.IsMissingFieldDataAllowed = value;
            }
        }
        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonMessageSerializer methods
        // ----------------------------------------------------------------------------------------
        // JsonMessageSerializer methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        public void RegisterContainerHost(IGenericContainerHost containerHost)
        {
            this.containerHost = containerHost;
        }

        /// <summary>
        /// Serializes the <see cref="GenericMessage"/> to string.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        public virtual string SerializeToString(IGenericMessage message, object contextObject)
        {
            return serializer.Serialize(message, contextObject);
        }

        /// <summary>
        /// Deserializes from string to a <see cref="GenericMessage"/>.
        /// </summary>
        /// <param name="messageString">The message string.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        public virtual IGenericMessage DeserializeFromString(string messageString, object contextObject)
        {
            return (IGenericMessage)serializer.Deserialize(messageString, typeof(GenericMessage), contextObject);
        }



        /// <summary>
        /// Serializes to byte array.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public byte[] SerializeToBytes(IGenericMessage message, object context)
        {
            string jsonString = serializer.Serialize(message, context);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        /// <summary>
        /// Deserializes from byte array.
        /// </summary>
        /// <param name="messageBytes">The message bytes.</param>
        /// <param name="contextObject">Deserialize context</param>
        /// <returns></returns>
        public IGenericMessage DeserializeFromBytes(byte[] messageBytes, object contextObject)
        {
            string jsonString = Encoding.UTF8.GetString(messageBytes);
            return (IGenericMessage)serializer.Deserialize(jsonString, typeof(GenericMessage), contextObject);
        }



        private Type UnknownTypeResolver(SerializationContext context)
        {
            if (context.InterfaceType != null)
            {
                return containerHost.GetInterfaceImplementationType(context.InterfaceType.FullName);
            }
            else if (context.Key == GenericMessagePayloadPropertyName
                && context.ParentObject is GenericMessage)
            {
                GenericMessage message = (GenericMessage)context.ParentObject;

                // check out of order json keys -> load manually
                if (message.Type == MessageType.Undefined)
                {
                    message.Type = (MessageType)short.Parse(GetJsonSimpleStringValue(context.JsonString, "Type"));
                }
                if (message.Type == MessageType.Exception)
                {
                    if (context.IsDeserialize)
                    {
                        // check if payload contains complex type (ExceptionWrapper)
                        // this check must be done because of backwards compatibility 
                        if (context.JsonString[context.ValueStartIndex + context.Key.Length + 3] == Structure.CharLeftBrace)
                        {
                            return typeof(ExceptionWrapper);
                        }
                        else
                        {
                            return typeof(string);
                        }
                    }
                    else
                    {
                        // always serialize wrapper object
                        return typeof(ExceptionWrapper);
                    }
                }                

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
                                    return paramInfo.ParameterType.GetElementType();
                                }
                                else
                                {
                                    return paramInfo.ParameterType;
                                }
                            }
                            else
                            {
                                return objectArrayType;
                            }

                        case MessageType.MethodInvokeResponse:


                            if (context.ArrayIndex == 0
                                || !context.ArrayIndex.HasValue)
                            {
                                return invokeInfo.InterfaceMethod.ReturnType;
                            }
                            else
                            {
                                // determine out parameter type
                                return invokeInfo.OutParameters[context.ArrayIndex.Value - 1].ParameterType.GetElementType();
                            }
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
                                // check out of order json keys -> load manually
                                if (message.Name == null)
                                {
                                    message.Name = GetJsonSimpleStringValue(context.JsonString, "Name");
                                }
                                if (message.Target == null) 
                                {                                    
                                    message.Target = GetJsonSimpleStringValue(context.JsonString, "Target");
                                }
                                

                                string cacheKey = string.Join(Structure.Comma, message.Target, message.Name);

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

                                return parameters[context.ArrayIndex.Value].ParameterType;
                            }
                            else
                            {
                                return objectArrayType;
                            }

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
                                            return invokeState.Method.ReturnType;
                                        }
                                        else
                                        {
                                            Type type = invokeState.MethodSource.OutParameters[context.ArrayIndex.Value - 1].ParameterType;
                                            type = type.GetElementType();
                                            return type;
                                        }
                                    }
                                    else
                                    {
                                        return objectArrayType;
                                    }
                                }
                                else if (context.ArrayIndex == 0
                                    || !context.ArrayIndex.HasValue)
                                {
                                    // first arrar item contains method return type 
                                    // or payload only inlcudes return object
                                    return invokeState.Method.ReturnType;
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
                    return objectArrayType;
                }

            }

            return null;
        }


        private Type SpecialTypeResolver(Type sourceType)
        {
            Type result;
            if (!resolvedSpecialTypes.TryGetValue(sourceType, out result))
            {
                // check if source type requires a special serialization target
                result = containerHost.GetExposedSubInterfaceForType(sourceType);

                if (result == null)
                {
                    // check expose sub type attribute
                    var exposureAttributes = sourceType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
                    if (exposureAttributes.Length > 0)
                    {
                        result = ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
                    }
                }

                resolvedSpecialTypes.TryAdd(sourceType, result);
            }
            return result;
        }




        private static string GetJsonSimpleStringValue(string json, string key)
        {
            string keyTag = "\"" + key + "\":";
            int startIndex = json.IndexOf(keyTag);
            int endIndex;

            startIndex += keyTag.Length;

            if (json[startIndex] == 'n'
                && json[startIndex + 1] == 'u'
                && json[startIndex + 2] == 'l'
                && json[startIndex + 3] == 'l')
            {
                return null;
            }

            if (json[startIndex] == Structure.CharQuotationMark)
            {
                startIndex++;

                // value ends with quotation mark
                endIndex = json.IndexOf(Structure.CharQuotationMark, startIndex);
            }
            else
            {
                // value ends with comma
                endIndex = json.IndexOf(Structure.Comma, startIndex);

                if (endIndex == -1)
                {
                    endIndex = json.IndexOf(Structure.CharRightBrace, startIndex);
                }
            }

            return json.Substring(startIndex, endIndex - startIndex);
        }

        // ----------------------------------------------------------------------------------------
        #endregion










    }

}
