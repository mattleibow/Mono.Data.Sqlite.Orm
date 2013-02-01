using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class IndexedAttribute : Attribute
    {
        public IndexedAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name", "All indexes need a name.");
            }

            Name = name;
            Collation = Collation.Default;
            Direction = Direction.Default;
        }

        public int Order { get; set; }
        public string Name { get; private set; }
        public Collation Collation { get; set; }
        public Direction Direction { get; set; }
    }
}