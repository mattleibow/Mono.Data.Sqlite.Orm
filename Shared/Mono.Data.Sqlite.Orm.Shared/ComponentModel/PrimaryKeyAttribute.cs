using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute()
        {
            Direction = Direction.Default;
        }

        public string Name { get; set; }
        public int Order { get; set; }
        public Direction Direction { get; set; }
    }
}