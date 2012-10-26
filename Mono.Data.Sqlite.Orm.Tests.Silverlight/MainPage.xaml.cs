namespace Mono.Data.Sqlite.Orm.Tests.Silverlight
{
    using System.IO.IsolatedStorage;
    using System.Windows;
    using System.Windows.Controls;

    using Microsoft.Silverlight.Testing;

    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnStartClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                IsolatedStorageFile.GetUserStoreForApplication().Remove();
                IsolatedStorageFile.GetUserStoreForApplication().IncreaseQuotaTo(100 * 1024 * 1024);
            }
            catch
            {
            }

            this.Content = UnitTestSystem.CreateTestPage();
        }
    }
}
