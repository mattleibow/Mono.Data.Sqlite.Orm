using System.Collections.Generic;
using System.Globalization;
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
    using System;

    [TestFixture]
    public class ContainsTest
    {
        public class TestObj
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Name={1}]", Id, Name);
            }
        }
        
        #region String Array

        [Test]
        public void ConstantStringArrayContainsTableColumnTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            const int n = 20;
            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n)
                                      select new TestObj { Name = i.ToString(CultureInfo.InvariantCulture) };

            db.InsertAll(cq);

            var tensq = new[] { "0", "10", "20" };
            List<TestObj> tens = (from o in db.Table<TestObj>() 
                                  where tensq.Contains(o.Name)
                                  select o).ToList();
            Assert.AreEqual(2, tens.Count);

            var moreq = new[] { "0", "x", "99", "10", "20", "234324" };
            List<TestObj> more = (from o in db.Table<TestObj>() 
                                  where moreq.Contains(o.Name)
                                  select o).ToList();
            Assert.AreEqual(2, more.Count);
        }

        [Test]
        public void QueriedStringArrayContainsTableColumnTest()
        {
            int n = 20;
            IEnumerable<TestObj> cq = from i in Enumerable.Range(1, n) select new TestObj { Name = i.ToString() };

            var db = new OrmTestSession();

            // temp stop trace for inputs
            SqliteSession.Trace = false;
            db.CreateTable<TestObj>();
            db.InsertAll(cq);
            SqliteSession.Trace = true;

            var tensArray = new[] { "0", "10", "20" };
            var tensResult = from tens in db.Table<TestObj>() 
                             where tensArray.Contains(tens.Name) 
                             select tens.Name;
            List<TestObj> more2 = (from all in db.Table<TestObj>()
                                   where tensResult.Contains(all.Name)
                                   select all).ToList();
            Assert.AreEqual(2, more2.Count);
        }

        #endregion

        #region Contains String

        [Test]
        public void StringColumnContainsConstantInlineStringTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var stringContainsTest = (from n in db.Table<TestObj>() 
                                      where n.Name.Contains("Good")
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        [Test]
        public void StringColumnContainsStringVariableTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = "good";
            var stringContainsTest = (from n in db.Table<TestObj>() 
                                      where n.Name.Contains(finder) 
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        [Test]
        public void StringColumnContainsStringPropertyOfAnotherObjectTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = new TestObj { Name = "good" };
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.Contains(finder.Name)
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        [Test]
        public void StringColumnContainsStringFunctionTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finderObj = new TestObj { Name = "good" };
            Func<string> finder = () => finderObj.Name;
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.Contains(finder())
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        #endregion

        #region Starts With String

        [Test]
        public void StringColumnStartsWithInlineStringTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var stringContainsTest = (from n in db.Table<TestObj>() 
                                      where n.Name.StartsWith("This")
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        [Test]
        public void StringColumnStartsWithStringVariableTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = "name";
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.StartsWith(finder)
                                      select n).SingleOrDefault();
            Assert.IsNull(stringContainsTest);
        }

        [Test]
        public void StringColumnStartsWithStringPropertyOfAnotherObjectTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = new TestObj { Name = "good" };
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.StartsWith(finder.Name)
                                      select n).SingleOrDefault();
            Assert.IsNull(stringContainsTest);
        }

        #endregion

        #region Ends With String

        [Test]
        public void StringColumnEndsWithConstantInlineStringTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.EndsWith("NAME")
                                      select n).Single();
            Assert.AreEqual(testObj.Id, stringContainsTest.Id);
        }

        [Test]
        public void StringColumnEndsWithStringVariableTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = "is";
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.EndsWith(finder)
                                      select n).SingleOrDefault();
            Assert.IsNull(stringContainsTest);
        }

        [Test]
        public void StringColumnEndsWithStringPropertyOfAnotherObjectTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestObj>();

            var testObj = new TestObj { Name = "This is a Good name" };
            db.Insert(testObj);

            var finder = new TestObj { Name = "good" };
            var stringContainsTest = (from n in db.Table<TestObj>()
                                      where n.Name.EndsWith(finder.Name)
                                      select n).SingleOrDefault();
            Assert.IsNull(stringContainsTest);
        }
        
        #endregion
    }
}