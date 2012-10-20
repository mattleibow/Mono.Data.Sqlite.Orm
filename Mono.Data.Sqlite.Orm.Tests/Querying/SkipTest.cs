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
    public class SkipTest
    {
        public class TestObj
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public int Order { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Order={1}]", Id, Order);
            }
        }

        [Test]
        public void Skip()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();
            const int n = 100;

            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n) select new TestObj { Order = i };
            TestObj[] objs = cq.ToArray();

            int numIn = db.InsertAll(objs);
            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TableQuery<TestObj> q = from o in db.Table<TestObj>() orderby o.Order select o;

            TableQuery<TestObj> qs1 = q.Skip(1);
            List<TestObj> s1 = qs1.ToList();
            Assert.AreEqual(n - 1, s1.Count);
            Assert.AreEqual(2, s1[0].Order);

            TableQuery<TestObj> qs5 = q.Skip(5);
            List<TestObj> s5 = qs5.ToList();
            Assert.AreEqual(n - 5, s5.Count);
            Assert.AreEqual(6, s5[0].Order);
        }
    }
}