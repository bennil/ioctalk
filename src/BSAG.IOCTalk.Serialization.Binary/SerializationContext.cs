using BSAG.IOCTalk.Common.Attributes;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values;
using BSAG.IOCTalk.Serialization.Binary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary
{
    /// <summary>
    /// Class SerializationContext.
    /// </summary>
    public class SerializationContext : ISerializeContext
    {
        private IUnknowContextTypeResolver unknowContextTypeResolver;

        private Dictionary<Type, IValueItem> differentTargetTypes = new Dictionary<Type, IValueItem>();
        private HashSet<uint> publishedMetaInfosPerInstance = new HashSet<uint>();
        private Dictionary<Type, List<string>> stringHashItemProperties = new Dictionary<Type, List<string>>();
        private Dictionary<uint, string> stringHashValues;

        public SerializationContext(BinarySerializer serializer, bool isDeserialize, object contextObj = null)
        {
            this.serializer = serializer;
            this.unknowContextTypeResolver = serializer.UnknowContextTypeResolver;
            this.IsDeserialize = isDeserialize;
            this.ExternalContext = contextObj;
        }


        private BinarySerializer serializer;


        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the external context object.
        /// </summary>
        /// <value>
        /// The context object.
        /// </value>
        public object ExternalContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is deserialize.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deserialize; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeserialize { get; set; }

        /// <summary>
        /// Gets or sets the parent serialization object.
        /// </summary>
        /// <value>
        /// The parent object.
        /// </value>
        public object ParentObject { get; set; }

        /// <summary>
        /// Gets or sets the 2nd level parent object.
        /// </summary>
        public object ParentParentObject { get; set; }


        public int ChildLevel { get; set; }


        /// <summary>
        /// Gets or sets the index of the array.
        /// </summary>
        /// <value>
        /// The index of the array.
        /// </value>
        public int? ArrayIndex { get; set; }

        public BinarySerializer Serializer
        {
            get
            {
                return serializer;
            }
        }


        public IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType)
        {
            var result = unknowContextTypeResolver.DetermineSpecialInterfaceType(objectType, defaultInterfaceType, this);

            if (result == null)
            {
                result = DetermineSpecialInterfaceTypeFallback(objectType, defaultInterfaceType);
            }

            return result;
        }


        public IValueItem DetermineSpecialInterfaceTypeFallback(Type objectType, Type defaultInterfaceType)
        {
            IValueItem result;
            if (differentTargetTypes.TryGetValue(objectType, out result))
            {
                if (result is ComplexStructure cs)
                {
                    // check if expected interface is assignable
                    if (defaultInterfaceType.IsAssignableFrom(cs.RuntimeType) || defaultInterfaceType.Equals(typeof(object)))
                        return result;
                }
                else
                    return result;
            }

            // ExposeSubTypeAttribute not supported in binary serialization use RegisterExposedSubInterfaceForType  instead
            //Type diffType = null;
            //// check expose sub type attribute
            //var exposureAttributes = objectType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
            //if (exposureAttributes.Length > 0)
            //{
            //    diffType = ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
            //}

            var differentTargetStructure = RegisterDifferentTargetType(objectType, defaultInterfaceType, null, true);
            return differentTargetStructure;

        }


        public Type DetermineTargetType(Type interfaceType)
        {
            return unknowContextTypeResolver.DetermineTargetType(interfaceType, this);
        }

        public IValueItem GetByType(Type type)
        {
            return serializer.GetByType(type, this);
        }

        public IValueItem GetByTypeId(uint typeId)
        {
            return serializer.GetByTypeId(typeId);
        }



        public void Reset(object externalContext)
        {
            this.ExternalContext = externalContext;
            this.Key = null;
            this.ParentObject = null;
            this.ParentParentObject = null;
            this.ChildLevel = 0;
            this.ArrayIndex = null;
        }


        public bool TryGetDifferentTargetType(Type objectType, out IValueItem targetItem)
        {
            return differentTargetTypes.TryGetValue(objectType, out targetItem);
        }


        public IValueItem RegisterDifferentTargetType(Type objectType, Type defaultInterfaceType, Type diffType, bool cacheDifference)
        {
            if (diffType != null)
            {
                if (diffType != defaultInterfaceType)
                {
                    var differentTargetStructure = serializer.GetByType(diffType, this);
                    
                    if (cacheDifference)
                    {
                        if (differentTargetStructure is ComplexStructure)
                        {
                            ((ComplexStructure)differentTargetStructure).CheckDifferentType = false;
                        }
                        differentTargetTypes[objectType] = differentTargetStructure;
                    }

                    return differentTargetStructure;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (cacheDifference)
                    differentTargetTypes[objectType] = null;

                return null;
            }
        }

        public bool IsWriteTypeMetaInfoRequired(uint typeId)
        {
            if (publishedMetaInfosPerInstance.Contains(typeId))
            {
                return false;
            }
            else
            {
                publishedMetaInfosPerInstance.Add(typeId);
                return true;
            }
        }








        public void RegisterStringHashProperty(Type type, string propertyName)
        {
            if (stringHashValues == null)
                stringHashValues = new Dictionary<uint, string>();

            List<string> props;
            if (!stringHashItemProperties.TryGetValue(type, out props))
            {
                props = new List<string>();
                stringHashItemProperties.Add(type, props);
            }

            if (!props.Contains(propertyName))
                props.Add(propertyName);
        }

        public bool IsStringHashProperty(Type type, string propertyName)
        {
            List<string> props;
            if (stringHashItemProperties.TryGetValue(type, out props))
            {
                return props.Contains(propertyName);
            }
            return false;
        }

        public bool IsWriteHashStringRequired(string stringValue, out uint stringHashCode)
        {
            stringHashCode = Hashing.CreateHash(stringValue);

            if (stringHashValues.ContainsKey(stringHashCode))
            {
                return false;
            }
            else
            {
                RegisterStringHashCodeValue(stringValue, stringHashCode);
                return true;
            }
        }

        public void RegisterStringHashCodeValue(string stringValue, uint stringHashCode)
        {
            string existingStr;
            if (stringHashValues.TryGetValue(stringHashCode, out existingStr))
            {
                if (!stringValue.Equals(existingStr))
                {
                    throw new InvalidOperationException($"String hash duplicate for {stringHashCode} - cached string: \"{existingStr}\" - new string: \"{stringValue}\"");
                }
            }
            else
            {
                stringHashValues.Add(stringHashCode, stringValue);
            }
        }

        public string GetHashString(uint hashCode)
        {
            string result;
            stringHashValues.TryGetValue(hashCode, out result);
            return result;
        }

    }
}
