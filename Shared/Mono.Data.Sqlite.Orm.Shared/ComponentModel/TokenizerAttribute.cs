namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    using System;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TokenizerAttribute : Attribute
    {
        public TokenizerAttribute(string value = CommonVirtualTableTokenizers.Simple, params string[] parameters)
        {
            this.Parameters = parameters;
            this.Value = value;
        }

        public string Value { get; set; }
        public string[] Parameters { get; private set; }

        public string FullValue
        {
            get
            {
                var vals = new [] { this.Value }.Concat(this.Parameters).ToArray();
                return string.Join(" ", vals);
            }
        }
    }

    public static class CommonVirtualTableTokenizers
    {
        public const string Simple = "simple";
        public const string Porter = "porter";
        public const string ICU = "icu";
        public const string Unicode61 = "unicode61";
    }
}
