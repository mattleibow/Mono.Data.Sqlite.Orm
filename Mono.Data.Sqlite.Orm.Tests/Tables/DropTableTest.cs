using System;
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
    public class DropTableTest
    {
        public class Product
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal Price { get; set; }
        }

        [Test]
        public void CreateInsertDrop()
        {
            var db = new OrmTestSession();

            db.CreateTable<Product>();

            db.Insert(new Product { Name = "Hello", Price = 16, });

            int n = db.Table<Product>().Count();

            Assert.AreEqual(1, n);

            db.DropTable<Product>();

            ExceptionAssert.Throws<SqliteException>(() => db.Table<Product>().Count());
        }

        [Test]
        public void ClearTableTest()
        {
            // setup
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            // insert
            db.Insert(new Product { Name = "Hello", Price = 16, });
            db.Insert(new Product { Name = "Hello", Price = 16, });

            // confirm
            Assert.AreEqual(2, db.Table<Product>().Count());
            db.Get<Product>(1);

            // clear
            Assert.AreEqual(2, db.ClearTable<Product>());

            // confirm
            Assert.AreEqual(0, db.Table<Product>().Count());

            // insert
            db.Insert(new Product { Name = "Hello", Price = 16, });

            // confirm that the Ids have not reset
            Assert.AreEqual(1, db.Table<Product>().Count());
            db.Get<Product>(3);
        }
    }
}