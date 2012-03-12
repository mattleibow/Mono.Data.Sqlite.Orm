using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mono.Data.Sqlite.Orm
{
    public static class Extensions
    {
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member)
            where T : Attribute
        {
            return member.GetCustomAttributes(typeof (T), true).Cast<T>();
        }
    }
}