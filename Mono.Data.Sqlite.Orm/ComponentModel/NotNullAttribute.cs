using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NotNullAttribute : Attribute
    {
        public NotNullAttribute()
        {
            OnConflict = ConflictResolution.Default;
        }

        public ConflictResolution OnConflict { get; set; }
    }
}