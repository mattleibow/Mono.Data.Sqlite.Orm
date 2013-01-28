using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    /// <summary>
    /// The CHECK constraint is used to limit the value range that can be 
    /// placed in a column.
    /// </summary>
    /// <remarks>
    /// A CHECK constraint may be attached to a column definition or specified 
    /// as a table constraint. In practice it makes no difference. 
    /// Each time a new row is inserted into the table or an existing row is
    /// updated, the expression associated with each CHECK constraint is 
    /// evaluated and cast to a NUMERIC value in the same way as a CAST 
    /// expression. 
    /// If the result is zero (integer value 0 or real value 0.0), then a 
    /// constraint violation has occurred. 
    /// If the CHECK expression evaluates to NULL, or any other non-zero value,
    /// it is not a constraint violation. 
    /// The expression of a CHECK constraint may not contain a subquery. 
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CheckAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckAttribute"/> class.
        /// </summary>
        /// <param name="expression">
        /// The expression to use when doing the check.
        /// </param>
        public CheckAttribute(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentNullException("expression", "All checks must have a non-empty expression.");
            }

            Expression = expression;
        }

        /// <summary>
        /// Gets the check expression.
        /// </summary>
        public string Expression { get; private set; }
    }
}