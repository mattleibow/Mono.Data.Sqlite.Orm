using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EnumAffinityAttribute : Attribute
    {
        public EnumAffinityAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Type = type;
        }

        public Type Type { get; private set; }
    }
}