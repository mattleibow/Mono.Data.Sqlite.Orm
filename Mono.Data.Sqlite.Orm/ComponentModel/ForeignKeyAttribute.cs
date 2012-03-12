using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        public ForeignKeyAttribute(Type childTable, string childKey)
        {
            if (childTable == null)
            {
                throw new ArgumentNullException("childTable", "All foreign keys must reference a table object.");
            }

            if (string.IsNullOrEmpty(childKey))
            {
                throw new ArgumentNullException("childKey", "All foreign keys must reference at least one column in the child table object.");
            }

            ChildTable = childTable;
            ChildKey = childKey;

            OnDeleteAction = ForeignKeyAction.Default;
            OnUpdateAction = ForeignKeyAction.Default;
            NullMatch = NullMatch.Default;
            Deferred = Deferred.Default;
        }

        public string Name { get; set; }
        public Type ChildTable { get; private set; }
        public string ChildKey { get; private set; }
        public int Order { get; set; }
        public ForeignKeyAction OnDeleteAction { get; set; }
        public ForeignKeyAction OnUpdateAction { get; set; }
        public NullMatch NullMatch { get; set; }
        public Deferred Deferred { get; set; }
    }
}