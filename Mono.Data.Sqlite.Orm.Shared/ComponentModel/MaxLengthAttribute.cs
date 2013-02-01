using System;

namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MaxLengthAttribute : Attribute
    {
        public MaxLengthAttribute(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", "All strings must have a maximum length of at least one character.");
            }

            Length = length;
        }

        public int Length { get; private set; }
    }
}