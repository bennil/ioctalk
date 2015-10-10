using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using BSAG.IOCTalk.Common.Attributes;
using System.Reflection;
using System.Collections.Concurrent;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// JSON array structure
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public sealed class StructureArray : AbstractObjectStructure
    {
        #region StructureArray fields
        // ----------------------------------------------------------------------------------------
        // StructureArray fields
        // ----------------------------------------------------------------------------------------
        //private Type enumerableType;
        private Type itemType;
        //private Type concreteDeserializeItemType;
        private Type targetCollectionType;

        private IJsonTypeStructure itemSerializer;
        private IJsonTypeStructure itemDeSerializer;
        
        private ConcurrentDictionary<Type, MethodInfo> specialCollectionTypeAddMethodCache;
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureArray constructors
        // ----------------------------------------------------------------------------------------
        // StructureArray constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Creates a new instance of the <c>StructureArray</c> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="enumerableType">Type of the enumerable.</param>
        /// <param name="context">The context.</param>
        /// <param name="isArrayItem">if set to <c>true</c> [is array item].</param>
        public StructureArray(string key, Type enumerableType, SerializationContext context, bool isArrayItem)
            : base(enumerableType, key, context, isArrayItem)
        {
            if (enumerableType.IsArray)
            {
                itemType = enumerableType.GetElementType();
            }
            else if (typeof(IEnumerable).IsAssignableFrom(enumerableType))
            {
                if (enumerableType.IsClass)
                {
                    targetCollectionType = enumerableType;

                    if (targetCollectionType.IsGenericType)
                    {
                        this.itemType = GetGenericEnumerableType(enumerableType);
                    }
                    else
                    {
                        // check if class implements the IEnumerable<T> interface
                        foreach (var enumerableInterf in enumerableType.GetInterfaces())
                        {
                            if (enumerableInterf.IsGenericType
                                && enumerableInterf.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            {
                                itemType = enumerableInterf.GetGenericArguments()[0];
                                break;
                            }
                        }

                        if (itemType == null)
                        {
                            // use object as item type
                            itemType = typeof(object);
                        }
                    }
                }
                else if (enumerableType.IsGenericType)
                {
                    // collection is a generic for IEnumerable<T>
                    this.itemType = GetGenericEnumerableType(enumerableType);
                    this.targetCollectionType = typeof(List<>).MakeGenericType(itemType);
                }
                else
                {
                    // use array list as untyped object collection
                    targetCollectionType = typeof(ArrayList);
                    itemType = typeof(object);
                }
            }
            else
            {
                throw new Exception("Enumeration type \"" + enumerableType.FullName + "\" is not supported!");
            }

            this.isObject = itemType.Equals(typeof(object)); //Structure.TryGetConcreteTargetClass(itemType, this.key, typeResolver, out concreteDeserializeItemType);

            if (isObject)
            {
                this.typeSerializerCache = new ConcurrentDictionary<Type, IJsonTypeStructure>();
            }
        }

        private Type GetGenericEnumerableType(Type enumerableType)
        {
            // IEnumerable<T> handling
            Type[] genericTypes = enumerableType.GetGenericArguments();
            if (genericTypes.Length == 1)
            {
                return genericTypes[0];
            }
            else
            {
                throw new Exception("More than one generic types are not supported in collections! Type: " + enumerableType.FullName);
            }
        }



        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureArray properties
        // ----------------------------------------------------------------------------------------
        // StructureArray properties
        // ----------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureArray methods
        // ----------------------------------------------------------------------------------------
        // StructureArray methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (keyExpected)
            {
                sb.Append(Structure.QuotationMark);
                sb.Append(key);
                if (obj == null)
                {
                    sb.Append(Structure.QuotationColonNullValue);
                    return;
                }
                else
                {
                    sb.Append(Structure.QuotationColonSeparator);
                }
            }

            sb.Append(Structure.CharLeftSquareBrace);


            Type collType = obj.GetType();

            // check if own collection implementation exposes special target type // todo: check if possible and required
            //bool specialTypeAdded = false;
            //if (!collType.IsArray
            //    && !IsSystemCollection(collType))
            //    //&& !checkedSubInterfaceTypes.Contains(collType))
            //{
            //    var exposureAttributes = collType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
            //    if (exposureAttributes.Length > 0)
            //    {
            //        // expose specialized sub interface type
            //        if (this.typeSerializerCache == null)
            //            this.typeSerializerCache = new Dictionary<Type, IJsonTypeStructure>();

            //        // add typed collection attribute
            //        sb.Append(Structure.TypeMetaTagJson);
            //        sb.Append(this.type.FullName);
            //        sb.Append(Structure.CharQuotationMark);

            //        specialTypeAdded = true;
            //    }
            //    //checkedSubInterfaceTypes.Add(collType);
            //}

            IEnumerable collection = (IEnumerable)obj;

            int count = 0;
            foreach (var item in collection)
            {
                //if (specialTypeAdded
                //    && count == 0)
                //{
                //    sb.Append(Structure.CharComma);
                //}

                if (item != null)
                {
                    if (isObject)
                    {
                        // determine serialize type
                        Type itemType = item.GetType();
                        IJsonTypeStructure currentObjectStructure;
                        if (!typeSerializerCache.TryGetValue(itemType, out currentObjectStructure))
                        {
                            Type targetType = null;
                            if (unknownTypeResolver != null)
                            {
                                context.Key = this.key;
                                context.ArrayIndex = count;

                                targetType = unknownTypeResolver(context);

                                context.ArrayIndex = null;
                            }
                            if (targetType == null)
                            {
                                targetType = itemType;
                            }
                            currentObjectStructure = Structure.DetermineStructure(targetType, GetNestedArrayKey(this.key, count), context, true);

                            typeSerializerCache[itemType] = currentObjectStructure;
                        }

                        currentObjectStructure.Serialize(sb, item, context);
                    }
                    else
                    {
                        if (itemSerializer == null)
                        {
                            this.itemSerializer = Structure.DetermineStructure(itemType, key, context, true);
                        }

                        // use static serialize type
                        itemSerializer.Serialize(sb, item, context);
                    }
                }
                else
                {
                    sb.Append(Structure.NullValue);
                }
                sb.Append(Structure.CharComma);
                count++;
            }
            if (count > 0)
            {
                sb.Length -= 1; // remove last comma
            }

            sb.Append(Structure.CharRightSquareBracet);

        }

        /// <summary>
        /// Deserializes the specified json string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="currentReadIndex">Index of the current read.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override object Deserialize(string json, ref int currentReadIndex, SerializationContext context)
        {
            currentReadIndex = currentReadIndex + keyLength;

            if (json[currentReadIndex] == Structure.CharLeftSquareBrace)
            {
                currentReadIndex++;

                if (type.IsArray)
                {
                    ArrayList soureList = new ArrayList();
                    while (true)
                    {
                        if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                        {
                            // is empty array
                            currentReadIndex += 1;
                            break;
                        }

                        object itemValue = DeserializeArrayItem(json, ref currentReadIndex, soureList.Count, context);

                        soureList.Add(itemValue);

                        if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                        {
                            currentReadIndex += 1;
                            break;
                        }

                        currentReadIndex += 1;
                    }

                    // copy items to target array
                    Array targetValueArray = Array.CreateInstance(itemType, soureList.Count);
                    for (int i = 0; i < soureList.Count; i++)
                    {
                        targetValueArray.SetValue(soureList[i], i);
                    }
                    return targetValueArray;
                }
                else
                {
                    object targetCollectionObj = Activator.CreateInstance(targetCollectionType);

                    if (targetCollectionObj is IList)
                    {
                        IList targetCollection = (IList)targetCollectionObj;

                        while (true)
                        {
                            if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                            {
                                // is empty array
                                currentReadIndex += 1;
                                break;
                            }

                            object itemValue = DeserializeArrayItem(json, ref currentReadIndex, targetCollection.Count, context);
                            targetCollection.Add(itemValue);

                            if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                            {
                                currentReadIndex += 1;
                                break;
                            }

                            currentReadIndex += 1;
                        }
                        return targetCollection;
                    }
                    else
                    {
                        // collection does not implement IList
                        // Unable to cast to ICollection<T>
                        // try to get Add method using reflection
                        MethodInfo miAdd;
                        if (specialCollectionTypeAddMethodCache == null
                            || !specialCollectionTypeAddMethodCache.TryGetValue(targetCollectionType, out miAdd))
                        {
                            miAdd = targetCollectionType.GetMethod("Add");
                            if (miAdd != null)
                            {
                                specialCollectionTypeAddMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
                                specialCollectionTypeAddMethodCache.TryAdd(targetCollectionType, miAdd);
                            }
                            else
                            {
                                throw new InvalidCastException("The collection type " + targetCollectionType.FullName + " is not supported! Unable to get items \"Add\" method.");
                            }
                        }

                        int count = 0;
                        while (true)
                        {
                            if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                            {
                                // is empty array
                                currentReadIndex += 1;
                                break;
                            }

                            object itemValue = DeserializeArrayItem(json, ref currentReadIndex, count, context);
                            miAdd.Invoke(targetCollectionObj, new object[] { itemValue });

                            if (json[currentReadIndex] == Structure.CharRightSquareBracet)
                            {
                                currentReadIndex += 1;
                                break;
                            }

                            currentReadIndex += 1;
                            count++;
                        }
                        return targetCollectionObj;

                    }

                }
            }
            else if (json[currentReadIndex] == 'n'
                            && json[currentReadIndex + 1] == 'u'
                            && json[currentReadIndex + 2] == 'l'
                            && json[currentReadIndex + 3] == 'l')
            {
                currentReadIndex += 4;
                return null;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private object DeserializeArrayItem(string json, ref int currentReadIndex, int arrayItemIndex, SerializationContext context)
        {
            if (json[currentReadIndex] == 'n'
                && json[currentReadIndex + 1] == 'u'
                && json[currentReadIndex + 2] == 'l'
                && json[currentReadIndex + 3] == 'l')
            {
                currentReadIndex = currentReadIndex + 4;
                return null;
            }

            //Type objectTargetType = null;
            if (json[currentReadIndex + 2] == '#'
                && json[currentReadIndex + 3] == 't'
                && json[currentReadIndex + 4] == 'y'
                && json[currentReadIndex + 5] == 'p'
                && json[currentReadIndex + 6] == 'e'
                )
            {
                return DeserializeSpecialType(json, ref currentReadIndex, context, currentReadIndex);
            }

            object itemValue;
            if (this.isObject)
            {
                // determine target type during deserialization
                Type type = null;
                if (unknownTypeResolver != null)
                {
                    context.Key = this.key;
                    context.ValueStartIndex = currentReadIndex;
                    context.ArrayIndex = arrayItemIndex;
                    context.JsonString = json;

                    type = unknownTypeResolver(context);

                    context.ArrayIndex = null;
                }

                if (type == null || type.Equals(typeof(object)))
                {
                    type = Structure.GetDefaultType(json, currentReadIndex);

                    if (type == null)
                    {
                        int readCount = Math.Min(json.Length - currentReadIndex, 25);
                        string jsonPart = json.Substring(currentReadIndex, readCount);

                        throw new InvalidOperationException(string.Format("Unable to get the target type for array object index {0}; Key: {1}; JSON part: \"{2}\"", arrayItemIndex, this.key, jsonPart));
                    }
                }

                IJsonTypeStructure currentObjectStructure;
                if (!typeSerializerCache.TryGetValue(type, out currentObjectStructure))
                {
                    currentObjectStructure = Structure.DetermineStructure(type, GetNestedArrayKey(this.key, arrayItemIndex), context, true);
                    typeSerializerCache[type] = currentObjectStructure;
                }

                itemValue = currentObjectStructure.Deserialize(json, ref currentReadIndex, context);
            }
            else
            {
                if (itemDeSerializer == null)
                {
                    itemDeSerializer = Structure.DetermineStructure(itemType, key, context, true);
                }

                // fixed array target type
                itemValue = itemDeSerializer.Deserialize(json, ref currentReadIndex, context);
            }

            return itemValue;
        }

        private string GetNestedArrayKey(string key, int arrayItemIndex)
        {
            return string.Join(Structure.Comma, key, arrayItemIndex);
        }


        private static bool IsSystemCollection(Type collectionType)
        {
            return collectionType.Namespace.StartsWith("System.Collections");
        }

        // ----------------------------------------------------------------------------------------
        #endregion

    }

}
