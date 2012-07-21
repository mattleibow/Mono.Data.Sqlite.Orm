#if SILVERLIGHT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace System
{
    internal static class ArrayAssert
    {
        public static void AreEqual<T>(T[] expected, T[] actual)
        {
            if ((expected == null && actual != null) ||
                (expected != null && actual == null))
            {
                Assert.Fail("expected {0}, but was {1}", expected, actual);
            }

            Assert.AreEqual(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}