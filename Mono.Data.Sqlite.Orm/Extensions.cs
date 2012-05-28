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

        public static IEnumerable<Type> GetImplementedInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces;
        }

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors;
        }
#else
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
        
        public static IEnumerable<Type> GetImplementedInterfaces(this Type type)
        {
            return type.GetInterfaces();
        }

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            return type.GetConstructors();
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