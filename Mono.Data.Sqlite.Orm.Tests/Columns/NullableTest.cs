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
    [TestFixture]
    public class NullableTest
    {
        public class NullableIntClass
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public int? NullableInt { get; set; }

            public override bool Equals(object obj)
            {
                var other = (NullableIntClass)obj;
                return Id == other.Id && NullableInt == other.NullableInt;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ NullableInt.GetHashCode();
            }
        }

        public class NullableFloatClass
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public float? NullableFloat { get; set; }

            public override bool Equals(object obj)
            {
                var other = (NullableFloatClass)obj;
                return Id == other.Id && NullableFloat == other.NullableFloat;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ NullableFloat.GetHashCode();
            }
        }

        public class StringClass
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public string StringData { get; set; }

            //Strings are allowed to be null by default
            public override bool Equals(object obj)
            {
                var other = (StringClass)obj;
                return Id == other.Id && StringData == other.StringData;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ StringData.GetHashCode();
            }
        }

        [Test]
        // [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableFloat()
        {
            var db = new OrmTestSession();
            db.CreateTable<NullableFloatClass>();

            var withNull = new NullableFloatClass { NullableFloat = null };
            var with0 = new NullableFloatClass { NullableFloat = 0 };
            var with1 = new NullableFloatClass { NullableFloat = 1 };
            var withMinus1 = new NullableFloatClass { NullableFloat = -1 };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableFloatClass[] results = db.Table<NullableFloatClass>().OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(4, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(with0, results[1]);
            Assert.AreEqual(with1, results[2]);
            Assert.AreEqual(withMinus1, results[3]);
        }

        [Test]
        // [Description("Create a table with a nullable int column then insert and select against it")]
        public void NullableInt()
        {
            var db = new OrmTestSession();
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass { NullableInt = null };
            var with0 = new NullableIntClass { NullableInt = 0 };
            var with1 = new NullableIntClass { NullableInt = 1 };
            var withMinus1 = new NullableIntClass { NullableInt = -1 };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results = db.Table<NullableIntClass>().OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(4, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(with0, results[1]);
            Assert.AreEqual(with1, results[2]);
            Assert.AreEqual(withMinus1, results[3]);
        }

        [Test]
        public void NullableString()
        {
            var db = new OrmTestSession();
            db.CreateTable<StringClass>();

            var withNull = new StringClass { StringData = null };
            var withEmpty = new StringClass { StringData = "" };
            var withData = new StringClass { StringData = "data" };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results = db.Table<StringClass>().OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(withNull, results[0]);
            Assert.AreEqual(withEmpty, results[1]);
            Assert.AreEqual(withData, results[2]);
        }

        [Test]
        public void StringWhereNotNull()
        {
            var db = new OrmTestSession();
            db.CreateTable<StringClass>();

            var withNull = new StringClass { StringData = null };
            var withEmpty = new StringClass { StringData = "" };
            var withData = new StringClass { StringData = "data" };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results =
                db.Table<StringClass>().Where(x => x.StringData != null).OrderBy(x => x.Id).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.AreEqual(withEmpty, results[0]);
            Assert.AreEqual(withData, results[1]);
        }

        [Test]
        public void StringWhereNull()
        {
            var db = new OrmTestSession();
            db.CreateTable<StringClass>();

            var withNull = new StringClass { StringData = null };
            var withEmpty = new StringClass { StringData = "" };
            var withData = new StringClass { StringData = "data" };

            db.Insert(withNull);
            db.Insert(withEmpty);
            db.Insert(withData);

            StringClass[] results =
                db.Table<StringClass>().Where(x => x.StringData == null).OrderBy(x => x.Id).ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(withNull, results[0]);
        }

        [Test]
        public void WhereNotNull()
        {
            var db = new OrmTestSession();
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass { NullableInt = null };
            var with0 = new NullableIntClass { NullableInt = 0 };
            var with1 = new NullableIntClass { NullableInt = 1 };
            var withMinus1 = new NullableIntClass { NullableInt = -1 };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results =
                db.Table<NullableIntClass>().Where(x => x.NullableInt != null).OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(3, results.Length);

            Assert.AreEqual(with0, results[0]);
            Assert.AreEqual(with1, results[1]);
            Assert.AreEqual(withMinus1, results[2]);
        }

        [Test]
        public void WhereNull()
        {
            var db = new OrmTestSession();
            db.CreateTable<NullableIntClass>();

            var withNull = new NullableIntClass { NullableInt = null };
            var with0 = new NullableIntClass { NullableInt = 0 };
            var with1 = new NullableIntClass { NullableInt = 1 };
            var withMinus1 = new NullableIntClass { NullableInt = -1 };

            db.Insert(withNull);
            db.Insert(with0);
            db.Insert(with1);
            db.Insert(withMinus1);

            NullableIntClass[] results =
                db.Table<NullableIntClass>().Where(x => x.NullableInt == null).OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(withNull, results[0]);
        }
    }
}