namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VirtualAttribute : Attribute
    {
        public VirtualAttribute(string moduleName = CommonVirtualTableModules.Fts4)
        {
            this.ModuleName = moduleName;
        }

        public string ModuleName { get; set; }
    }

    public static class CommonVirtualTableModules
    {
        public const string Fts3 = "FTS3";
        public const string Fts4 = "FTS4";
    }
}