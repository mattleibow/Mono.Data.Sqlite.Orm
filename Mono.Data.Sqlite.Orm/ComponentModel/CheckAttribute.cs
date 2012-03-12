using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CheckAttribute : Attribute
    {
        public CheckAttribute(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException("expression", "All checks must have a non-empty expression.");
            }

            Expression = expression;
        }

        public string Expression { get; private set; }
    }
}