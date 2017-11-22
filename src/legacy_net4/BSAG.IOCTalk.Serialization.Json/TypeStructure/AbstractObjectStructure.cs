using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Reflection;
using System.Collections.Concurrent;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Abstract JSON object structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-04-02
    /// </remarks>
    public abstract class AbstractObjectStructure : AbstractStructure
    {
        #region AbstractObjectStructure fields
        // ----------------------------------------------------------------------------------------
        // AbstractObjectStructure fields
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// The object type
        /// </summary>
        protected Type type;

        /// <summary>
        /// Unknown type resolver handler
        /// </summary>
        protected UnknownTypeResolver unknownTypeResolver;

        /// <summary>
        /// The initial serialization context
        /// </summary>
        protected SerializationContext initialContext;

        /// <summary>
        /// Is object type flag
        /// </summary>
        protected bool isObject;
        
        /// <summary>
        /// Type serialization cache
        /// </summary>
        protected ConcurrentDictionary<Type, IJsonTypeStructure> typeSerializerCache;

        /// <summary>
        /// Type name serialization cache
        /// </summary>
        protected ConcurrentDictionary<string, IJsonTypeStructure> typeNameSerializerCache;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractObjectStructure constructors
        // ----------------------------------------------------------------------------------------
        // AbstractObjectStructure constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureComplexObject"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="key">The key.</param>
        /// <param name="context">The context.</param>
        /// <param name="isArrayItem">if set to <c>true</c> [is array item].</param>
        public AbstractObjectStructure(Type type, string key, SerializationContext context, bool isArrayItem)
            : base(key, isArrayItem)
        {
            this.type = type;
            this.unknownTypeResolver = context.UnknowTypeResolver;
            this.initialContext = context;
        }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractObjectStructure properties
        // ----------------------------------------------------------------------------------------
        // AbstractObjectStructure properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region AbstractObjectStructure methods
        // ----------------------------------------------------------------------------------------
        // AbstractObjectStructure methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Deserializes an object with a special type meta tag.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context">The context.</param>
        /// <param name="expectedSepIndex">Expected index of the sep.</param>
        /// <returns></returns>
        protected object DeserializeSpecialType(string json, ref int currentReadIndex, SerializationContext context, int expectedSepIndex)
        {
            // special type meta tag
            StructureString readTypeTagSerailaizer = new StructureString(Structure.TypeMetaTagKey, false);
            currentReadIndex = expectedSepIndex + 1;
            string typeName = (string)readTypeTagSerailaizer.Deserialize(json, ref currentReadIndex, context);

            if (typeNameSerializerCache == null)
                typeNameSerializerCache = new ConcurrentDictionary<string, IJsonTypeStructure>();

            IJsonTypeStructure currentObjectStructure;
            if (!typeNameSerializerCache.TryGetValue(typeName, out currentObjectStructure))
            {
                Type objectTargetType;
                if (!TypeService.TryGetTypeByName(typeName, out objectTargetType))
                {
                    throw new InvalidCastException(string.Format("Couldn't find Type: \"{0}\"", typeName));
                }
                currentObjectStructure = Structure.DetermineStructure(objectTargetType, this.key, context, true);

                typeNameSerializerCache.TryAdd(typeName, currentObjectStructure);
            }
            return currentObjectStructure.Deserialize(json, ref currentReadIndex, context);
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
