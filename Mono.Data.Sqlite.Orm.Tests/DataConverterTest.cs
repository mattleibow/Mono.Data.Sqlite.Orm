namespace Mono.Data.Sqlite.Orm.Tests
{
    using System;
    using System.Drawing;
    using System.Linq;

    using ComponentModel;
    using DataConverter;

    using NUnit.Framework;
    
    [TestFixture]
    public class DataConverterTest
    {
        public class ColorConverter : IDataConverter
        {
            public object Convert(object value, Type targetType, object parameter)
            {
                Assert.AreEqual("SomeParameter", parameter);

                Color color;

                if (value is Color)
                {
                    color = (Color) value;
                }
                else
                {
                    color = Color.Transparent;
                }

                return string.Join("/", color.A, color.R, color.G, color.B);
            }

            public object ConvertBack(object value, Type targetType, object parameter)
            {
                Assert.AreEqual("SomeParameter", parameter);

                try
                {
                    var parts = value.ToString().Split('/');
                    return Color.FromArgb(int.Parse(parts[0]),
                                          int.Parse(parts[1]),
                                          int.Parse(parts[2]),
                                          int.Parse(parts[3]));
                }
                catch
                {
                    return Color.Transparent;
                }
            }
        }

        [Table("TestTable")]
        public class TestConverter
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            [DataConverter(typeof(ColorConverter), typeof(string), Parameter = "SomeParameter")]
            public Color Color { get; set; }
        }

        [Table("TestTable")]
        public class TestPlain
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public string Color { get; set; }
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

            db.Insert(new TestConverter {Color = Color.Red});

            var plain = db.Get<TestPlain>(1);

            Assert.AreEqual("255/255/0/0", plain.Color);
        }

        [Test]
        public void DataConverterSelectTest()
        {
            var db = new OrmTestSession();
            db.CreateTable<TestConverter>();

            db.Insert(new TestPlain {Color = "255/0/255/0"});

            var withC = db.Get<TestConverter>(1);

            Assert.AreEqual(Color.FromArgb(255, 0, 255, 0), withC.Color);
        }
    }
}