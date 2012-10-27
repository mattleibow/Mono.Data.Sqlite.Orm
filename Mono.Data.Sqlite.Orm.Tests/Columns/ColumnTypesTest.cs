using System;

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
    public class ColumnTypesTest
    {
        public enum EEnum
        {
            EnumVal1 = 42,

            EnumVal2 = -12
        };

        public class Test
        {
            [PrimaryKey]
            public int Id { get; set; }

            public Int32 Int32 { get; set; }

            public String String { get; set; }

            public Byte Byte { get; set; }

            public UInt16 UInt16 { get; set; }

            public SByte SByte { get; set; }

            public Int16 Int16 { get; set; }

            public Boolean Boolean { get; set; }

            public UInt32 UInt32 { get; set; }

            public Int64 Int64 { get; set; }

            public Single Single { get; set; }

            public Double Double { get; set; }

            public Decimal Decimal { get; set; }

            public EEnum Enum1 { get; set; }

            public EEnum Enum2 { get; set; }

            public DateTime Timestamp { get; set; }

            public byte[] Blob { get; set; }

            public Guid Guid { get; set; }
        }

        [Test]
        public void ColumnsSaveLoadMaxCorrectly()
        {
            var db = new OrmTestSession();
            db.CreateTable<Test>();

            var test = new Test
                           {
                               Id = 0,
                               Int32 = Int32.MaxValue,
                               String = "A unicode string \u2022 <- bullet point",
                               Byte = Byte.MaxValue,
                               UInt16 = UInt16.MaxValue,
                               SByte = SByte.MaxValue,
                               Int16 = Int16.MaxValue,
                               Boolean = true,
                               UInt32 = UInt32.MaxValue,
                               Int64 = Int64.MaxValue,
                               Single = Single.MaxValue,
                               Double = Double.MaxValue,
                               Decimal = 79228162514264300000000000000M,
                               Enum1 = EEnum.EnumVal1,
                               Enum2 = EEnum.EnumVal2,
                               Timestamp = DateTime.Parse("2012-04-05 15:08:24.723"),
                               Blob = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                               Guid = Guid.NewGuid()
                           };

            db.Insert(test);
            var res = db.Get<Test>(test.Id);

            Assert.AreEqual(test.Id, res.Id);
            Assert.AreEqual(test.Int32, res.Int32);
            Assert.AreEqual(test.String, res.String);
            Assert.AreEqual(test.Byte, res.Byte);
            Assert.AreEqual(test.UInt16, res.UInt16);
            Assert.AreEqual(test.SByte, res.SByte);
            Assert.AreEqual(test.Int16, res.Int16);
            Assert.AreEqual(test.Boolean, res.Boolean);
            Assert.AreEqual(test.UInt32, res.UInt32);
            Assert.AreEqual(test.Int64, res.Int64);
            Assert.AreEqual(test.Single, res.Single);
            Assert.AreEqual(test.Double, res.Double);
            Assert.AreEqual(test.Decimal, res.Decimal);
            Assert.AreEqual(test.Enum1, res.Enum1);
            Assert.AreEqual(test.Enum2, res.Enum2);
            Assert.AreEqual(test.Timestamp, res.Timestamp);
            ArrayAssert.AreEqual(test.Blob, res.Blob);
            Assert.AreEqual(test.Guid, res.Guid);
        }

        [Test]
        public void ColumnsSaveLoadMinCorrectly()
        {
            var db = new OrmTestSession();
            db.CreateTable<Test>();

            var test = new Test
                           {
                               Id = 0,
                               Int32 = Int32.MinValue,
                               String = "A unicode string \u2022 <- bullet point",
                               Byte = Byte.MinValue,
                               UInt16 = UInt16.MinValue,
                               SByte = SByte.MinValue,
                               Int16 = Int16.MinValue,
                               Boolean = false,
                               UInt32 = UInt32.MinValue,
                               Int64 = Int64.MinValue,
                               Single = Single.MinValue,
                               Double = Double.MinValue,
                               Decimal = -79228162514264300000000000000M,
                               Enum1 = EEnum.EnumVal1,
                               Enum2 = EEnum.EnumVal2,
                               Timestamp = DateTime.Parse("2012-04-05 15:08:24.723"),
                               Blob = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                               Guid = Guid.NewGuid()
                           };

            db.Insert(test);
            var res = db.Get<Test>(test.Id);

            Assert.AreEqual(test.Id, res.Id);
            Assert.AreEqual(test.Int32, res.Int32);
            Assert.AreEqual(test.String, res.String);
            Assert.AreEqual(test.Byte, res.Byte);
            Assert.AreEqual(test.UInt16, res.UInt16);
            Assert.AreEqual(test.SByte, res.SByte);
            Assert.AreEqual(test.Int16, res.Int16);
            Assert.AreEqual(test.Boolean, res.Boolean);
            Assert.AreEqual(test.UInt32, res.UInt32);
            Assert.AreEqual(test.Int64, res.Int64);
            Assert.AreEqual(test.Single, res.Single);
            Assert.AreEqual(test.Double, res.Double);
            Assert.AreEqual(test.Decimal, res.Decimal);
            Assert.AreEqual(test.Enum1, res.Enum1);
            Assert.AreEqual(test.Enum2, res.Enum2);
            Assert.AreEqual(test.Timestamp, res.Timestamp);
            ArrayAssert.AreEqual(test.Blob, res.Blob);
            Assert.AreEqual(test.Guid, res.Guid);
        }

        [Test]
        public void ColumnsSaveLoadZerosCorrectly()
        {
            var db = new OrmTestSession();
            db.CreateTable<Test>();

            var test = new Test
                           {
                               Id = 0,
                               Int32 = 0,
                               String = "A unicode string \u2022 <- bullet point",
                               Byte = 0,
                               UInt16 = 0,
                               SByte = 0,
                               Int16 = 0,
                               Boolean = false,
                               UInt32 = 0,
                               Int64 = 0,
                               Single = Single.Epsilon,
                               Double = Double.Epsilon,
                               Decimal = Decimal.Zero,
                               Enum1 = EEnum.EnumVal1,
                               Enum2 = EEnum.EnumVal2,
                               Timestamp = DateTime.Parse("0001-01-01 00:00:00.000"),
                               Blob = new byte[] { 0 },
                               Guid = Guid.NewGuid()
                           };

            db.Insert(test);
            var res = db.Get<Test>(test.Id);

            Assert.AreEqual(test.Id, res.Id);
            Assert.AreEqual(test.Int32, res.Int32);
            Assert.AreEqual(test.String, res.String);
            Assert.AreEqual(test.Byte, res.Byte);
            Assert.AreEqual(test.UInt16, res.UInt16);
            Assert.AreEqual(test.SByte, res.SByte);
            Assert.AreEqual(test.Int16, res.Int16);
            Assert.AreEqual(test.Boolean, res.Boolean);
            Assert.AreEqual(test.UInt32, res.UInt32);
            Assert.AreEqual(test.Int64, res.Int64);
            Assert.AreEqual(test.Single, res.Single);
            Assert.AreEqual(test.Double, res.Double);
            Assert.AreEqual(test.Decimal, res.Decimal);
            Assert.AreEqual(test.Enum1, res.Enum1);
            Assert.AreEqual(test.Enum2, res.Enum2);
            Assert.AreEqual(test.Timestamp, res.Timestamp);
            ArrayAssert.AreEqual(test.Blob, res.Blob);
            Assert.AreEqual(test.Guid, res.Guid);
        }

        [Test]
        public void ColumnsSaveLoadRandomValuesCorrectly()
        {
            var db = new OrmTestSession();
            db.CreateTable<Test>();

            var test = new Test
                           {
                               Id = 0,
                               Int32 = 0x1337beef,
                               String = "A unicode string \u2022 <- bullet point",
                               Byte = 0xEA,
                               UInt16 = 65535,
                               SByte = -128,
                               Int16 = -32768,
                               Boolean = false,
                               UInt32 = 0xdeadbeef,
                               Int64 = 0x123456789abcdef,
                               Single = 6.283185f,
                               Double = 6.283185307179586476925286766559,
                               Decimal = (decimal)6.283185307179586476925286766559,
                               Enum1 = EEnum.EnumVal1,
                               Enum2 = EEnum.EnumVal2,
                               Timestamp = DateTime.Parse("2012-04-05 15:08:24.723"),
                               Blob = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                               Guid = Guid.NewGuid()
                           };

            db.Insert(test);
            var res = db.Get<Test>(test.Id);

            Assert.AreEqual(test.Id, res.Id);
            Assert.AreEqual(test.Int32, res.Int32);
            Assert.AreEqual(test.String, res.String);
            Assert.AreEqual(test.Byte, res.Byte);
            Assert.AreEqual(test.UInt16, res.UInt16);
            Assert.AreEqual(test.SByte, res.SByte);
            Assert.AreEqual(test.Int16, res.Int16);
            Assert.AreEqual(test.Boolean, res.Boolean);
            Assert.AreEqual(test.UInt32, res.UInt32);
            Assert.AreEqual(test.Int64, res.Int64);
            Assert.AreEqual(test.Single, res.Single);
            Assert.AreEqual(test.Double, res.Double);
            Assert.AreEqual(test.Decimal, res.Decimal);
            Assert.AreEqual(test.Enum1, res.Enum1);
            Assert.AreEqual(test.Enum2, res.Enum2);
            Assert.AreEqual(test.Timestamp, res.Timestamp);
            ArrayAssert.AreEqual(test.Blob, res.Blob);
            Assert.AreEqual(test.Guid, res.Guid);
        }
    }
}