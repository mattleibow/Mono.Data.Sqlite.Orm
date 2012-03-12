using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RenameTableAttribute : Attribute
    {
        public RenameTableAttribute(string oldName)
        {
            if (string.IsNullOrEmpty(oldName))
            {
                throw new ArgumentNullException("oldName", "You cannot rename a table from an empty string.");
            }

            OldName = oldName;
        }

        public string OldName { get; private set; }
    }
}