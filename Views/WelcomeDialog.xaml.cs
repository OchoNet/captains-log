using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CaptainsLog.Views
{
    public partial class WelcomeDialog : Window
    {
        public WelcomeDialog()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}