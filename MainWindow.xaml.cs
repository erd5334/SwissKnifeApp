using MahApps.Metro.Controls;
using SwissKnifeApp.Views.Modules;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwissKnifeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool isMenuExpanded = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isMenuExpanded)
            {
                // Menüyü daralt
                MenuColumn.Width = new GridLength(50);
                isMenuExpanded = false;
            }
            else
            {
                // Menüyü genişlet
                MenuColumn.Width = new GridLength(250);
                isMenuExpanded = true;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                WelcomeGrid.Visibility = Visibility.Collapsed;

                switch (tag)
                {
                    case "TextOperations":
                        MainFrame.Navigate(new TextOperationsPage());
                        break;
                    case "UnitConverter":
                        MainFrame.Navigate(new UnitConverterPage());
                        break;
                    case "PdfOperations":
                            MainFrame.Navigate(new PdfOperationsPage());
                        break;
                    case "PasswordTools":
                        MainFrame.Navigate(new PasswordToolsPage());
                        break;
                    case "QrCode":
                        MainFrame.Navigate(new QrBarcodeToolsPage());
                        break;
                    case "SpeedTest":
                        MainFrame.Navigate(new SpeedTestPage());
                        break;
                    case "ClipboardHistory":
                        MainFrame.Navigate(new ClipboardHistoryPage());
                        break;
                    case "JsonXmlFormatter":
                        MainFrame.Navigate(new JsonXmlFormatterPage());
                        break;
                    case "ImageConverter":
                        MainFrame.Navigate(new ImageConverterPage());
                        break;
                    case "MoneyToText":
                        MainFrame.Navigate(new MoneyToTextPage());
                        break;
                    case "FileManager":
                        MainFrame.Navigate(new FileManagerPage());
                        break;
                    case "DataAnalysis":
                        MainFrame.Navigate(new DataAnalysisPage());
                        break;

                    default:
                        MessageBox.Show($"'{tag}' modülü henüz eklenmedi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
        }
    }
}