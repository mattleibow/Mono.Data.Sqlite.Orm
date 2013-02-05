using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT || MS_TEST
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

        public class NamedCompositePrimaryKeyTable
        {
            [PrimaryKey(Name = "PK_Alfred")]
            public int FirstId { get; set; }

            [PrimaryKey(Name = "PK_Alfred")]
            public int SecondId { get; set; }
        }

        [Test]
        public void NamedCompositePrimaryKeyTableTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<NamedCompositePrimaryKeyTable>();

            var sql = new TableMapping(typeof(NamedCompositePrimaryKeyTable)).GetCreateSql();
            var correct = @"CREATE TABLE [NamedCompositePrimaryKeyTable] (
[FirstId] integer NOT NULL,
[SecondId] integer NOT NULL,
CONSTRAINT PK_Alfred
PRIMARY KEY ([FirstId], [SecondId])
);";

            Assert.AreEqual(correct, sql);
        }

        [Test]
        public void MultiplePkOperations()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            // insert
            const int n = 10, m = 5;
            var objs = new TestObj[n * m];
            for (int j = 0; j != n; ++j)
            {
                for (int i = 0; i != m; ++i)
                {
                    objs[j * m + i] = new TestObj { Id = j, SubId = i, Text = "I am (" + j + "," + i + ")" };
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
                Assert.AreEqual(SQLiteErrorCode.Constraint, ex.ErrorCode);
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
            {
                Assert.AreEqual("All changed", o.Text);
            }

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

            ExceptionAssert.Throws<InvalidOperationException>(() => db.Get<TestObj>(8, 2));

            db.Execute("delete from TestObj where SubId=2");
            numCount = db.ExecuteScalar<int>("select count(*) from TestObj");
            Assert.AreEqual(numCount, objs.Length - 10);
            foreach (TestObj o in (from o in db.Table<TestObj>() select o))
            {
                Assert.AreNotEqual(2, o.SubId);
            }
        }

        public class StringKeyObject
        {
            [PrimaryKey]
            public string Key { get; set; }

            public string Value { get; set; }
        }

        [Test]
        public void StringPrimaryKeys()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<StringKeyObject>();

                // insert 2 items
                db.Insert(new StringKeyObject { Key = "Name", Value = "Matthew" });
                db.Insert(new StringKeyObject { Key = "Age", Value = "19" });

                // see if they saved
                Assert.AreEqual(2, db.Table<StringKeyObject>().Count());

                // get the age
                var fromDb = db.Table<StringKeyObject>().Where(x => x.Key == "Age").Single();

                // make sure they are correct
                Assert.AreEqual("Age", fromDb.Key);
                Assert.AreEqual("19", fromDb.Value);

                // try updating
                db.Update(new StringKeyObject { Key = "Name", Value = "Matthew Leibowitz" });

                // make sure it wasn't an add
                Assert.AreEqual(2, db.Table<StringKeyObject>().Count());

                // get the name
                fromDb = db.Table<StringKeyObject>().Where(x => x.Key == "Name").Single();

                // make sure the name has changed
                Assert.AreEqual("Matthew Leibowitz", fromDb.Value);

                // try a delete
                db.Delete(new StringKeyObject { Key = "Age" });

                // ensure it was really deleted
                Assert.AreEqual(1, db.Table<StringKeyObject>().Count());
            }
        }
    }
}