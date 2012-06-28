using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

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
