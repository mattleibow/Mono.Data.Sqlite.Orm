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
    public class CollateTest
    {
        public class TestObj
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string CollateDefault { get; set; }

            [Collation(Collation.Binary)]
            public string CollateBinary { get; set; }

            [Collation(Collation.RTrim)]
            public string CollateRTrim { get; set; }

            [Collation(Collation.NoCase)]
            public string CollateNoCase { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}]", Id);
            }
        }

        [Test]
        public void Collate()
        {
            var obj = new TestObj
                          {
                              CollateDefault = "Alpha ",
                              CollateBinary = "Alpha ",
                              CollateRTrim = "Alpha ",
                              CollateNoCase = "Alpha ",
                          };

            var db = new OrmTestSession();
            db.CreateTable<TestObj>();
            db.Insert(obj);

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateDefault == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateBinary == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateRTrim == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateRTrim == "ALPHA" select o).Count());

            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha " select o).Count());
            Assert.AreEqual(1, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA " select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "Alpha" select o).Count());
            Assert.AreEqual(0, (from o in db.Table<TestObj>() where o.CollateNoCase == "ALPHA" select o).Count());
        }
    }
}