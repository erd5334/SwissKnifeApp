using System.Diagnostics;
using System.Windows;

namespace SwissKnifeApp.Views.Modules
{
    public partial class CookieHelpWindow : Window
    {
        public CookieHelpWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnOpenChromeStore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get cookies.txt LOCALLY eklentisinin Chrome Web Store linki
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Link açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenFirefoxAddons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // cookies.txt eklentisinin Firefox Add-ons linki
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Link açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
