using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Mono.Data.Sqlite.Orm.Tests;
using NUnit.Framework;

namespace TestRunner.Silverlight
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                IsolatedStorageFile.GetUserStoreForApplication().Remove();
                IsolatedStorageFile.GetUserStoreForApplication().IncreaseQuotaTo(100 * 1024 * 1024);
            }
            catch
            {
            }

            ThreadPool.QueueUserWorkItem(RunTests);
        }

        private void RunTests(object state)
        {
            Type testAssembly = typeof(BooleanTest);

            Type[] types = testAssembly.Assembly.GetTypes();

            IEnumerable<Type> testFixtures = types.Where(x => x.GetCustomAttributes(typeof(TestFixtureAttribute), true).Any());
            foreach (Type testFixture in testFixtures)
            {
                object theTestFixture = Activator.CreateInstance(testFixture);

                IEnumerable<MethodInfo> tests = testFixture.GetMethods().Where(x => x.GetCustomAttributes(typeof(TestAttribute), true).Any());

                foreach (MethodInfo test in tests)
                {
                    Type fixture = testFixture;
                    MethodInfo test1 = test;

                    Dispatcher.BeginInvoke(() => label1.Content = string.Format("Testing: {0}.{1}", fixture.Name, test1.Name));

                    string testName = fixture.Name + "." + test1.Name;
                    try
                    {
                        DateTime past = DateTime.Now;
                        test.Invoke(theTestFixture, null);

                        var message = string.Format("{0} - pass: {1}", testName, (DateTime.Now - past).TotalMilliseconds);
                        Dispatcher.BeginInvoke(() => listBox1.Items.Add(fixture.Name + "." + test1.Name + message));
                    }
                    catch (Exception ex)
                    {
                        var message = string.Format("{0} - fail: {1}", testName, ex.InnerException.Message);
                        Dispatcher.BeginInvoke(() => listBox2.Items.Add(fixture.Name + "." + test1.Name + message));
                    }
                }
            }

            Dispatcher.BeginInvoke(() => label1.Content = "Complete!");
        }
    }
}