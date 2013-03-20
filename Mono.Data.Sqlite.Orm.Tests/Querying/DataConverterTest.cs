namespace Mono.Data.Sqlite.Orm.Tests
{
    using System;
#if SILVERLIGHT || WINDOWS_PHONE
    using System.Windows.Media;
#elif NETFX_CORE
    using Windows.UI;
#else
    using System.Drawing;
#endif
    using System.Linq;

    using ComponentModel;

    using DataConverter;
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

    [TestFixture]
    public class DataConverterTest
    {
        public class EnumConverter : IDataConverter
        {
            // convert to db type
            public object Convert(object value, Type targetType, object parameter)
            {
                if (value == null)
                {
                    return null;
                }
                return Enum.Parse(typeof(MyEnum), value.ToString(), true);
            }

            // convert to c# type
            public object ConvertBack(object value, Type targetType, object parameter)
            {
                if (value == null)
                {
                    return null;
                }

                return value.ToString();
            }
        }

        public class ColorConverter : IDataConverter
        {
            public object Convert(object value, Type targetType, object parameter)
            {
                Assert.AreEqual("SomeParameter", parameter);

                Color color;

                if (value is Color)
                {
                    color = (Color)value;
                }
                else
                {
                    color = Color.FromArgb(0, 0, 0, 0);
                }

                return string.Join("/", color.A, color.R, color.G, color.B);
            }

            public object ConvertBack(object value, Type targetType, object parameter)
            {
                Assert.AreEqual("SomeParameter", parameter);

                try
                {
                    var parts = value.ToString().Split('/');
                    return Color.FromArgb(
                        byte.Parse(parts[0]), 
                        byte.Parse(parts[1]), 
                        byte.Parse(parts[2]),
                        byte.Parse(parts[3]));
                }
                catch
                {
                    return Color.FromArgb(0, 0, 0, 0);
                }
            }
        }

        [Table("TestTable")]
        public class TestConverter
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            [DataConverter(typeof(ColorConverter), typeof(string), Parameter = "SomeParameter")]
            public Color Color { get; set; }
        }

        [Table("TestTable")]
        public class TestPlain
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Color { get; set; }
        }

        public class EnumTestTable
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public MyEnum? EnumColumn { get; set; }
        }

        [Table("EnumTestTable")]
        public class EnumTestTablePlain
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            [DataConverter(typeof(EnumConverter), typeof(MyEnum?))]
            public string EnumColumn { get; set; }
        }

        public enum MyEnum
        {
            First = 1,
            Second = 2,
            Third = 3
        }

        [Test]
        public void EnumDataConverterTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<EnumTestTable>();

            db.Insert(new EnumTestTable { EnumColumn = MyEnum.Second });
            db.Insert(new EnumTestTable { EnumColumn = null });

            var plain = db.Get<EnumTestTablePlain>(1);
            Assert.AreEqual("2", plain.EnumColumn);

            plain = db.Get<EnumTestTablePlain>(2);
            Assert.IsNull(plain.EnumColumn);

            var rich = db.Get<EnumTestTable>(1);
            Assert.AreEqual(MyEnum.Second, rich.EnumColumn);

            rich = db.Get<EnumTestTable>(2);
            Assert.IsNull(rich.EnumColumn);

            db.Insert(new EnumTestTablePlain { EnumColumn = "2" });
            db.Insert(new EnumTestTablePlain { EnumColumn = null });

            plain = db.Get<EnumTestTablePlain>(3);
            Assert.AreEqual("2", plain.EnumColumn);

            plain = db.Get<EnumTestTablePlain>(4);
            Assert.IsNull(plain.EnumColumn);

            rich = db.Get<EnumTestTable>(3);
            Assert.AreEqual(MyEnum.Second, rich.EnumColumn);

            rich = db.Get<EnumTestTable>(4);
            Assert.IsNull(rich.EnumColumn);
        }

        [Test]
        public void DataConverterCreateTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestConverter>();

            var column = db.GetMapping<TestConverter>().EditableColumns.First();

            var dbType = column.ColumnType;
            var converter = column.DataConverter.GetType();

            Assert.AreEqual(typeof(string), dbType);
            Assert.AreEqual(typeof(ColorConverter), converter);
        }

        [Test]
        public void DataConverterInsertTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestConverter>();

            db.Insert(new TestConverter { Color = Color.FromArgb(255, 255, 0, 0) });

            var plain = db.Get<TestPlain>(1);

            Assert.AreEqual("255/255/0/0", plain.Color);
        }

        [Test]
        public void DataConverterSelectTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestConverter>();

            db.Insert(new TestPlain { Color = "255/0/255/0" });

            var withC = db.Get<TestConverter>(1);

            Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), withC.Color);
        }
    }
}