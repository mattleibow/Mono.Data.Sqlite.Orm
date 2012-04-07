using System;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class ByteArrayTest
    {
        public class ByteArrayClass
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public byte[] Bytes { get; set; }

            public void AssertEquals(ByteArrayClass other)
            {
                Assert.AreEqual(Id, other.Id);
                var actual = other.Bytes;
                CollectionAssert.AreEqual(Bytes, actual);
            }
        }

        // BUG: zero length arrays returned as null in Silverlight 
        //  - http://code.google.com/p/csharp-sqlite/issues/detail?id=149
        [Test]
        [Description("Create objects with various byte arrays and check they can be stored and retrieved correctly")]
        public void ByteArrays()
        {
            //Byte Arrays for comparisson
            var byteArrays = new[]
                                 {
                                     new ByteArrayClass {Bytes = new byte[] {1, 2, 3, 4, 250, 252, 253, 254, 255}}, //Range check
                                     new ByteArrayClass {Bytes = new byte[] {0}}, //null bytes need to be handled correctly
                                     new ByteArrayClass {Bytes = new byte[] {0, 0}},
                                     new ByteArrayClass {Bytes = new byte[] {0, 1, 0}},
                                     new ByteArrayClass {Bytes = new byte[] {1, 0, 1}},
                                     new ByteArrayClass {Bytes = new byte[] {}}, //Empty byte array should stay empty (and not become null)
                                     new ByteArrayClass {Bytes = null} //Null should be supported
                                 };

            var database = new OrmTestSession();
            database.CreateTable<ByteArrayClass>();

            //Insert all of the ByteArrayClass
            foreach (ByteArrayClass b in byteArrays)
                database.Insert(b);

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
        [Description("Create A large byte array and check it can be stored and retrieved correctly")]
        public void LargeByteArray()
        {
            const int byteArraySize = 1024*1024;
            var bytes = new byte[byteArraySize];
            for (int i = 0; i < byteArraySize; i++)
                bytes[i] = (byte) (i%256);

            var byteArray = new ByteArrayClass {Bytes = bytes};

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