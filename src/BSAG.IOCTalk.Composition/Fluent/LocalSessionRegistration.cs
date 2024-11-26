using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Composition.Fluent
{
    public class LocalSessionRegistration<InterfaceType>
    {
        private TalkCompositionHost source;

        internal LocalSessionRegistration(TalkCompositionHost source)
        {
            this.source = source;
        }

        public LocalSessionRegistration<InterfaceType> TargetAlsoImplements<AdditionalInterface>()
        {
            if (source.LocalSessionServiceTypeMappings == null)
            {
                source.LocalSessionServiceTypeMappings = new Dictionary<Type, Type>();
            }

            Type sourceType = typeof(InterfaceType);
            Type targetAlsoImplements = typeof(AdditionalInterface);

            if (!targetAlsoImplements.IsInterface)
                throw new InvalidOperationException($"Target type \"{targetAlsoImplements}\" must be an interface!");

            source.LocalSessionServiceTypeMappings[targetAlsoImplements] = sourceType;
            
            //Type[] additionalServiceTypes;
            //if (source.LocalSessionServiceTypeMappings.TryGetValue(sourceType, out additionalServiceTypes))
            //{
            //    List<Type> extendedTypeList = new List<Type>(additionalServiceTypes);
            //    if (!extendedTypeList.Contains(targetAlsoImplements))
            //    {
            //        extendedTypeList.Add(targetAlsoImplements);

            //        source.LocalSessionServiceTypeMappings[sourceType] = extendedTypeList.ToArray();
            //    }
            //}
            //else
            //{
            //    source.LocalSessionServiceTypeMappings[sourceType] = new Type[] { targetAlsoImplements };
            //}

            return this;
        }
    }
}
