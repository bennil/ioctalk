using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using BSAG.IOCTalk.Test.Common;
using BSAG.IOCTalk.Test.Common.Service.Implementation;
using BSAG.IOCTalk.Test.TestObjects;
using BSAG.IOCTalk.Test.TestObjects.NoProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    public class UnknowTestTypeResolver : IUnknowContextTypeResolver
    {

        public UnknowTestTypeResolver()
        {
        }

        public Type DetermineTargetType(Type interfaceType, ISerializeContext context)
        {
            if (interfaceType.Equals(typeof(ITestInterfaceBase)))
            {
                return typeof(TestInterfaceImpl1);
            }
            else if (interfaceType.Equals(typeof(IDeepTestInterface1)))
            {
                return typeof(TestInterfaceImpl1);
            }
            else if (interfaceType.Equals(typeof(ITestInterfaceWithoutSetProperties)))
            {
                return typeof(TestImplementationWithoutSetProperties);
            }
            else if (interfaceType.Equals(typeof(IPerformanceData)))
            {
                return typeof(PerformanceData);
            }
            //else if (interfaceType.Equals(typeof(IGenericMessage)))
            //{
            //    return typeof(GenericMessage);
            //}

            return null;
        }

        public IValueItem DetermineSpecialInterfaceType(Type objectType, Type defaultInterfaceType, ISerializeContext ctx)
        {
            return null;
        }

        //public Type DetermineInterfaceType(Type type)
        //{
        //    if (type.Equals(typeof(GenericMessage)))
        //    {
        //        return typeof(IGenericMessage);
        //    }

        //    return null;
        //}


        //private Type UnknownTypeResolver(SerializationContext context)
        //{
        //    if (context.InterfaceType != null)
        //    {
        //        if (context.InterfaceType.Equals(typeof(ITestInterfaceBase)))
        //        {
        //            return typeof(TestInterfaceImpl1);
        //        }
        //        else if (context.InterfaceType.Equals(typeof(ITestInterfaceWithoutSetProperties)))
        //        {
        //            return typeof(TestImplementationWithoutSetProperties);
        //        }
        //    }
        //    else if (context.Key == "BaseObject")
        //    {
        //        return typeof(TestInterfaceImpl1);
        //    }
        //    else if (context.Key == "PerfData")
        //    {
        //        return typeof(PerformanceData);
        //    }
        //    else if (context.Key == "ObjectArray"
        //        && context.ArrayIndex.HasValue)
        //    {
        //        switch (context.ArrayIndex.Value)
        //        {
        //            case 3:
        //                return typeof(TimeSpan);

        //            case 5:
        //                return typeof(SubObject);
        //        }
        //    }

        //    return null;
        //}

        //private Type SpecialTypeResolver(Type sourceType)
        //{
        //    // check expose sub type attribute
        //    var exposureAttributes = sourceType.GetCustomAttributes(typeof(ExposeSubTypeAttribute), false);
        //    if (exposureAttributes.Length > 0)
        //    {
        //        return ((ExposeSubTypeAttribute)exposureAttributes[0]).Type;
        //    }

        //    return null;
        //}
    }
}
