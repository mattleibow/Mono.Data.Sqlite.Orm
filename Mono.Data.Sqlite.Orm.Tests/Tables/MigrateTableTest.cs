using System;
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
    public class MigrateTableTest
    {
        public class CreateTest
        {
            public CreateTest(OrmTestSession db)
            {
                db.CreateTable<OrderLine>();

                TableMapping orderLine = db.GetMapping<OrderLine>();
                Assert.AreEqual(4, orderLine.Columns.Count, "OrderLine has 4 columns");

                var l = new OrderLine { Status = OrderLineStatus.Shipped };
                db.Insert(l);

                OrderLine lo = db.Table<OrderLine>().First(x => x.Status == OrderLineStatus.Shipped);
                Assert.AreEqual(lo.Id, l.Id);

                Id = lo.Id;
            }

            public int Id { get; set; }

            #region Nested type: OrderLine

            public class OrderLine
            {
                [AutoIncrement]
                [PrimaryKey]
                public int Id { get; set; }

                public int Quantity { get; set; }

                public decimal UnitPrice { get; set; }

                public OrderLineStatus Status { get; set; }
            }

            #endregion
        }

        public class MigrateTest
        {
            public MigrateTest(OrmTestSession db)
            {
                db.CreateTable<OrderDetails>();

                TableMapping orderLine = db.GetMapping<OrderDetails>();
                Assert.AreEqual(6, orderLine.Columns.Count, "OrderDetails (OrderLine) has 6 columns");

                OrderDetails l = db.Table<OrderDetails>().First(x => x.ShippingStatus == OrderLineStatus.Shipped);
                l.Tax = 12;
                db.Update(l);

                OrderDetails lo = db.Table<OrderDetails>().First(x => x.Tax == 12);
                Assert.AreEqual(lo.Id, l.Id);

                Id = lo.Id;
            }

            public int Id { get; set; }

            #region Nested type: OrderDetails

            [Table("OrderLine")]
            public class OrderDetails
            {
                [AutoIncrement]
                [PrimaryKey]
                public int Id { get; set; }

                public int Quantity { get; set; }

                public decimal UnitPrice { get; set; }

                // a new column
                public decimal? Tax { get; set; }

                // a new column
                [Default("10")]
                public decimal DefaultValue { get; set; }

                // a renamed column
                [Column("Status")]
                public OrderLineStatus ShippingStatus { get; set; }
            }

            #endregion
        }

        public class MigrateFailTest
        {
            public MigrateFailTest(OrmTestSession db)
            {
                ExceptionAssert.Throws<InvalidOperationException>(() => db.CreateTable<OrderLine>());
            }

            public int Id { get; set; }

            #region Nested type: OrderDetails

            public class OrderLine
            {
                [AutoIncrement]
                [PrimaryKey]
                public int Id { get; set; }

                public int Quantity { get; set; }

                public decimal UnitPrice { get; set; }

                // a new column
                public decimal? Tax { get; set; }

                // a new column
                [Default("10")]
                public decimal DefaultValue { get; set; }

                // a renamed column
                [Column("Status")]
                public OrderLineStatus ShippingStatus { get; set; }

                public int NonNullableColumn { get; set; }
            }

            #endregion
        }

        public class RenameTableTest
        {
            public RenameTableTest(OrmTestSession db)
            {
                db.CreateTable<RenamedTable>();
            }

            public int Id { get; set; }

            #region Nested type: OrderDetails

            [RenameTable("OrderLine")]
            public class RenamedTable
            {
                [AutoIncrement]
                [PrimaryKey]
                public int Id { get; set; }

                public int Quantity { get; set; }

                public decimal UnitPrice { get; set; }

                // a new column
                public decimal? Tax { get; set; }

                // a new column
                [Default("10")]
                public decimal DefaultValue { get; set; }

                // a renamed column
                [Column("Status")]
                public OrderLineStatus ShippingStatus { get; set; }
            }

            #endregion
        }

        public enum OrderLineStatus
        {
            Placed = 1,

            Shipped = 100
        }

        [Test]
        public void CreateAndMigrateThem()
        {
            var db = new OrmTestSession();

            var create = new CreateTest(db);
            var migrate = new MigrateTest(db);
            var fail = new MigrateFailTest(db);
            var rename = new RenameTableTest(db);

            Assert.AreEqual(create.Id, migrate.Id);
        }
    }
}