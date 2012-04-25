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

                TableMapping mapping = db.GetMapping<SimpleTable>();
                Assert.AreEqual(4, mapping.Columns.Count);
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
            Assert.AreEqual("DifferentName", mapping.TableName);
            Assert.AreEqual("NewName", mapping.Columns[1].Name);
        }

        [Table(OnPrimaryKeyConflict = ConflictResolution.Fail)]
        public class AdvancedTable
        {
            [AutoIncrement]
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
            Assert.AreEqual(2, mapping.Columns.Count);
            Assert.IsNotNull(mapping.Columns[1].Unique);
            Assert.AreEqual(true, mapping.Columns.First(c => c.Name == "Id").IsAutoIncrement);
            Assert.AreEqual(true, mapping.Columns.First(c => c.Name == "IsWorking").IsNullable);
            Assert.AreEqual(1, mapping.PrimaryKeys.Count);
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

            Assert.AreEqual("Id <= 10", mapping.Checks.First());
            Assert.AreEqual(2, mapping.PrimaryKeys.Count);
            Assert.AreEqual("AnotherId", mapping.PrimaryKeys[0].Name);
            Assert.AreEqual("Id", mapping.PrimaryKeys[1].Name);

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

        [Index("IX_MultiIndexedTable")]
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

            var multiIdx = multiMap.Indexes.Where(i => i.IndexName == "IX_MultiIndexedTable").ToArray();
            Assert.AreEqual(1, multiIdx.Count());
            Assert.AreEqual(2, multiIdx.First().Columns.Count());
            Assert.AreEqual(Collation.RTrim, multiIdx.First().Columns.First(c => c.ColumnName == "Indexed").Collation);
        }
    }
}