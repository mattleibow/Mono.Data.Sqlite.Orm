using System.Linq;

using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT || MS_TEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestFixtureAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
#elif NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;

#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class InheritanceTest
    {
        public class Base
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string BaseProp { get; set; }
        }

        public class Derived : Base
        {
            public string DerivedProp { get; set; }
        }

        [Test]
        public void InheritanceWorks()
        {
            var db = new OrmTestSession();

            var mapping = db.GetMapping<Derived>();

            Assert.AreEqual(3, mapping.Columns.Count);
            Assert.AreEqual("Id", mapping.PrimaryKey.Columns.First().Name);
        }
    }
}