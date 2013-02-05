using Mono.Data.Sqlite.Orm.ComponentModel;
#if SILVERLIGHT || MS_TEST|| MS_TEST
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
    public class HooksTest
    {
        private const string InsertedTest = "Inserted Test";

        private const string ReplacedText = "Replaced Text";

        public class HookTestTable
        {
            [AutoIncrement]
            [PrimaryKey]
            public int Id { get; set; }

            public string Text { get; set; }
        }

        [Test]
        public void CreateInstanceHookTest()
        {
            var db = new OrmTestSession();
            db.InstanceCreated += InstanceCreated;
            db.CreateTable<HookTestTable>();
            db.Insert(new HookTestTable { Text = InsertedTest });
            var got = db.Get<HookTestTable>(1);
            Assert.AreEqual(ReplacedText, got.Text);
        }

        [Test]
        public void CreateInstanceHookTestNonGeneric()
        {
            var db = new OrmTestSession();
            db.InstanceCreated += InstanceCreated;
            db.CreateTable<HookTestTable>();
            db.Insert(new HookTestTable { Text = InsertedTest });
            var got = db.Get(typeof(HookTestTable), 1);
            Assert.AreEqual(ReplacedText, ((HookTestTable)got).Text);
        }

        private void InstanceCreated(object sender, InstanceCreatedEventArgs e)
        {
            var created = (HookTestTable)e.Instance;
            Assert.AreEqual(InsertedTest, created.Text);
            created.Text = ReplacedText;
        }
    }
}