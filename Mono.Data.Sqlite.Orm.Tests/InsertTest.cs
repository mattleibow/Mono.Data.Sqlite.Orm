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
    public class InsertTest
    {
        public class TestObj
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class TestObjPlain
        {
            public string Text { get; set; }
        }

        public class TestObj2
        {
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        public class TestObjDateTime
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public Guid Guid { get; set; }
            public DateTime TheDate { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, TheDate={1}]", Id, TheDate);
            }
        }

        [Test]
        public void InsertALotPlain()
        {
            var db = new OrmTestSession();
            
            SqliteSession.Trace = false;

            db.CreateTable<TestObjPlain>();

            const int n = 100000;
            IEnumerable<TestObjPlain> q = Enumerable.Range(1, n).Select(i => new TestObjPlain { Text = "I am" });
            TestObjPlain[] objs = q.ToArray();
            int numIn = db.InsertAll(objs);

            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TestObjPlain[] inObjs = db.Table<TestObjPlain>().ToArray();

            foreach (TestObjPlain t in inObjs)
            {
                Assert.AreEqual("I am", t.Text);
            }

            int numCount = db.Table<TestObjPlain>().Count();

            Assert.AreEqual(numCount, n, "Num counted must = num objects");

            db.Close();
        }

        [Test]
        public void InsertALot()
        {
            var db = new OrmTestSession();

            SqliteSession.Trace = false;

            db.CreateTable<TestObj>();

            const int n = 100000;
            IEnumerable<TestObj> q = Enumerable.Range(1, n).Select(i => new TestObj {Text = "I am"});
            TestObj[] objs = q.ToArray();
            int numIn = db.InsertAll(objs);

            Assert.AreEqual(numIn, n, "Num inserted must = num objects");

            TestObj[] inObjs = db.Table<TestObj>().ToArray();

            for (int i = 0; i < inObjs.Length; i++)
            {
                Assert.AreEqual(i + 1, objs[i].Id);
                Assert.AreEqual(i + 1, inObjs[i].Id);
                Assert.AreEqual("I am", inObjs[i].Text);
            }

            int numCount = db.Table<TestObj>().Count();

            Assert.AreEqual(numCount, n, "Num counted must = num objects");

            db.Close();
        }

        [Test]
        public void InsertDateTime()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObjDateTime>();

            DateTime date = DateTime.Now;
            var obj1 = new TestObjDateTime {TheDate = date};

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);

            TestObjDateTime result = db.Table<TestObjDateTime>().First();
            Assert.AreEqual(date, result.TheDate);
            Assert.AreEqual(obj1.TheDate, result.TheDate);

            db.Close();
        }

        [Test]
        public void InsertGuid()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObjDateTime>();
            
            var obj2 = new TestObjDateTime {Guid = Guid.NewGuid()};

            int numIn1 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);

            TestObjDateTime result = db.Table<TestObjDateTime>().First(i => i.Id == obj2.Id);
            Assert.AreEqual(obj2.Guid, result.Guid);

            db.Close();
        }

        [Test]
        public void InsertIntoTwoTables()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();
            db.CreateTable<TestObj2>();

            var obj1 = new TestObj {Text = "GLaDOS loves testing!"};
            var obj2 = new TestObj2 {Text = "Keep testing, just keep testing"};

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result1 = db.Table<TestObj>().ToList();
            Assert.AreEqual(numIn1, result1.Count);
            Assert.AreEqual(obj1.Text, result1.First().Text);

            List<TestObj> result2 = db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(numIn2, result2.Count);

            db.Close();
        }

        [Test]
        public void InsertTwoTimes()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var obj1 = new TestObj {Text = "GLaDOS loves testing!"};
            var obj2 = new TestObj {Text = "Keep testing, just keep testing"};

            int numIn1 = db.Insert(obj1);
            int numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            List<TestObj> result = db.Query<TestObj>("select * from TestObj").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(obj1.Text, result[0].Text);
            Assert.AreEqual(obj2.Text, result[1].Text);

            db.Close();
        }

        // BUG: Excetions ar silently swallowed in Silverlight
        //  - http://code.google.com/p/csharp-sqlite/issues/detail?id=130
        [Test]
        public void InsertWithExtra()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj2>();

            var obj1 = new TestObj2 {Id = 1, Text = "GLaDOS loves testing!"};
            var obj2 = new TestObj2 {Id = 1, Text = "Keep testing, just keep testing"};
            var obj3 = new TestObj2 {Id = 1, Text = "Done testing"};

            db.Insert(obj1);


            try
            {
                db.Insert(obj2);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SqliteException)
            {
            }
            db.Insert(obj2, ConflictResolution.Replace);


            try
            {
                db.Insert(obj3);
                Assert.Fail("Expected unique constraint violation");
            }
            catch (SqliteException)
            {
            }
            db.Insert(obj3, ConflictResolution.Ignore);

            List<TestObj> result = db.Query<TestObj>("select * from TestObj2").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(obj2.Text, result.First().Text);


            db.Close();
        }

        [Test]
        public void Unicode()
        {
            var insertItem = new TestObj
                                 {
                                     Text = "Sinéad O'Connor"
                                 };

            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            int numIn = db.Insert(insertItem);

            Assert.AreEqual(numIn, 1, "Num inserted must = num objects");

            TestObj inObjs = db.Query<TestObj>("select * from TestObj").First();

            Assert.AreEqual(insertItem.Text, inObjs.Text);
        }
    }
}