#if SILVERLIGHT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace System
{
    internal static class ExceptionAssert
    {
        public static void Throws<TException>(Action blockToExecute) where TException : Exception
        {
            Type expectedType = typeof (TException);

            try
            {
                blockToExecute();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, expectedType, string.Format(
                    "Expected exception of type {0} but type of {1} was thrown instead.",
                    expectedType, ex.GetType()));

                return;
            }

            Assert.Fail(string.Format("Expected exception of type {0} but no exception was thrown.", expectedType));
        }
    }

#if SILVERLIGHT
    internal class TestCategoryAttribute : Attribute
    {
        public string Name { get; set; }

        public TestCategoryAttribute(string name)
        {
            Name = name;
        }
    }

    public static class TestAssembly
    {
    }
#endif
}