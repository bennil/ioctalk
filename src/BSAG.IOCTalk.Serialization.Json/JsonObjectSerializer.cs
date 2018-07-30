using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Serialization.Json.TypeStructure;
using System.Collections.Concurrent;
using BSAG.IOCTalk.Common.Attributes;

namespace BSAG.IOCTalk.Serialization.Json
{
    /// <summary>
    /// Delegate to get an unknown target type.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public delegate Type UnknownTypeResolver(SerializationContext context);

    /// <summary>
    /// Delegate to resolve a special serialization type/interface.
    /// </summary>
    /// <param name="sourceType">Type of the source.</param>
    /// <returns>Returns the special serialization type or null.</returns>
    public delegate Type SpecialTypeResolver(Type sourceType);


    /// <summary>
    /// JSON object serializer
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public class JsonObjectSerializer
    {
        #region JsonObjectSerializer fields
        // ----------------------------------------------------------------------------------------
        // JsonObjectSerializer fields
        // ----------------------------------------------------------------------------------------

        private ConcurrentDictionary<Type, IJsonTypeStructure> typeStructure = new ConcurrentDictionary<Type, IJsonTypeStructure>();
        private UnknownTypeResolver unknownTypeResolver;
        private SpecialTypeResolver specialTypeResolver;

        private bool isMissingFieldDataAllowed = true;
        private ConcurrentDictionary<Type, Type> resolvedDefaultSpecialTypes = new ConcurrentDictionary<Type, Type>();

        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonObjectSerializer constructors
        // ----------------------------------------------------------------------------------------
        // JsonObjectSerializer constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>JsonObjectSerializer</c> class.
        /// </summary>
        public JsonObjectSerializer()
        {
            this.specialTypeResolver = DefaultSpecialTypeResolver;
        }

        /// <summary>
        /// Creates a new instance of the <c>JsonObjectSerializer</c> class.
        /// </summary>
        /// <param name="unknownTypeResolver">The unknown type resolver.</param>
        public JsonObjectSerializer(UnknownTypeResolver unknownTypeResolver)
            : this()
        {
            this.unknownTypeResolver = unknownTypeResolver;
        }

        /// <summary>
        /// Creates a new instance of the <c>JsonObjectSerializer</c> class.
        /// </summary>
        /// <param name="unknownTypeResolver">The unknown type resolver.</param>
        /// <param name="specialTypeResolver">The special type resolver.</param>
        public JsonObjectSerializer(UnknownTypeResolver unknownTypeResolver, SpecialTypeResolver specialTypeResolver)
        {
            this.unknownTypeResolver = unknownTypeResolver;
            this.specialTypeResolver = specialTypeResolver;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonObjectSerializer properties
        // ----------------------------------------------------------------------------------------
        // JsonObjectSerializer properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the unknown type resolver delegate.
        /// </summary>
        public UnknownTypeResolver UnknownTypeResolver
        {
            get
            {
                return unknownTypeResolver;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether this instance is missing field data allowed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is missing field data allowed; otherwise, <c>false</c>.
        /// </value>
        public bool IsMissingFieldDataAllowed
        {
            get
            {
                return isMissingFieldDataAllowed;
            }
            set
            {
                isMissingFieldDataAllowed = value;
            }
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region JsonObjectSerializer methods
        // ----------------------------------------------------------------------------------------
        // JsonObjectSerializer methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Serializes the specified obj.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        public string Serialize(object obj, object contextObject)
        {
            StringBuilder sb = new StringBuilder();

            SerializationContext context = new SerializationContext();
            context.Serializer = this;
            context.ExternalContext = contextObject;
            context.UnknowTypeResolver = unknownTypeResolver;
            context.SpecialTypeResolver = specialTypeResolver;

            Type type = obj.GetType();
            IJsonTypeStructure structure;
            if (!typeStructure.TryGetValue(type, out structure))
            {
                structure = Structure.DetermineStructure(type, null, context, false);
                typeStructure[type] = structure;
            }

            structure.Serialize(sb, obj, context);

            return sb.ToString();
        }

        /// <summary>
        /// Deserializes the specified json.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="contextObject">The context object.</param>
        /// <returns></returns>
        public object Deserialize(string json, Type targetType, object contextObject)
        {
            SerializationContext context = new SerializationContext();
            context.IsDeserialize = true;
            context.ExternalContext = contextObject;
            context.Serializer = this;
            context.UnknowTypeResolver = unknownTypeResolver;
            context.SpecialTypeResolver = specialTypeResolver;

            IJsonTypeStructure structure;
            if (!typeStructure.TryGetValue(targetType, out structure))
            {
                structure = Structure.DetermineStructure(targetType, null, context, false);
                typeStructure[targetType] = structure;
            }

            int currentReadIndex = 0;
            return structure.Deserialize(json, ref currentReadIndex, context);
        }

        private Type DefaultSpecialTypeResolver(Type sourceType)
        {
            Type result;
            if (!resolvedDefaultSpecialTypes.TryGetValue(sourceType, out result))
            {
                // check expose sub type attribute
                var exposureAttributes = sourceType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
                if (exposureAttributes.Length > 0)
                {
                    result = ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
                }

                resolvedDefaultSpecialTypes.TryAdd(sourceType, result);
            }
            return result;
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
