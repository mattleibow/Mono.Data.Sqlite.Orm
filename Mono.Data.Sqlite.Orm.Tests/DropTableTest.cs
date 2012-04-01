using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

#if SILVERLIGHT || WINDOWS_PHONE
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class DropTableTest
    {
        public class Product
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        [Test]
        public void CreateInsertDrop()
        {
            var db = new OrmTestSession();

            db.CreateTable<Product>();

            db.Insert(new Product
                          {
                              Name = "Hello",
                              Price = 16,
                          });

            int n = db.Table<Product>().Count();

            Assert.AreEqual(1, n);

            db.DropTable<Product>();

            try
            {
                // Should throw SqliteException
                db.Table<Product>().Count();

                Assert.Fail("Expeced 'table does not exist' error.");
            }
            catch (SqliteException)
            {
            }
            catch
            {
                Assert.Fail();
            }
        }
    }
}