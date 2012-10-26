using Microsoft.Phone.Controls;
using Microsoft.Silverlight.Testing;

namespace TestRunner.WindowsPhone
{
    using System.Windows;

    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                const bool RunUnitTests = true;

                if (RunUnitTests)
                {
                    var testPage = UnitTestSystem.CreateTestPage();

                    var imtp = testPage as IMobileTestPage;
                    if (imtp != null)
                    {
                        BackKeyPress += (x, xe) => xe.Cancel = imtp.NavigateBack();
                    }

                    ((PhoneApplicationFrame)Application.Current.RootVisual).Content = testPage;
                }
            };
        }
    }
}