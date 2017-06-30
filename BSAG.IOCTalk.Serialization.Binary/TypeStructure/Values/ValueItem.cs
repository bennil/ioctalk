using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Values
{
    public static class ValueItem
    {

        public const byte NullValueIdent = 0;
        public const byte SingleObjectIdent = 1;
        public const byte CollectionObjectIdent = 2;
        public const byte SingleValueIdent = 3;
        public const byte TypeMetaInfo = 4;


        /// <summary>
        /// Creates the value item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <returns>IValueItem.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public static IValueItem CreateValueItem(PropertyInfo prop, ITypeResolver typeResolver)
        {
            var type = prop.PropertyType;
            var name = prop.Name;

            var getter = prop.GenerateGetterFunc();
            var setter = prop.GenerateSetterAction();

            return CreateValueItem(type, name, getter, setter, typeResolver);
        }

        public static IValueItem CreateValueItem(Type type, string name, Func<object, object> getter, Action<object, object> setter, ITypeResolver typeResolver)
        {
            if (type.Equals(typeof(string)))
            {
                return new StringItem(name, getter, setter);
            }
            else if (type.IsValueType)
            {
                var nullableType = Nullable.GetUnderlyingType(type);
                bool isNullableType = nullableType != null;
                if (isNullableType)
                {
                    type = nullableType;
                }

                return CreateValueTypeItem(type, name, getter, setter, isNullableType);
            }
            else if (type.IsArray
               || type.GetInterface("IEnumerable") != null)
            {
                return new CollectionItems(type, getter, setter, typeResolver);
            }
            else
            {
                return new ComplexStructure(type, getter, setter, typeResolver);
            }
        }

        private static IValueItem CreateValueTypeItem(Type type, string name, Func<object, object> getter, Action<object, object> setter, bool isNullable)
        {
            AbstractValueItem item;
            if (type.Equals(typeof(Int32)))
            {
                item = new Int32Item(name, getter, setter);
            }
            else if (type.Equals(typeof(bool)))
            {
                item = new BoolItem(name, getter, setter);
            }
            else if (type.Equals(typeof(double)))
            {
                item = new DoubleItem(name, getter, setter);
            }
            else if (type.Equals(typeof(decimal)))
            {
                item = new DecimalItem(name, getter, setter);
            }
            else if (type.IsEnum)
            {
                item = new EnumItem(name, getter, setter, type);
            }
            else if (type.Equals(typeof(Int64)))
            {
                item = new Int64Item(name, getter, setter);
            }
            else if (type.Equals(typeof(Int16)))
            {
                item = new Int16Item(name, getter, setter);
            }
            else if (type.Equals(typeof(DateTime)))
            {
                item = new DateTimeItem(name, getter, setter);
            }
            else if (type.Equals(typeof(TimeSpan)))
            {
                item = new TimeSpanItem(name, getter, setter);
            }
            else if (type.Equals(typeof(char)))
            {
                item = new CharItem(name, getter, setter);
            }
            else if (type.Equals(typeof(Guid)))
            {
                item = new GuidItem(name, getter, setter);
            }
            else
            {
                throw new NotSupportedException($"The type \"{type.FullName}\" is not supported for binary transmission!");
            }
            item.IsNullable = isNullable;
            return item;
        }

        public static Func<object, object> GenerateGetterFunc(this PropertyInfo pi)
        {
            var expParamPo = Expression.Parameter(typeof(object), "p");
            var expParamPc = Expression.Convert(expParamPo, pi.DeclaringType);

            var expMma = Expression.MakeMemberAccess(
                    expParamPc
                    , pi
                );

            var expMmac = Expression.Convert(expMma, typeof(object));

            var exp = Expression.Lambda<Func<object, object>>(expMmac, expParamPo);

            return exp.Compile();
        }

        public static Action<object, object> GenerateSetterAction(this PropertyInfo pi)
        {
            var expParamPo = Expression.Parameter(typeof(object), "p");
            var expParamPc = Expression.Convert(expParamPo, pi.DeclaringType);

            var expParamV = Expression.Parameter(typeof(object), "v");
            var expParamVc = Expression.Convert(expParamV, pi.PropertyType);

            var expMma = Expression.Call(
                    expParamPc
                    , pi.GetSetMethod()
                    , expParamVc
                );

            var exp = Expression.Lambda<Action<object, object>>(expMma, expParamPo, expParamV);

            return exp.Compile();
        }
    }
}
