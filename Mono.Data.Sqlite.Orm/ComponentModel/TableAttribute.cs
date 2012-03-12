using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public TableAttribute()
        {
            OnPrimaryKeyConflict = ConflictResolution.Default;
        }

        public TableAttribute(string name)
        {
            Name = name;
            OnPrimaryKeyConflict = ConflictResolution.Default;
        }

        public string Name { get; set; }
        public ConflictResolution OnPrimaryKeyConflict { get; set; }
    }
}