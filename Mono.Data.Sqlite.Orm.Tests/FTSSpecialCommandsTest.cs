namespace Mono.Data.Sqlite.Orm.Tests
{
    using System;
    using System.Text;

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

    [TestFixture]
    public class FTSSpecialCommandsTest
    {
        [Virtual(CommonVirtualTableModules.Fts3)]
        public class SimpleTable
        {
            public string Name { get; set; }
        }

        [Test]
        public void OptimizeTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.Optimize<SimpleTable>();
            }
        }

        [Test]
        public void RebuildTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.Rebuild<SimpleTable>();
            }
        }

        [Test]
        public void IntegrityCheckTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.IntegrityCheck<SimpleTable>();
            }
        }

        [Test]
        public void MergeTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.Merge<SimpleTable>();
            }
        }

        [Test]
        public void RunMergeUntilOptimalTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.RunMergeUntilOptimal<SimpleTable>();
            }
        }

        [Test]
        public void AutoMergeTest()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();
                db.Insert(new SimpleTable { Name = RandomString() });
                db.AutoMerge<SimpleTable>();
            }
        }

        ///<summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <param name="lowerCase">If true, generate lowercase string</param>
        /// <returns>Random string</returns>
        private string RandomString(int size = -1)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();

            if (size == -1)
            {
                size = random.Next(25);
            }

            for (int i = 0; i < size; i++)
            {
                builder.Append(Convert.ToChar((int)Math.Floor(26 * random.NextDouble() + 65)));
            }

            return builder.ToString();
        }
    }
}