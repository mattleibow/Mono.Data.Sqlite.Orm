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
    public class ConnectionTrackingTest
    {
        public class Product : ITrackConnection
        {
            private OrderLine[] _orderLines;

            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public decimal Price { get; set; }

            public OrderLine[] OrderLines
            {
                get
                {
                    return _orderLines
                           ?? (_orderLines = Connection.Table<OrderLine>().Where(o => o.ProductId == Id).ToArray());
                }
            }

            #region ITrackConnection Members

            [ComponentModel.Ignore]
            public SqliteSessionBase Connection { get; set; }

            #endregion
        }

        public class OrderLine : ITrackConnection
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public int ProductId { get; set; }

            public int Quantity { get; set; }

            public decimal UnitPrice { get; set; }

            #region ITrackConnection Members

            [ComponentModel.Ignore]
            public SqliteSessionBase Connection { get; set; }

            #endregion
        }

        [Test]
        public void CreateThem()
        {
            var db = new OrmTestSession();
            db.CreateTable<Product>();
            db.CreateTable<OrderLine>();

            var foo = new Product { Name = "Foo", Price = 10.0m };
            var bar = new Product { Name = "Bar", Price = 0.10m };
            db.Insert(foo);
            db.Insert(bar);
            db.Insert(new OrderLine { ProductId = foo.Id, Quantity = 6, UnitPrice = 10.01m });
            db.Insert(new OrderLine { ProductId = foo.Id, Quantity = 3, UnitPrice = 0.02m });
            db.Insert(new OrderLine { ProductId = bar.Id, Quantity = 9, UnitPrice = 100.01m });

            OrderLine[] lines = foo.OrderLines;

            Assert.AreEqual(lines.Length, 2, "Has 2 order lines");
            Assert.AreEqual(foo.Connection, db, "foo.Connection was set");
            Assert.AreEqual(lines[0].Connection, db, "lines[0].Connection was set");
        }
    }
}