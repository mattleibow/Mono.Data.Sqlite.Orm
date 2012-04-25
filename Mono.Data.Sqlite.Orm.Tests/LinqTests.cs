using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class LinqTest
    {
        public class Product
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        private OrmTestSession CreateDb()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();
            return db;
        }

        [Test]
        public void ContainsConstantData()
        {
            int n = 20;
            IEnumerable<Product> cq = from i in Enumerable.Range(1, n)
                                      select new Product
                                                 {
                                                     Name = i.ToString()
                                                 };

            OrmTestSession db = CreateDb();

            db.InsertAll(cq);

            var tensq = new[] {"0", "10", "20"};
            List<Product> tens = (from o in db.Table<Product>() where tensq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, tens.Count);

            var moreq = new[] {"0", "x", "99", "10", "20", "234324"};
            List<Product> more = (from o in db.Table<Product>() where moreq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more.Count);
        }

        [Test]
        public void ContainsQueriedData()
        {
            int n = 20;
            IEnumerable<Product> cq = from i in Enumerable.Range(1, n)
                                      select new Product
                                                 {
                                                     Name = i.ToString()
                                                 };

            OrmTestSession db = CreateDb();

            db.InsertAll(cq);

            var tensq = new[] {"0", "10", "20"};
            List<Product> tens = (from o in db.Table<Product>() where tensq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, tens.Count);

            var moreq = new[] {"0", "x", "99", "10", "20", "234324"};
            List<Product> more = (from o in db.Table<Product>() where moreq.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more.Count);

            List<string> moreq2 = moreq.ToList();
            List<Product> more2 = (from o in db.Table<Product>() where moreq2.Contains(o.Name) select o).ToList();
            Assert.AreEqual(2, more2.Count);
        }

        [Test]
        public void FunctionParameter()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product
                          {
                              Name = "A",
                              Price = 20,
                          });

            db.Insert(new Product
                          {
                              Name = "B",
                              Price = 10,
                          });

            Func<decimal, List<Product>> getProductsWithPriceAtLeast = val => (from p in db.Table<Product>()
                                                                               where p.Price > val
                                                                               select p).ToList();

            List<Product> r = getProductsWithPriceAtLeast(15);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }

        [Test]
        public void WhereGreaterThan()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product
                          {
                              Name = "A",
                              Price = 20,
                          });

            db.Insert(new Product
                          {
                              Name = "B",
                              Price = 10,
                          });

            Assert.AreEqual(2, db.Table<Product>().Count());

            List<Product> r = (from p in db.Table<Product>()
                               where p.Price > 15
                               select p).ToList();
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }
    }
}