using System;
using System.Collections.Generic;
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
    public class LinqTest
    {
        public class Product
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal Price { get; set; }
        }

        public class CoolTable
        {
            public string Name { get; set; }

            public decimal Price { get; set; }
        }

        public class BitwiseTable
        {
            public int Flags { get; set; }
        }

        [Test]
        public void FunctionParameter()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20, });
            db.Insert(new Product { Name = "B", Price = 10, });

            Func<decimal, List<Product>> getProductsWithPriceAtLeast =
                val => (from p in db.Table<Product>()
                        where p.Price > val 
                        select p).ToList();

            List<Product> r = getProductsWithPriceAtLeast(15);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }

        [Test]
        public void WhereGreaterThan()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20, });
            db.Insert(new Product { Name = "B", Price = 10, });

            Assert.AreEqual(2, db.Table<Product>().Count());

            List<Product> r = (from p in db.Table<Product>() 
                               where p.Price > 15 
                               select p).ToList();
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual("A", r[0].Name);
        }

        [Test]
        public void OrderByCast()
        {
            var db = new OrmTestSession();
            db.CreateTable<CoolTable>();

            db.Insert(new CoolTable {Name = "A", Price = 20});
            db.Insert(new CoolTable {Name = "B", Price = 100});

            var nocast = (from p in db.Table<CoolTable>()
                          orderby p.Price descending
                          select p).ToList();
            Assert.AreEqual(2, nocast.Count);
            Assert.AreEqual("B", nocast[0].Name);

            var cast = (from p in db.Table<CoolTable>()
                        orderby (int) p.Price descending
                        select p).ToList();
            Assert.AreEqual(2, cast.Count);
            Assert.AreEqual("B", cast[0].Name);
        }

        [Test]
        public void GetWithExpressionTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            var product = db.Get<Product>(x => x.Name == "A");

            Assert.AreEqual(20, product.Price);
        }

        [Test]
        public void GetWithExpressionFailsTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            ExceptionAssert.Throws<InvalidOperationException>(() => db.Get<Product>(x => x.Name == "B"));
        }

        [Test]
        public void DistinctTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<CoolTable>();

            db.Insert(new CoolTable { Name = "A", Price = 20 });
            db.Insert(new CoolTable { Name = "A", Price = 20 });
            db.Insert(new CoolTable { Name = "A", Price = 20 });

            Assert.AreEqual(3, db.Table<CoolTable>().ToArray().Length);
            Assert.AreEqual(1, db.Table<CoolTable>().Distinct().ToArray().Length);

            Assert.AreEqual(3, db.Table<CoolTable>().Count());
            Assert.AreEqual(1, db.Table<CoolTable>().Distinct().Count());
        }

        [Test]
        public void TestElementAtOrDefault()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20, });
            db.Insert(new Product { Name = "B", Price = 10, });

            Assert.AreEqual(2, db.Table<Product>().Count());

            var correct = db.Table<Product>().ElementAtOrDefault(1);
            Assert.AreEqual("B", correct.Name);

            var incorrect = db.Table<Product>().ElementAtOrDefault(2);
            Assert.IsNull(incorrect);
        }

        [Test]
        public void FirstTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            Assert.AreEqual("A", db.Table<Product>().First().Name);

            db.ClearTable<Product>();

            ExceptionAssert.Throws<InvalidOperationException>(() => db.Table<Product>().First());
        }

        [Test]
        public void FirstOrDefaultTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            Assert.AreEqual("A", db.Table<Product>().FirstOrDefault().Name);

            db.ClearTable<Product>();

            Assert.IsNull(db.Table<Product>().FirstOrDefault());
        }

        [Test]
        public void WithColumnTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            var product = db.Table<Product>().With(x => x.Id).With(x => x.Price).FirstOrDefault();

            Assert.AreEqual(1, product.Id);
            Assert.AreEqual(20, product.Price);
            Assert.IsNull(product.Name);
        }

        [Test]
        public void WithMultipleColumnTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();

            db.Insert(new Product { Name = "A", Price = 20 });

            var product = db.Table<Product>().With(x => x.Id, x => x.Price).FirstOrDefault();

            Assert.AreEqual(1, product.Id);
            Assert.AreEqual(20, product.Price);
            Assert.IsNull(product.Name);
        }

        [Test]
        public void BitwiseAndTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<BitwiseTable>();

                db.Insert(new BitwiseTable { Flags = 0 });
                db.Insert(new BitwiseTable { Flags = 1 });
                db.Insert(new BitwiseTable { Flags = 2 });
                db.Insert(new BitwiseTable { Flags = 2 | 1 });

                var bit = db.Table<BitwiseTable>().Where(x => (x.Flags & 2) != 0).Select(x => x.Flags).ToArray();
                ArrayAssert.AreEqual(new[] { 2, 3 }, bit);

                bit = db.Table<BitwiseTable>().Where(x => (x.Flags & 1) != 0).Select(x => x.Flags).ToArray();
                ArrayAssert.AreEqual(new[] { 1, 3 }, bit);

                bit = db.Table<BitwiseTable>().Where(x => (x.Flags & 4) != 0).Select(x => x.Flags).ToArray();
                ArrayAssert.AreEqual(new int[] { }, bit);
            }
        }

        [Test]
        public void BitwiseOrTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<BitwiseTable>();

                db.Insert(new BitwiseTable { Flags = 0 });
                db.Insert(new BitwiseTable { Flags = 1 });
                db.Insert(new BitwiseTable { Flags = 2 });
                db.Insert(new BitwiseTable { Flags = 2 | 1 });

                var bit = db.Table<BitwiseTable>().Where(x => (x.Flags | 1) == 3).Select(x => x.Flags).ToArray();
                ArrayAssert.AreEqual(new[] { 2, 3 }, bit);
            }
        }

        public abstract class TestObjBase<T>
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }
            public T Data { get; set; }
            public DateTime Date { get; set; }
        }
        
        public class TestObjString : TestObjBase<string>
        {
            
        }

        [Test]
        public void CanCompareAnyField()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObjString>();
            db.Insert(new TestObjString
                {
                    Data = Convert.ToString(3),
                    Date = new DateTime(2013, 1, 3)
                });

            var results = db.Table<TestObjString>().Where(o => o.Data.Equals("3"));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("3", results.FirstOrDefault().Data);

            results = db.Table<TestObjString>().Where(o => o.Id.Equals(1));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("3", results.FirstOrDefault().Data);

            var date = new DateTime(2013, 1, 3);
            results = db.Table<TestObjString>().Where(o => o.Date.Equals(date));
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("3", results.FirstOrDefault().Data);
        }
    }
}
