using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UniqueAttribute : Attribute
    {
        public UniqueAttribute()
        {
            OnConflict = ConflictResolution.Default;
        }

        public ConflictResolution OnConflict { get; set; }
    }
}