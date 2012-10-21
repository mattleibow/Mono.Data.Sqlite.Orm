#if SILVERLIGHT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using NUnit.Framework;
#endif

namespace System
{
    internal static class ExceptionAssert
    {
        public static void Throws<TException>(Action blockToExecute) 
            where TException : Exception
        {
#if SILVERLIGHT
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
#elif NETFX_CORE
            Assert.ThrowsException<TException>(blockToExecute);
#else
            Assert.Catch(typeof(TException), () => blockToExecute());
#endif
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