using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class ContainsTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Name={1}]", Id, Name);
            }
        }

        [Test]
        public void Contains()
		{
			var db = new OrmTestSession();
			db.CreateTable<TestObj>();
			
			const int n = 20;
			IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj
                                                 {
                                                     Name = i.ToString(CultureInfo.InvariantCulture)
                                                 };

			db.InsertAll(cq);

			var tensq = new[] {"0", "10", "20"};
			List<TestObj> tens = (from o in db.Table<TestObj>() where tensq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, tens.Count);

			var moreq = new[] {"0", "x", "99", "10", "20", "234324"};
			List<TestObj> more = (from o in db.Table<TestObj>() where moreq.Contains(o.Name) select o).ToList();
			Assert.AreEqual(2, more.Count);
		}
    }
}