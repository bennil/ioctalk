﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Attributes;
using System.Collections.Concurrent;

namespace BSAG.IOCTalk.Serialization.Json.TypeStructure
{
    /// <summary>
    /// Complex JSON object structure type
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2014-03-14
    /// </remarks>
    public sealed class StructureComplexObject : AbstractObjectStructure
    {
        #region StructureComplexObject fields
        // ----------------------------------------------------------------------------------------
        // StructureComplexObject fields
        // ----------------------------------------------------------------------------------------
        private Type concreteTargetType;

        private IJsonTypeStructure[] objectStructure;
        private Func<object, object>[] getAccessorByPropertyIndex;
        private Action<object, object>[] setAccessorByPropertyIndex;

        private List<StructureComplexOutOfOrder> outOfOrderStructures;
        private static object syncObj = new object();
        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureComplexObject constructors
        // ----------------------------------------------------------------------------------------
        // StructureComplexObject constructors
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureComplexObject"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="key">The key.</param>
        /// <param name="typeResolver">The type resolver.</param>
        public StructureComplexObject(Type type, string key, SerializationContext context, bool isArrayItem)
            : base(type, key, context, isArrayItem)
        {
            DetermineTypeStructure();
        }


        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureComplexObject properties
        // ----------------------------------------------------------------------------------------
        // StructureComplexObject properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether this instance is specialized sub type.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is specialized sub type; otherwise, <c>false</c>.
        /// </value>
        internal bool IsSpecializedSubType { get; set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region StructureComplexObject methods
        // ----------------------------------------------------------------------------------------
        // StructureComplexObject methods
        // ----------------------------------------------------------------------------------------

        private void DetermineTypeStructure()
        {
            this.isObject = type.Equals(typeof(object));

            if (this.isObject)
            {
                this.typeSerializerCache = new ConcurrentDictionary<Type, IJsonTypeStructure>();
            }
            else
            {
                // check if special type is exposed
                // this inital check is to keep back compatibility if the remote client does not expose a #type description tag. Then you can add an interface to interface mapping on the receiver side.
                if (initialContext.SpecialTypeResolver != null)
                {
                    Type specialType = initialContext.SpecialTypeResolver(type);
                    if (specialType != null)
                    {
                        type = specialType;
                    }
                }

                HashSet<string> existingProperties = new HashSet<string>();

                List<IJsonTypeStructure> objectStructureList = new List<IJsonTypeStructure>();
                List<Func<object, object>> getAccessors = new List<Func<object, object>>();
                List<Action<object, object>> setAccessors = new List<Action<object, object>>();
                AddProperties(type, objectStructureList, getAccessors, setAccessors, existingProperties);

                if (type.IsInterface)
                {
                    // analyze interface properties
                    foreach (Type interfaceType in type.GetInterfaces())
                    {
                        AddProperties(interfaceType, objectStructureList, getAccessors, setAccessors, existingProperties);
                    }
                }

                objectStructure = objectStructureList.ToArray();
                getAccessorByPropertyIndex = getAccessors.ToArray();
                setAccessorByPropertyIndex = setAccessors.ToArray();
            }
        }

        private void AddProperties(Type type, List<IJsonTypeStructure> objectStructureList, List<Func<object, object>> getAccessors, List<Action<object, object>> setAccessors, HashSet<string> existingProperties)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Public))
            {
                if (existingProperties.Contains(prop.Name))
                {
                    continue;
                }
                if (prop.GetIndexParameters().Length > 0)
                {
                    // ignore index properties
                    continue;
                }
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }
                if (prop.PropertyType.Equals(type))
                {
                    // ignore same nested types because of recursive endless loop
                    continue;
                }
                objectStructureList.Add(Structure.DetermineStructure(prop.PropertyType, prop.Name, initialContext, false));

                getAccessors.Add(prop.GenerateGetterFunc());
                setAccessors.Add(prop.GenerateSetterAction());

