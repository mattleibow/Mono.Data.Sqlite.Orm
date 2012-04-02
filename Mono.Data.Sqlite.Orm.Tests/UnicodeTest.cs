using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

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

            db.Insert(new Product
                          {
                              Name = TestString,
                          });

            var p = db.Get<Product>(1);

            Assert.AreEqual(TestString, p.Name);
        }

        [Test]
        public void Query()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product
                          {
                              Name = TestString,
                          });

            var ps = (from p in db.Table<Product>()
                      where p.Name == TestString
                      select p).ToList();

            Assert.AreEqual(1, ps.Count);
            Assert.AreEqual(TestString, ps[0].Name);
        }
    }
}