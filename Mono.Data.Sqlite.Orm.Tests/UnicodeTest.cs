using System.Linq;

using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT 
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
    public class UnicodeTest
    {
        private const string TestString = "\u2329\u221E\u232A";

        public class Product
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal Price { get; set; }
        }

        [Test]
        public void Insert()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = TestString, });

            var p = db.Get<Product>(1);

            Assert.AreEqual(TestString, p.Name);
        }

        [Test]
        public void Query()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = TestString, });

            var ps = (from p in db.Table<Product>() where p.Name == TestString select p).ToList();

            Assert.AreEqual(1, ps.Count);
            Assert.AreEqual(TestString, ps[0].Name);
        }
    }
}