                existingProperties.Add(prop.Name);
            }
        }


        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="context">The context.</param>
        public override void Serialize(StringBuilder sb, object obj, SerializationContext context)
        {
            if (obj == null)
            {
                Structure.SerializeNull(key, sb);
                return;
            }

            Type objectType = obj.GetType();
            Type exposedSubInterfaceType = null;
            bool isSpecialTargetType = false;
            if (context.SpecialTypeResolver != null
                && !IsSpecializedSubType) // do not recheck specialized sub structure
            {
                var exposedSubInterfaceTypeTemp = context.SpecialTypeResolver(objectType);

                if (exposedSubInterfaceTypeTemp != null
                    && type.IsAssignableFrom(exposedSubInterfaceTypeTemp))
                {
                    // expose specialized sub interface type
                    exposedSubInterfaceType = exposedSubInterfaceTypeTemp;
                    isSpecialTargetType = true;
                    if (this.typeSerializerCache == null)
                        this.typeSerializerCache = new ConcurrentDictionary<Type, IJsonTypeStructure>();
                }
            }

            if (isObject || isSpecialTargetType)
            {
                // undefined object -> determine target type first
                IJsonTypeStructure currentObjectStructure;
                if (!typeSerializerCache.TryGetValue(objectType, out currentObjectStructure))
                {
                    Type targetType = null;
                    if (isSpecialTargetType)
                    {
                        // expose specialized sub interface type
                        targetType = exposedSubInterfaceType;
                        isSpecialTargetType = true;
                    }
                    else
                    {
                        if (unknownTypeResolver != null)
                        {
                            context.Key = this.key;
                            targetType = unknownTypeResolver(context);
                        }
                        if (targetType == null)
                        {
                            targetType = objectType;
                        }
                    }

                    currentObjectStructure = Structure.DetermineStructure(targetType, this.key, context, this.isArrayItem);

                    if (isSpecialTargetType)
                    {
                        ((StructureComplexObject)currentObjectStructure).IsSpecializedSubType = true;
                    }

                    typeSerializerCache[objectType] = currentObjectStructure;
                }

                currentObjectStructure.Serialize(sb, obj, context);
            }
            else
            {
                if (keyExpected)
                {
                    sb.Append(Structure.QuotationMark);
                    sb.Append(key);
                    sb.Append(Structure.QuotationColonSeparator);
                }
                object oldParentObj = context.ParentObject;
                context.ParentObject = obj;

                sb.Append("{");

                if (IsSpecializedSubType)
                {
                    sb.Append(Structure.TypeMetaTagJson);
                    sb.Append(this.type.FullName);
                    sb.Append(Structure.CharQuotationMark);

                    if (objectStructure.Length > 0)
                        sb.Append(Structure.CharComma);
                }

                int endIndex = objectStructure.Length - 1;
                for (int i = 0; i <= endIndex; i++)
                {
                    // get property value
                    object propertyValue = getAccessorByPropertyIndex[i](obj);

                    objectStructure[i].Serialize(sb, propertyValue, context);

                    if (i < endIndex)
                        sb.Append(Structure.Comma);
                }

                context.ParentObject = oldParentObj;

                sb.Append("}");
            }

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
            // check if value is null
            int expectedSepIndex = currentReadIndex + keyLength;
            if (json[expectedSepIndex] == 'n'
                && json[expectedSepIndex + 1] == 'u'
                && json[expectedSepIndex + 2] == 'l'
                && json[expectedSepIndex + 3] == 'l')
            {
                currentReadIndex = expectedSepIndex + 4;
                return null;
            }

            if (json[expectedSepIndex + 1] == Structure.CharQuotationMark
                && json[expectedSepIndex + 2] == '#'
                && json[expectedSepIndex + 3] == 't'
                && json[expectedSepIndex + 4] == 'y'
                && json[expectedSepIndex + 5] == 'p'
                && json[expectedSepIndex + 6] == 'e'
                )
            {
                return DeserializeSpecialType(json, ref currentReadIndex, context, expectedSepIndex);
            }


            if (isObject)
            {
                Type objectTargetType = null;
                if (unknownTypeResolver != null)
                {
                    context.Key = this.key;
                    context.ValueStartIndex = currentReadIndex;
                    context.ArrayIndex = null;
                    context.JsonString = json;
                    if (type.IsInterface)
                        context.InterfaceType = this.type;

                    objectTargetType = unknownTypeResolver(context);

                    if (context.InterfaceType != null)
                        context.InterfaceType = null;
                }

                if (objectTargetType == null)
                {
                    objectTargetType = Structure.GetDefaultType(json, expectedSepIndex);

                    if (objectTargetType == null)
                    {
                        throw new InvalidOperationException("Can't determine target type for JSON key: \"" + context.Key + "\"");
                    }
                }

                IJsonTypeStructure currentObjectStructure;
                if (!typeSerializerCache.TryGetValue(objectTargetType, out currentObjectStructure))
                {
                    currentObjectStructure = Structure.DetermineStructure(objectTargetType, this.key, context, this.isArrayItem);
                    typeSerializerCache[objectTargetType] = currentObjectStructure;
                }

                return currentObjectStructure.Deserialize(json, ref currentReadIndex, context);
            }
            else
            {
                if (json[currentReadIndex] != Structure.CharLeftBrace
                    && !isArrayItem)
                {
                    if (json[currentReadIndex] == Structure.CharComma)
                    {
                        currentReadIndex += 1;
                    }

                    expectedSepIndex = currentReadIndex + keyLength - 1;

                    if (json[expectedSepIndex] == Structure.CharColon)
                    {
                        currentReadIndex = expectedSepIndex + 1;
                    }
                    else
                    {
                        throw new Exception("Unexpected JSON data!");
                    }
                }

                if (this.concreteTargetType == null)
                {
                    // try get static target type
                    if (type.IsInterface || type.IsAbstract)
                    {
                        context.InterfaceType = type;
                        context.Key = key;

                        Type itemTypeResolved = unknownTypeResolver(context);
                        if (itemTypeResolved != null)
                        {
                            concreteTargetType = itemTypeResolved;
                        }
                        context.InterfaceType = null;
                    }
                    else
                    {
                        concreteTargetType = type;
                    }
                }

                // create target object
                object target;
                try
                {
                    target = TypeService.CreateInstance(this.concreteTargetType);
                }
                catch (Exception ex)
                {
                    throw new Exception("Can't create object instance for \"" + this.concreteTargetType + "\" - \"" + type + "\"!", ex);
                }
                object oldParentObj = context.ParentObject;
                context.ParentObject = target;

                // jump over opening brace
                currentReadIndex += 1;

                for (int propIndex = 0; propIndex < objectStructure.Length; propIndex++)
                {
                    IJsonTypeStructure structure = objectStructure[propIndex];
                    var setPropertyAccessor = setAccessorByPropertyIndex[propIndex];

                    // check out of order json
                    int expectedKeyStartIndex = currentReadIndex + 1;
                    int actualKeyIndex = json.IndexOf(structure.Key, expectedKeyStartIndex);
                    if (actualKeyIndex != expectedKeyStartIndex
                        || json[structure.Key.Length + actualKeyIndex] != Structure.CharQuotationMark)  // Recognize key with the same begin string but different ending
                    {
                        // out of order key recognized  


                        int keyEndIndex = json.IndexOf(Structure.CharQuotationMark, expectedKeyStartIndex);
                        int? keyStructureIndex = null;
                        StructureComplexOutOfOrder cachedOutOfOrderStruct = null;
                        string actualKey = null;
                        if (keyEndIndex != -1)  // ignore if end of json is reached
                        {
                            actualKey = json.Substring(expectedKeyStartIndex, keyEndIndex - expectedKeyStartIndex);

                            // check if out of order structure is already cached
                            if (outOfOrderStructures != null)
                            {
                                for (int indexCachedOutOfOrder = 0; indexCachedOutOfOrder < outOfOrderStructures.Count; indexCachedOutOfOrder++)
                                {
                                    var cachedItem = outOfOrderStructures[indexCachedOutOfOrder];

                                    if (cachedItem.ObjectStructure[propIndex].Key == actualKey)
                                    {
                                        cachedOutOfOrderStruct = cachedItem;
                                        break;
                                    }
                                }
                            }

                            if (cachedOutOfOrderStruct == null)
                            {
                                // find out of order key
                                keyStructureIndex = FindOutOfOrderKey(objectStructure, actualKey);
                            }
                        }

                        if (cachedOutOfOrderStruct != null)
                        {
                            structure = cachedOutOfOrderStruct.ObjectStructure[propIndex];
                            setPropertyAccessor = cachedOutOfOrderStruct.SetAccessorByPropertyIndex[propIndex];
                        }
                        else if (keyStructureIndex.HasValue)
                        {
                            lock (syncObj)
                            {
                                // create or move out of order structure

                                if (outOfOrderStructures == null)
                                    outOfOrderStructures = new List<StructureComplexOutOfOrder>();

                                // try reuse existing out of order array
                                StructureComplexOutOfOrder currentOutOfOrderStruct;
                                var existing = outOfOrderStructures.Where(ooo => ooo.MaxTargetIndex < propIndex).FirstOrDefault();

                                IJsonTypeStructure[] sourceStructure = objectStructure;
                                Func<object, object>[] sourceGetAccessorByPropertyIndex = getAccessorByPropertyIndex;
                                Action<object, object>[] sourceSetAccessorByPropertyIndex = setAccessorByPropertyIndex;

                                // create new cloned out of order strucutre
                                if (existing == null)
                                {
                                    currentOutOfOrderStruct = new StructureComplexOutOfOrder()
                                    {
                                        ObjectStructure = (IJsonTypeStructure[])sourceStructure.Clone(),
                                        GetAccessorByPropertyIndex = (Func<object, object>[])sourceGetAccessorByPropertyIndex.Clone(),
                                        SetAccessorByPropertyIndex = (Action<object, object>[])sourceSetAccessorByPropertyIndex.Clone()
                                    };
                                }
                                else
                                {
                                    currentOutOfOrderStruct = existing;
                                    sourceStructure = existing.ObjectStructure;
                                    structure = sourceStructure[propIndex];
                                    sourceGetAccessorByPropertyIndex = existing.GetAccessorByPropertyIndex;
                                    sourceSetAccessorByPropertyIndex = existing.SetAccessorByPropertyIndex;

                                    // search in chached object structure again
                                    keyStructureIndex = FindOutOfOrderKey(sourceStructure, actualKey);
                                }

                                // switch object structure to received json order
                                IJsonTypeStructure foundOutOfOrderStructure = sourceStructure[keyStructureIndex.Value];
                                currentOutOfOrderStruct.ObjectStructure[propIndex] = foundOutOfOrderStructure;
                                currentOutOfOrderStruct.ObjectStructure[keyStructureIndex.Value] = structure;
                                structure = foundOutOfOrderStructure;
                                currentOutOfOrderStruct.MaxTargetIndex = propIndex;

                                // switch value setter
                                var currentAccessor = sourceGetAccessorByPropertyIndex[propIndex];
                                var foundOutOfOrderAccessor = sourceGetAccessorByPropertyIndex[keyStructureIndex.Value];
                                currentOutOfOrderStruct.GetAccessorByPropertyIndex[propIndex] = foundOutOfOrderAccessor;
                                currentOutOfOrderStruct.GetAccessorByPropertyIndex[keyStructureIndex.Value] = currentAccessor;

                                var currentAccessorSet = sourceSetAccessorByPropertyIndex[propIndex];
                                var foundOutOfOrderAccessorSet = sourceSetAccessorByPropertyIndex[keyStructureIndex.Value];
                                currentOutOfOrderStruct.SetAccessorByPropertyIndex[propIndex] = foundOutOfOrderAccessorSet;
                                currentOutOfOrderStruct.SetAccessorByPropertyIndex[keyStructureIndex.Value] = currentAccessorSet;

                                setPropertyAccessor = foundOutOfOrderAccessorSet;

                                if (existing == null)
                                    outOfOrderStructures.Add(currentOutOfOrderStruct);
                            }
                        }
                        else
                        {
                            if (context.Serializer.IsMissingFieldDataAllowed)
                            {
                                if (SkipJsonElement(json, ref currentReadIndex))
                                {
                                    propIndex--;    // re-read current structure
                                }
                                continue;   // ignore missing field
                            }
                            else
                            {
                                throw new KeyNotFoundException(string.Format("The key: \"{0}\" is not present in the object structure! JSON: \"{1}\"; Target Type: \"{2}\"", actualKey, json, this.type.AssemblyQualifiedName));
                            }
                        }

                    }

                    object value = structure.Deserialize(json, ref currentReadIndex, context);

                    if (value != null)
                    {
                        setPropertyAccessor(target, value);
                    }

                    if (json[currentReadIndex] == Structure.CharRightBrace)
                    {
                        // end reached
                        currentReadIndex++;
                        break;
                    }

                    currentReadIndex++;
                }

                if (json[currentReadIndex - 1] != Structure.CharRightBrace)
                {
                    if (context.Serializer.IsMissingFieldDataAllowed)
                    {
                        // json object contains more data than expected from the type structure
                        // skip futher object data
                        SkipJsonObject(json, ref currentReadIndex);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("The json object contains more data than expected in target type! JSON: \"{0}\"; Key: \"{1}\"; Target Type: \"{2}\"", json, this.key, this.type.AssemblyQualifiedName));
                    }
                }

                context.ParentObject = oldParentObj;

                return target;
            }
        }

        private int? FindOutOfOrderKey(IJsonTypeStructure[] sourceObjStruct, string actualKey)
        {
            int? keyStructureIndex = null;
            for (int tempPropIndex = 0; tempPropIndex < sourceObjStruct.Length; tempPropIndex++)
            {
                if (sourceObjStruct[tempPropIndex].Key == actualKey)
                {
                    keyStructureIndex = tempPropIndex;
                    break;
                }
            }

            return keyStructureIndex;
        }

        private static bool SkipJsonElement(string json, ref int currentReadIndex)
        {
            int expectedKeyStartIndex = currentReadIndex + 1;
            int keyEndIndex = json.IndexOf(Structure.CharQuotationMark, expectedKeyStartIndex);

            if (keyEndIndex != -1)  // ignore if end of json is reached
            {
                if (json[keyEndIndex + 1] == Structure.CharColon)
                {
                    if (json[keyEndIndex + 2] == Structure.CharQuotationMark)
                    {
                        // Read string considering escaped quotations
                        int endValueIndex = json.IndexOf(Structure.QuotationMark, keyEndIndex + 3);
                        while (json[endValueIndex - 1] == Structure.CharEscape)
                        {
                            // escaped quotation mark
                            // read further to find string ending
                            endValueIndex = json.IndexOf(Structure.QuotationMark, endValueIndex + 1);
                        }

                        currentReadIndex = endValueIndex + 2;
                        return true;
                    }
                    else if (json[keyEndIndex + 2] == Structure.CharLeftBrace)
                    {
                        // element is a sub object
                        // skip object
                        currentReadIndex = keyEndIndex + 3;
                        if (SkipJsonObject(json, ref currentReadIndex))
                        {
                            currentReadIndex++; // skip property separator
                            return true;
                        }
                    }
                    else
                    {
                        int endValueIndex = json.IndexOfAny(Structure.EndValueChars, keyEndIndex + 2);
                        if (endValueIndex > 0)
                        {
                            currentReadIndex = endValueIndex + 1;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool SkipJsonObject(string json, ref int currentReadIndex)
        {
            int objectEndIndex = json.IndexOf(Structure.CharRightBrace, currentReadIndex);

            if (objectEndIndex != -1)  // ignore if end of json is reached
            {
                int subObjectIndex = json.IndexOf(Structure.CharLeftBrace, currentReadIndex);

                if (subObjectIndex != -1
                    && subObjectIndex >= currentReadIndex
                    && subObjectIndex < objectEndIndex)
                {
                    // sub object -> read next end
                    int openCount = 1;
                    for (int charIndex = currentReadIndex; charIndex < json.Length; charIndex++)
                    {
                        char c = json[charIndex];

                        if (c == Structure.CharLeftBrace)
                        {
                            openCount++;
                        }
                        else if (c == Structure.CharRightBrace)
                        {
                            openCount--;

                            if (openCount <= 0)
                            {
                                // end object index reached
                                currentReadIndex = charIndex + 1;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    currentReadIndex = objectEndIndex + 1;
                    return true;
                }

            }

            return false;
        }

        // ----------------------------------------------------------------------------------------
        #endregion
    }

}
