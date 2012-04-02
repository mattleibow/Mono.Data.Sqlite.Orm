using System;
using System.Diagnostics;
using Mono.Data.Sqlite.Orm.ComponentModel;
using NUnit.Framework;

namespace Mono.Data.Sqlite.Orm.Tests
{
    [TestFixture]
    public class BooleanTest
    {
        public class Vo
        {
            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public bool Flag { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return string.Format("VO:: ID:{0} Flag:{1} Text:{2}", Id, Flag, Text);
            }
        }

        [Test]
        public void TestBoolean()
        {
            var db = new OrmTestSession();
            db.CreateTable<Vo>();

            for (int i = 0; i < 10; i++)
            {
                db.Insert(new Vo {Flag = (i%3 == 0), Text = String.Format("VO{0}", i)});
            }

            // count vo which flag is true            
            Assert.AreEqual(4, CountWithFlag(db, true));
            Assert.AreEqual(6, CountWithFlag(db, false));

            Debug.WriteLine("VO with true flag:");
            foreach (Vo vo in db.Query<Vo>("SELECT * FROM VO Where Flag = ?", true))
            {
                Debug.WriteLine(vo.ToString());
            }

            Debug.WriteLine("VO with false flag:");
            foreach (Vo vo in db.Query<Vo>("SELECT * FROM VO Where Flag = ?", false))
            {
                Debug.WriteLine(vo.ToString());
            }
        }

        public int CountWithFlag(OrmTestSession db, bool flag)
        {
            return db.ExecuteScalar<int>("SELECT COUNT(*) FROM VO Where Flag = ?", flag);
        }
    }
}