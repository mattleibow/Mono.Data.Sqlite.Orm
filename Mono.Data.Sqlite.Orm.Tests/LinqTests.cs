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
            [AutoIncrement, PrimaryKey]
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

        [Test]
        public void GetWithExpressionTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});

            var product = db.Get<Product>(x => x.Name == "A");

            Assert.AreEqual(20, product.Price);
        }

        [Test]
        public void GetWithExpressionFailsTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});
            try
            {
                var product = db.Get<Product>(x => x.Name == "B");

                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                
            }
        }

        [Test]
        public void GetWithExpressionAsyncTest()
        {
            var db = OrmAsyncTestSession.GetConnection();

            db.CreateTableAsync<Product>().Wait();

            db.InsertAsync(new Product {Name = "A", Price = 20}).Wait();

            var task = db.GetAsync<Product>(x => x.Name == "A");
            task.Wait();
            
            Assert.AreEqual(20, task.Result.Price);
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

            var correct = db.Table<Product>().ElementAtOrDefault(1);
            Assert.AreEqual("B", correct.Name);

            var incorrect = db.Table<Product>().ElementAtOrDefault(2);
            Assert.IsNull(incorrect);
        }

        [Test]
        public void FirstTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});

            Assert.AreEqual("A", db.Table<Product>().First().Name);

            db.ClearTable<Product>();

            try
            {
                db.Table<Product>().First();

                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                
            }
        }

        [Test]
        public void FirstOrDefaultTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});

            Assert.AreEqual("A", db.Table<Product>().FirstOrDefault().Name);

            db.ClearTable<Product>();

            Assert.IsNull(db.Table<Product>().FirstOrDefault());
        }

        [Test]
        public void WithColumnTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});

            var product = db.Table<Product>().With(x => x.Id).With(x => x.Price).FirstOrDefault();

            Assert.AreEqual(1, product.Id);
            Assert.AreEqual(20, product.Price);
            Assert.IsNull(product.Name);
        }

        [Test]
        public void WithMultipleColumnTest()
        {
            OrmTestSession db = CreateDb();

            db.Insert(new Product {Name = "A", Price = 20});

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
    }
}