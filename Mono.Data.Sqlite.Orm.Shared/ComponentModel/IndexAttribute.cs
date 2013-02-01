using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class IndexAttribute : Attribute
    {
        public IndexAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name", "All indexes need a name.");
            }

            Name = name;
            Unique = false;
        }

        public string Name { get; private set; }
        public bool Unique { get; set; }
    }
}