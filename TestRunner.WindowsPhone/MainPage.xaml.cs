using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Phone.Controls;
using Mono.Data.Sqlite.Orm.Tests;
using NUnit.Framework;

namespace TestRunner.WindowsPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(RunTests);
        }

        private void RunTests(object state)
        {
            Type testAssembly = typeof (BooleanTest);

            Type[] types = testAssembly.Assembly.GetTypes();

            IEnumerable<Type> testFixtures = types.Where(x => x.GetCustomAttributes(typeof (TestFixtureAttribute), true).Any());
            foreach (Type testFixture in testFixtures)
            {
                object theTestFixture = Activator.CreateInstance(testFixture);

                IEnumerable<MethodInfo> tests = testFixture.GetMethods().Where(x => x.GetCustomAttributes(typeof (TestAttribute), true).Any());

                foreach (MethodInfo test in tests)
                {
                    Type fixture = testFixture;
                    MethodInfo test1 = test;

                    Dispatcher.BeginInvoke(() => listBox1.Items.Add("Testing: " + fixture.Name + "." + test1.Name));

                    string message = " - fail: ";
                    try
                    {
                        DateTime past = DateTime.Now;
                        test.Invoke(theTestFixture, null);
                        message = " - pass: " + (DateTime.Now - past).TotalMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        message += ex.InnerException.Message;
                    }

                    Dispatcher.BeginInvoke(() => listBox1.Items.Add(fixture.Name + "." + test1.Name + message));
                }
            }
        }
    }
}