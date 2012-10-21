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
    public class CreateVirtualTableTest
    {
        [Virtual]
        public class SimpleTable
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public long BigInt { get; set; }

            public bool IsWorking { get; set; }
        }

        [Test]
        public void CreateSimpleTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();

                var sql = new TableMapping(typeof(SimpleTable)).GetCreateSql();
                var correct = @"CREATE VIRTUAL TABLE [SimpleTable] USING FTS4 (
[Id] integer NOT NULL,
[Name] text ,
[BigInt] bigint NOT NULL,
[IsWorking] integer NOT NULL
);";

                Assert.AreEqual(correct, sql);
            }
        }

        [Virtual]
        [Tokenizer(CommonVirtualTableTokenizers.Porter)]
        public class TokenizedSimpleTable
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public long BigInt { get; set; }

            public bool IsWorking { get; set; }
        }

        [Test]
        public void CreateTokenizedSimpleTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<TokenizedSimpleTable>();

                var sql = new TableMapping(typeof(TokenizedSimpleTable)).GetCreateSql();
                var correct = @"CREATE VIRTUAL TABLE [TokenizedSimpleTable] USING FTS4 (
[Id] integer NOT NULL,
[Name] text ,
[BigInt] bigint NOT NULL,
[IsWorking] integer NOT NULL,
tokenize=porter
);";

                Assert.AreEqual(correct, sql);
            }
        }

        [Virtual]
        [Tokenizer(CommonVirtualTableTokenizers.Porter)]
        public class QueryableTable
        {
            public string Name { get; set; }
        }

        [Test]
        public void StringQueryOnQueryableTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<QueryableTable>();
                db.Insert(new QueryableTable { Name = "sqlite" });
                db.Insert(new QueryableTable { Name = "sqlevy" });
                db.Insert(new QueryableTable { Name = "cars" });
                db.Insert(new QueryableTable { Name = "we know sqlite is cool" });
                db.Insert(new QueryableTable { Name = "we think sqlites" });

                var count = db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM [QueryableTable] WHERE [Name] MATCH ?", "sqlite");

                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void TableQueryOnQueryableTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<QueryableTable>();
                db.Insert(new QueryableTable { Name = "sqlite" });
                db.Insert(new QueryableTable { Name = "sqlevy" });
                db.Insert(new QueryableTable { Name = "cars" });
                db.Insert(new QueryableTable { Name = "we know sqlite is cool" });
                db.Insert(new QueryableTable { Name = "we think sqlites" });

                var count = db.Table<QueryableTable>().Where(x => x.Name.Matches("sqlite")).Count();

                Assert.AreEqual(3, count);
            }
        }
    }
}