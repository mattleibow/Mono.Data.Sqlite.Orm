using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

#if WINDOWS_PHONE || SILVERLIGHT
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class MultiplePKs
    {
        public partial class TestObj
        {
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, SubId={1}, Text={2}]", Id, SubId, Text);
            }
        }

        public partial class TestObj
        {
            [PrimaryKey]
            public int SubId { get; set; }
        }

        [Test]
        public void MultiplePkOperations()
		{
			var db = new OrmTestSession();
			db.CreateTable<TestObj>();
			
			// insert
			const int n = 10 , m= 5;
			var objs = new TestObj[n * m];
			for (int j = 0; j != n; ++j)
			{
				for (int i = 0; i != m; ++i)
				{
					objs[j * m + i] = new TestObj {Id = j, SubId = i, Text = "I am (" + j + "," + i + ")"};
				}
			}

			int numIn = db.InsertAll(objs);

			Assert.AreEqual(numIn, n * m, "Num inserted must = num objects");

			var obj = db.Get<TestObj>(5, 3);
			Assert.AreEqual(5, obj.Id);
			Assert.AreEqual(3, obj.SubId);
			Assert.AreEqual("I am (5,3)", obj.Text);
			
			try
			{
				db.Insert(obj);
			}
			catch (SqliteException ex)
			{
                Assert.AreEqual(SqliteErrorCode.Constraint, (SqliteErrorCode)ex.ErrorCode);
			}

			// update
			obj.Text = "I've been changed";
			db.Update(obj);
			db.Update<TestObj>("Text", "I've been changed also", 8, 2);

			obj = db.Get<TestObj>(5, 3);
			Assert.AreEqual("I've been changed", obj.Text);

			obj = db.Get<TestObj>(8, 2);
			Assert.AreEqual("I've been changed also", obj.Text);

			db.UpdateAll<TestObj>("Text", "All changed");
			IEnumerable<TestObj> q1 = from o in db.Table<TestObj>() select o;
			foreach (TestObj o in q1)
				Assert.AreEqual("All changed", o.Text);

			TestObj[] q2 = (from o in db.Table<TestObj>() where o.SubId == 3 select o).ToArray();
			Assert.AreEqual(10, q2.Length);
			for (int i = 0; i != 10; ++i)
			{
				Assert.AreEqual(i, q2[i].Id);
			}

            object numCount = db.Table<TestObj>().Count();
			Assert.AreEqual(numCount, objs.Length, "Num counted must = num objects");

			// delete
			obj = db.Get<TestObj>(8, 2);
			db.Delete(obj);
			
			try
			{
				var item = db.Get<TestObj>(8, 2);

				Assert.Fail();
			}
			catch (InvalidOperationException)
			{
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}

            db.Execute("delete from TestObj where SubId=2");
            numCount = db.ExecuteScalar<int>("select count(*) from TestObj");
			Assert.AreEqual(numCount, objs.Length - 10);
			foreach (TestObj o in (from o in db.Table<TestObj>() select o))
				Assert.AreNotEqual(2, o.SubId);
		}
    }
}