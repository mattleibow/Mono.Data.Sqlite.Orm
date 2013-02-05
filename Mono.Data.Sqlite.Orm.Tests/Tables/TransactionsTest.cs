using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;

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
    public class TransactionsTest
    {
        public class TestObj
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public String Text { get; set; }

            public override string ToString()
            {
                return string.Format("[TestObj: Id={0}, Text={1}]", Id, Text);
            }
        }

        [Test]
        public void InsertUsingSystemTransactions()
        {
            var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

            SqliteSession.Trace = true;

            using (var db = new SqliteSession("Data Source=TempDb" + DateTime.Now.Ticks + ".db;DefaultTimeout=100", false))
            {
                db.Connection.Open();
                db.CreateTable<TestObj>();
                db.Connection.Close();

                using (var trans = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    db.Connection.Open();
                    db.Insert(new TestObj { Text = "My Text" });
                }

                Assert.AreEqual(0, db.Table<TestObj>().Count());

                db.Connection.Close();

                using (var trans = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    db.Connection.Open();
                    db.Insert(new TestObj { Text = "My Text" });
                    trans.Complete();
                }

                Assert.AreEqual(1, db.Table<TestObj>().Count());
            }
        }

        [Test]
        public void InsertUsingNestedSystemTransactions()
        {
            var options = new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

            using (var db = new OrmTestSession())
            {
                using (var trans = new TransactionScope(TransactionScopeOption.Required, options))
                {
                    db.CreateTable<TestObj>();
                    db.Insert(new TestObj { Text = "My Text" });

                    using (var trans2 = new TransactionScope(TransactionScopeOption.Required, options))
                    {
                        db.Insert(new TestObj { Text = "My Text" });
                        trans2.Complete();
                    }

                    trans.Complete();
                }

                Assert.AreEqual(2, db.Table<TestObj>().Count());
            }
        }

        [Test]
        public void InsertUsingSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.RollbackSavepoint("First");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 1);
            }
        }

        [Test]
        public void InsertUsingMultipleSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.CreateSavepoint("Second");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 3);

                    trans.RollbackSavepoint("Second");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 2);
            }
        }

        [Test]
        public void InsertUsingInterweavingSavePoints()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans = db.BeginTransaction())
                {
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 1);

                    trans.CreateSavepoint("First");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 2);

                    trans.CreateSavepoint("Second");
                    db.Insert(obj);
                    Assert.AreEqual(db.Table<TestObj>().Count(), 3);

                    trans.RollbackSavepoint("First");

                    trans.Commit();
                }

                Assert.AreEqual(db.Table<TestObj>().Count(), 1);
            }
        }

        [Test]
        public void InsertUsingSavePointsOnACommittedTransaction()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                var trans = db.BeginTransaction();
                trans.CreateSavepoint("First");
                trans.Commit();

#if SILVERLIGHT || MS_TEST
                ExceptionAssert.Throws<SqliteException>(() => trans.RollbackSavepoint("First"));
#elif NETFX_CORE
                Assert.ThrowsException<SqliteException>(() => trans.RollbackSavepoint("First"));
#else
                Assert.Catch<SqliteException>(() => trans.RollbackSavepoint("First"));
#endif
            }
        }

        [Test]
        public void NestedTransactionsTest()
        {
            var obj = new TestObj { Text = "Matthew" };

            using (var db = new OrmTestSession())
            {
                db.CreateTable<TestObj>();

                using (var trans1 = db.BeginTransaction())
                {
                    trans1.CreateSavepoint("First");
                    db.Insert(obj);

                    using (var trans2 = db.BeginTransaction())
                    {
                        // rollback on the second trans affects the first as 
                        // it is the same transaction 
                        trans2.RollbackSavepoint("First");
                    }

                    Assert.AreEqual(db.Table<TestObj>().Count(), 0);
                }
            }
        }
    }
}