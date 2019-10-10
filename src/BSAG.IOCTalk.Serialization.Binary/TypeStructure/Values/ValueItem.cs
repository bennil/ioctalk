using BSAG.IOCTalk.Common.Reflection;
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
        public const byte HashCodeString = 5;


        /// <summary>
        /// Creates the value item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <returns>IValueItem.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public static IValueItem CreateValueItem(PropertyInfo prop, ISerializeContext ctx)
        {
            var type = prop.PropertyType;
            var name = prop.Name;

            var getter = prop.GenerateGetterFunc();
            var setter = prop.GenerateSetterAction();

            return CreateValueItem(prop.DeclaringType, type, name, getter, setter, ctx);
        }

        public static IValueItem CreateValueItem(Type declaringType, Type type, string name, Func<object, object> getter, Action<object, object> setter, ISerializeContext ctx)
        {
            if (type.Equals(typeof(string)))
            {
                if (declaringType != null
                    && ctx.Serializer.IsStringHashProperty(declaringType, name))
                {
                    return new StringHashItem(name, getter, setter);
                }
                else
                {
                    return new StringItem(name, getter, setter);
                }
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
                return new CollectionItems(type, getter, setter, ctx);
            }
            else if (type.Equals(typeof(Type)))
            {
                return new TypeRefItem(name, getter, setter);
            }
            else
            {
                return new ComplexStructure(type, getter, setter, ctx);
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

        public static Type GetRuntimeType(IValueItem valueItem)
        {
            if (valueItem is IObjectType)
            {
                return ((IObjectType)valueItem).RuntimeType;
            }
            else
            {
                switch (valueItem.Type)
                {
                    case ItemType.Int32:
                        return typeof(Int32);

                    case ItemType.Bool:
                        return typeof(bool);

                    case ItemType.Double:
                        return typeof(double);

                    case ItemType.Decimal:
                        return typeof(decimal);

                    case ItemType.Enum:
                        return typeof(Enum);

                    case ItemType.Int16:
                        return typeof(Int16);                      

                    case ItemType.Int64:
                        return typeof(Int64);

                    case ItemType.DateTime:
                        return typeof(DateTime);

                    case ItemType.TimeSpan:
                        return typeof(TimeSpan);

                    case ItemType.Char:
                        return typeof(char);

                    case ItemType.Guid:
                        return typeof(Guid);                        

                }

                return null;
            }
        }

    }
}
