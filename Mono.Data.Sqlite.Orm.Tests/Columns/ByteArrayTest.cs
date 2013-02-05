using System;
using System.Linq;

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
    public class ByteArrayTest
    {
        public class ByteArrayClass
        {
            [PrimaryKey]
            [AutoIncrement]
            public int Id { get; set; }

            public byte[] Bytes { get; set; }

            public void AssertEquals(ByteArrayClass other)
            {
                Assert.AreEqual(Id, other.Id);
                var actual = other.Bytes;
                CollectionAssert.AreEqual(Bytes, actual);
            }
        }

        [Test]
        // [Description("Create objects with various byte arrays and check they can be stored and retrieved correctly")]
        public void ByteArraysSavedCorrectlyTest()
        {
            //Byte Arrays for comparisson
            var byteArrays = new[]
                {
                    new ByteArrayClass { Bytes = new byte[] { 1, 2, 3, 4, 250, 252, 253, 254, 255 } }, // Range check
                    new ByteArrayClass { Bytes = new byte[] { 0, 0 } },
                    new ByteArrayClass { Bytes = new byte[] { 0, 1, 0 } },
                    new ByteArrayClass { Bytes = new byte[] { 1, 0, 1 } },
                };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            //Insert all of the ByteArrayClass
            foreach (ByteArrayClass b in byteArrays)
            {
                database.Insert(b);
            }

            //Get them back out
            ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().OrderBy(x => x.Id).ToArray();

            Assert.AreEqual(fetchedByteArrays.Length, byteArrays.Length);
            //Check they are the same
            for (int i = 0; i < byteArrays.Length; i++)
            {
                var byteArrayClass = byteArrays[i];
                var other = fetchedByteArrays[i];

                var actual = byteArrayClass.Bytes;
                var expected = other.Bytes;

                byteArrayClass.AssertEquals(other);
            }
        }

        [Test]
        public void EmptyByteArraySavedAndRetrievedCorrectlyTest()
        {
            //Empty byte array should stay empty (and not become null)
            var byteArray = new ByteArrayClass { Bytes = new byte[] { } };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            database.Insert(byteArray);

            //Get them back out
            var fetchedByteArray = database.Table<ByteArrayClass>().Single();

            //Check they are the same
            ArrayAssert.AreEqual(byteArray.Bytes, fetchedByteArray.Bytes);
        }

        [Test]
        public void ZeroByteArraySavedAndRetrievedCorrectlyTest()
        {
            var byteArray = new ByteArrayClass { Bytes = new byte[] { 0 } };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            database.Insert(byteArray);

            //Get them back out
            var fetchedByteArray = database.Table<ByteArrayClass>().Single();

            //Check they are the same
            ArrayAssert.AreEqual(byteArray.Bytes, fetchedByteArray.Bytes);
        }

        [Test]
        public void NullByteArraySavedAndRetrievedCorrectlyTest()
        {
            var byteArray = new ByteArrayClass { Bytes = null };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            database.Insert(byteArray);

            //Get them back out
            var fetchedByteArray = database.Table<ByteArrayClass>().Single();

            //Check they are the same
            ArrayAssert.AreEqual(byteArray.Bytes, fetchedByteArray.Bytes);
        }

        [Test]
        // [Description("Create A large byte array and check it can be stored and retrieved correctly")]
        public void LargeByteArrayTest()
        {
            const int byteArraySize = 1024 * 1024;
            var bytes = new byte[byteArraySize];
            for (int i = 0; i < byteArraySize; i++)
            {
                bytes[i] = (byte)(i % 256);
            }

            var byteArray = new ByteArrayClass { Bytes = bytes };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            //Insert the ByteArrayClass
            database.Insert(byteArray);

            //Get it back out
            ByteArrayClass[] fetchedByteArrays = database.Table<ByteArrayClass>().ToArray();

            Assert.AreEqual(fetchedByteArrays.Length, 1);

            //Check they are the same
            byteArray.AssertEquals(fetchedByteArrays[0]);
        }
    }
}