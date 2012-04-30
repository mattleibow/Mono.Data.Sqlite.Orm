using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if NETFX_CORE
using System.Reflection.RuntimeExtensions;
#endif

namespace Mono.Data.Sqlite.Orm
{
    public static class Extensions
    {
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member)
            where T : Attribute
        {
            return member.GetCustomAttributes(typeof (T), true).Cast<T>();
        }

#if NETFX_CORE
        public static IEnumerable<PropertyInfo> GetMappableProperties(this Type type)
        {
            return type.GetRuntimeProperties();
        }
#else
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static IEnumerable<PropertyInfo> GetMappableProperties(this Type type)
        {
            const BindingFlags flags = BindingFlags.Public |
                                       BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.SetProperty;
            return type.GetProperties(flags);
        }
#endif
    }
}