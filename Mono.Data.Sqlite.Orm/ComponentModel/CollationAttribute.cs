using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CollationAttribute : Attribute
    {
        public CollationAttribute(Collation collation)
        {
            Collation = collation;
        }

        public Collation Collation { get; private set; }
    }
}