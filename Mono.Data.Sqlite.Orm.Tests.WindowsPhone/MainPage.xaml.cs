using Microsoft.Phone.Controls;
using Microsoft.Phone.Testing;

namespace TestRunner.WindowsPhone
{
    using System.Windows;

    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            this.Content = UnitTestSystem.CreateTestPage();
        }
    }
}