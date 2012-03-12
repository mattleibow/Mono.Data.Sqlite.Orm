using System.Collections.Generic;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class InterfaceTests
    {
        public interface IProductInterface
        {
            [AutoIncrement, PrimaryKey]
            int Id { get; }

            string Name { get; }
            decimal? Price { get; }

            [ComponentModel.Ignore]
            List<int> Blah { get; }
        }

        public class Product : IProductInterface
        {
            #region IProductInterface Members

            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
            public decimal? Price { get; set; }

            [ComponentModel.Ignore]
            public List<int> Blah { get; set; }

            #endregion
        }

        [Test]
        public void InterfaceTest()
        {
            var db = new OrmTestSession();

            db.CreateTable<Product>();

            var obj1 = new Product {Name = "Some Cool Name"};

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);

            db.Close();
        }
    }
}