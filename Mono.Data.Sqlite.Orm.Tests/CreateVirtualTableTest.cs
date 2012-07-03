namespace Mono.Data.Sqlite.Orm.Tests
{
    using Mono.Data.Sqlite.Orm.ComponentModel;
    using NUnit.Framework;

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
                var correct =
@"CREATE VIRTUAL TABLE [SimpleTable] USING FTS4 (
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
                var correct =
@"CREATE VIRTUAL TABLE [TokenizedSimpleTable] USING FTS4 (
[Id] integer NOT NULL,
[Name] text ,
[BigInt] bigint NOT NULL,
[IsWorking] integer NOT NULL,
tokenize=porter
);";

                Assert.AreEqual(correct, sql);
            }
        }
    }
}