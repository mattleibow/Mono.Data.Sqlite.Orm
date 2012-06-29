using System;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;
using IgnoreAttribute = Mono.Data.Sqlite.Orm.ComponentModel.IgnoreAttribute;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class CreateTableTest
    {
        public class SimpleTable
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public long BigInt { get; set; }
            public bool IsWorking { get; set; }
        }

        public class UnknownColumnType
        {
            public SimpleTable IsWorking { get; set; }
        }

        [Test]
        public void CreateUnknownColumnType()
        {
            using (var db = new OrmTestSession())
            {
                try
                {
                    db.CreateTable<UnknownColumnType>();

                    Assert.Fail();
                }
                catch (NotSupportedException)
                {
                }
            }
        }

        [Test]
        public void CreateSimpleTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<SimpleTable>();

                var sql = new TableMapping(typeof(SimpleTable)).GetCreateSql();
                var correct = 
@"CREATE TABLE [SimpleTable] (
[Id] integer NOT NULL,
[Name] text ,
[BigInt] bigint NOT NULL,
[IsWorking] integer NOT NULL);";

                Assert.AreEqual(correct, sql);
            }
        }

        public class EnumTable
        {
            [EnumAffinity(typeof(char))]
            public PersonType Type { get; set; }
            public PersonAge Age { get; set; }
            [EnumAffinity(typeof(string))]
            public PersonKind Kind { get; set; }

            public enum PersonType
            {
                Child = 'C',
                Adult = 'A',
                Senior = 'S'
            }

            public enum PersonAge
            {
                ChildAge = 3,
                AdultAge = 21,
                SeniorAge = 40
            }

            public enum PersonKind
            {
                Senior,
                Junior
            }
        }

        [Test]
        public void CreateEnumTable()
        {
            using (var db = new OrmTestSession())
            {
                db.CreateTable<EnumTable>();

                var sql = new TableMapping(typeof(EnumTable)).GetCreateSql();
                var correct =
@"CREATE TABLE [EnumTable] (
[Type] varchar(1) NOT NULL,
[Age] integer NOT NULL,
[Kind] text NOT NULL);";

                Assert.AreEqual(correct, sql);

                db.Insert(new EnumTable
                              {
                                  Type = EnumTable.PersonType.Child,
                                  Age = EnumTable.PersonAge.ChildAge,
                                  Kind = EnumTable.PersonKind.Junior
                              });
            }
        }

        [Table("DifferentName")]
        public class CustomTable
        {
            public int Id { get; set; }

            [Column("NewName")]
            public string Name { get; set; }

            public bool IsWorking { get; set; }
        }

        [Test]
        public void CreateCustomTable()
        {
            var db = new OrmTestSession();

            db.CreateTable<CustomTable>();

            TableMapping mapping = db.GetMapping<CustomTable>();

            var sql = mapping.GetCreateSql();
            var correct =
@"CREATE TABLE [DifferentName] (
[Id] integer NOT NULL,
[NewName] text ,
[IsWorking] integer NOT NULL);";

            Assert.AreEqual(correct, sql);

            Assert.AreEqual("DifferentName", mapping.TableName);
            Assert.AreEqual("NewName", mapping.Columns[1].Name);
        }

        [Table(OnPrimaryKeyConflict = ConflictResolution.Fail)]
        public class AdvancedTable
        {
            [PrimaryKey(Name = "PK_MyPrimaryKey", Direction = Direction.Desc)]
            public int Id { get; set; }

            [Unique(OnConflict = ConflictResolution.Rollback)]
            public bool? IsWorking { get; set; }
        }

        [Test]
        public void CreateAdvancedTable()
        {
            var db = new OrmTestSession();

            db.CreateTable<AdvancedTable>();

            TableMapping mapping = db.GetMapping<AdvancedTable>();

            var sql = mapping.GetCreateSql();
            var correct =
@"CREATE TABLE [AdvancedTable] (
[Id] integer CONSTRAINT PK_MyPrimaryKey PRIMARY KEY Desc ON CONFLICT Fail NOT NULL,
[IsWorking] integer UNIQUE ON CONFLICT Rollback);";

            Assert.AreEqual(correct, sql);

            Assert.AreEqual(2, mapping.Columns.Count);
            Assert.IsNotNull(mapping.Columns[1].Unique);
            Assert.AreEqual(true, mapping.Columns.First(c => c.Name == "IsWorking").IsNullable);
            Assert.AreEqual(1, mapping.PrimaryKey.Columns.Length);
            Assert.AreEqual(ConflictResolution.Fail, mapping.OnPrimaryKeyConflict);
        }

        [Check("Id <= 10")]
        public class VeryAdvancedTable
        {
            [PrimaryKey(Name = "PK_MyPrimaryKey", Direction = Direction.Desc, Order = 1)]
            [Check("Id <= 25")]
            public int Id { get; set; }

            [Column("DiffName")]
            [MaxLength(255)]
            [Collation(Collation.RTrim)]
            public string Name { get; set; }

            [NotNull]
            [Default("1")]
            public bool? IsWorking { get; set; }

            [PrimaryKey(Name = "PK_MyPrimaryKey", Direction = Direction.Asc, Order = 0)]
            public int AnotherId { get; set; }

            [Ignore]
            public bool? IgnoredColumn { get; set; }
        }

        [Test]
        public void CreateVeryAdvancedTable()
        {
            var db = new OrmTestSession();

            db.CreateTable<VeryAdvancedTable>();

            TableMapping mapping = db.GetMapping<VeryAdvancedTable>();

            var sql = mapping.GetCreateSql();
            var correct =
@"CREATE TABLE [VeryAdvancedTable] (
[Id] integer NOT NULL CHECK (Id <= 25),
[DiffName] varchar(255) COLLATE RTrim,
[IsWorking] integer NOT NULL DEFAULT(1),
[AnotherId] integer NOT NULL,
CONSTRAINT PK_MyPrimaryKey PRIMARY KEY (AnotherId, Id Desc),
CHECK (Id <= 10));";

            Assert.AreEqual(correct, sql);

            Assert.AreEqual("Id <= 10", mapping.Checks.First());
            Assert.AreEqual(2, mapping.PrimaryKey.Columns.Length);
            Assert.AreEqual("AnotherId", mapping.PrimaryKey.Columns[0].Name);
            Assert.AreEqual("Id", mapping.PrimaryKey.Columns[1].Name);

            TableMapping.Column idCol = mapping.Columns.First(c => c.Name == "Id");
            Assert.AreEqual("Id <= 25", idCol.Checks.First());
            Assert.AreEqual("PK_MyPrimaryKey", idCol.PrimaryKey.Name);
            Assert.AreEqual(Direction.Desc, idCol.PrimaryKey.Direction);

            TableMapping.Column difCol = mapping.Columns.First(c => c.Name == "DiffName");
            Assert.AreEqual(255, difCol.MaxStringLength);
            Assert.AreEqual(Collation.RTrim, difCol.Collation);

            TableMapping.Column workinCol = mapping.Columns.First(c => c.Name == "IsWorking");
            Assert.IsFalse(workinCol.IsNullable);
            Assert.AreEqual("1", workinCol.DefaultValue);

            Assert.IsFalse(mapping.Columns.Any(c => c.Name == "IgnoredColumn"));
        }

        public class Category
        {
            [PrimaryKey]
            [AutoIncrement]
            [Column("CatId")]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class Book
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public string Title { get; set; }

            [ForeignKey(typeof(Category), "DoesNotExist", Name = "FK_Book_Category")]
            public int CategoryId { get; set; }
        }

        [Test]
        public void CreateTableWithForeignKeyColumnThatDoesNotExist()
        {
            var db = new OrmTestSession();
            db.CreateTable<Category>();
            try
            {
                db.CreateTable<Book>();

                Assert.Fail();
            }
            catch (SqliteException)
            {
                
            }
            catch
            {
                Assert.Fail();
            }
        }

        public class ReferencingTable
        {
            [ForeignKey(typeof(ReferencedTable), "Id", Name = "FK_Foreign_Key")]
            public int RefId { get; set; }

            [ForeignKey(typeof(ReferencedTable), "Id2", OnUpdateAction = ForeignKeyAction.Cascade)]
            [Column("RandomName")]
            public int Indexed { get; set; }
        }

        public class ReferencedTable
        {
            public int Id { get; set; }
            public int Id2 { get; set; }
        }

        [Test]
        public void CreateReferencingTable()
        {
            var db = new OrmTestSession();

            db.CreateTable<ReferencedTable>();
            db.CreateTable<ReferencingTable>();

            TableMapping refingMap = db.GetMapping<ReferencingTable>();

            var sql = refingMap.GetCreateSql();
            var correct =
@"CREATE TABLE [ReferencingTable] (
[RefId] integer NOT NULL,
[RandomName] integer NOT NULL,
CONSTRAINT FK_Foreign_Key FOREIGN KEY (RefId)
REFERENCES ReferencedTable (Id)
  FOREIGN KEY (RandomName)
REFERENCES ReferencedTable (Id2)
ON UPDATE CASCADE
);";

            Assert.AreEqual(correct, sql);

            Assert.AreEqual(2, refingMap.ForeignKeys.Count);
            Assert.AreEqual(1, refingMap.ForeignKeys.Count(f => f.Name == "FK_Foreign_Key"));
            TableMapping.ForeignKey[] fk = refingMap.ForeignKeys.Where(f => f.ChildTable == "ReferencedTable").ToArray();
            Assert.AreEqual(2, fk.Count());
            Assert.AreEqual("Id", fk.Where(f => f.Keys.First().Key == "RefId").First().Keys.First().Value);
            Assert.AreEqual("Id2", fk.Where(f => f.Keys.First().Key == "RandomName").First().Keys.First().Value);
        }

        public class MultiReferencingTable
        {
            [ForeignKey(typeof(MultiReferencedTable), "Id", Name = "FK_Foreign_Key", Order = 1)]
            public int RefId { get; set; }

            [ForeignKey(typeof(MultiReferencedTable), "Id2", Name = "FK_Foreign_Key", Order = 0)]
            public int Indexed { get; set; }
        }

        public class MultiReferencedTable
        {
            public int Id { get; set; }
            public int Id2 { get; set; }
        }

        [Test]
        public void CreateMultiReferencingTable()
        {
            var db = new OrmTestSession();

            db.CreateTable<MultiReferencedTable>();
            db.CreateTable<MultiReferencingTable>();

            TableMapping refingMap = db.GetMapping<MultiReferencingTable>();

            var sql = refingMap.GetCreateSql();
            var correct =
@"CREATE TABLE [MultiReferencingTable] (
[RefId] integer NOT NULL,
[Indexed] integer NOT NULL,
CONSTRAINT FK_Foreign_Key FOREIGN KEY (Indexed, RefId)
REFERENCES MultiReferencedTable (Id2, Id)
);";

            Assert.AreEqual(correct, sql);

            Assert.AreEqual(1, refingMap.ForeignKeys.Count);
            Assert.AreEqual("FK_Foreign_Key", refingMap.ForeignKeys.First().Name);
            TableMapping.ForeignKey[] fk = refingMap.ForeignKeys.Where(f => f.ChildTable == "MultiReferencedTable").ToArray();
            Assert.AreEqual(1, fk.Count());

            Assert.AreEqual("RefId", fk[0].Keys.Skip(1).First().Key);
            Assert.AreEqual("Id", fk[0].Keys.Skip(1).First().Value);
            Assert.AreEqual("Indexed", fk[0].Keys.First().Key);
            Assert.AreEqual("Id2", fk[0].Keys.First().Value);
        }


        [Index("IX_TabelIndex", Unique = true)]
        public class IndexedTable
        {
            [Indexed("IX_TabelIndex")]
            public int Indexed { get; set; }
        }

        public class IndexedColumnTable
        {
            [Indexed("IX_SomeName", Collation = Collation.RTrim)]
            public int Indexed { get; set; }
        }

        [Index("IX_MultiIndexedTable", Unique = true)]
        public class MultiIndexedTable
        {
            [Indexed("IX_MultiIndexedTable", Collation = Collation.RTrim)]
            public int Indexed { get; set; }

            [Indexed("IX_MultiIndexedTable", Direction = Direction.Desc)]
            public int Second { get; set; }
        }

        // BUG: Creating a Unique Index within a Transaction causes MethodAccessException in Silverlight
        //  - http://code.google.com/p/csharp-sqlite/issues/detail?id=150
        [Test]
        public void CreateIndexedTable()
        {
            var db = new OrmTestSession();
            db.CreateTable<IndexedTable>();
            TableMapping tableMap = db.GetMapping<IndexedTable>();

            var sql = tableMap.Indexes.Single().GetCreateSql("IndexedTable");
            var correct =
@"CREATE UNIQUE INDEX [IX_TabelIndex] on [IndexedTable] (
[Indexed]  );";

            Assert.AreEqual(correct, sql);

            var tblIdx = tableMap.Indexes.Where(i => i.IndexName == "IX_TabelIndex").ToArray();
            Assert.AreEqual(1, tblIdx.Count());
            Assert.AreEqual(true, tblIdx.First().Unique);
        }

        [Test]
        public void CreateIndexedColumnTable()
        {
            var db = new OrmTestSession();
            db.CreateTable<IndexedColumnTable>();
            TableMapping columnMap = db.GetMapping<IndexedColumnTable>();

            var sql = columnMap.Indexes.Single().GetCreateSql("IndexedColumnTable");
            var correct =
@"CREATE  INDEX [IX_SomeName] on [IndexedColumnTable] (
[Indexed] COLLATE RTrim );";

            Assert.AreEqual(correct, sql);

            var colIdx = columnMap.Indexes.Where(i => i.IndexName == "IX_SomeName").ToArray();
            Assert.AreEqual(1, colIdx.Count());
            Assert.AreEqual(Collation.RTrim, colIdx.First().Columns.First().Collation);
        }

        [Test]
        public void CreateMultiIndexedTable()
        {
            var db = new OrmTestSession();
            db.CreateTable<MultiIndexedTable>();
            TableMapping multiMap = db.GetMapping<MultiIndexedTable>();

            var sql = multiMap.Indexes.Single().GetCreateSql("MultiIndexedTable");
            var correct =
@"CREATE UNIQUE INDEX [IX_MultiIndexedTable] on [MultiIndexedTable] (
[Indexed] COLLATE RTrim ,
[Second]  Desc);";

            Assert.AreEqual(correct, sql);

            var multiIdx = multiMap.Indexes.Where(i => i.IndexName == "IX_MultiIndexedTable").ToArray();
            Assert.AreEqual(1, multiIdx.Count());
            Assert.AreEqual(2, multiIdx.First().Columns.Count());
            Assert.AreEqual(Collation.RTrim, multiIdx.First().Columns.First(c => c.ColumnName == "Indexed").Collation);
        }
    }
}