using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite.Orm.Tests;
using NUnit.Framework;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TestRunner.Metro
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void RunTests(object state)
        {
            var testAssembly = typeof(BooleanTest);
            var types = testAssembly.GetTypeInfo().Assembly.DefinedTypes;
            var testFixtures = types.Where(x => x.GetCustomAttributes(typeof(TestFixtureAttribute), true).Any());
            foreach (var testFixture in testFixtures)
            {
                var theTestFixture = Activator.CreateInstance(testFixture.AsType());
                var tests = testFixture.DeclaredMethods.Where(x => x.GetCustomAttributes(typeof(TestAttribute), true).Any());

                foreach (var test in tests)
                {
                    var fixture = testFixture;
                    var test1 = test;

                    Dispatcher.InvokeAsync(CoreDispatcherPriority.Normal,
                        delegate
                        {
                            lblCurrentTest.Text = "Testing: " + fixture.Name + "." + test1.Name;
                        }, this, null);

                    try
                    {
                        var past = DateTime.Now;
                        test.Invoke(theTestFixture, null);
                        string message = " - pass: " + (DateTime.Now - past).TotalMilliseconds;

                        Dispatcher.InvokeAsync(CoreDispatcherPriority.Normal,
                            delegate
                            {
                                lstResults.Items.Add(fixture.Name + "." + test1.Name + message);
                            }, this, null);

                    }
                    catch (Exception ex)
                    {
                        string message = " - fail: \n"+ex.InnerException.Message+Environment.NewLine;
                        Dispatcher.InvokeAsync(CoreDispatcherPriority.Normal,
                            delegate
                            {
                                lstFails.Items.Add(fixture.Name + "." + test1.Name + message);
                            }, this, null);
                    }
                }
            }
        }

        private void btnRunTests_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.RunAsync(RunTests);
        }
    }
}
