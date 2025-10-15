using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SpeedTest.Net;
using SpeedTest.Net.Enums;
using SpeedTest.Net.Models;

namespace SwissKnifeApp.Views.Modules
{
    public partial class SpeedTestPage : Page
    {
    private readonly ObservableCollection<SpeedTestResult> _history = new();
    private Server? _server; // cached server (nullable)

        public SpeedTestPage()
        {
            InitializeComponent();
            lvHistory.ItemsSource = _history;
        }

        private async void btnStartTest_Click(object sender, RoutedEventArgs e)
        {
            btnStartTest.IsEnabled = false;
            txtPing.Text = "Ping: ölçülüyor...";
            txtDownload.Text = "İndirme: ölçülüyor...";
            txtUpload.Text = "Yükleme: (uygulanmadı)"; // Upload henüz implement edilmedi

            try
            {
                await ExecuteSpeedTestAsync(SpeedTestSource.Fast);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnStartTest.IsEnabled = true;
            }
        }

        private async void GetConnectionSpeed(object sender, RoutedEventArgs e)
        {
            if (_server == null)
            {
                MessageBox.Show("Önce sunucuyu getir (Fetch Server)");
                return;
            }
            await ExecuteSpeedTestAsync(SpeedTestSource.Speedtest, _server);
        }

        private async void GetConnectionSpeedUsingFast(object sender, RoutedEventArgs e)
        {
            await ExecuteSpeedTestAsync(SpeedTestSource.Fast);
        }

        private async void GetConnectionSpeedLocal(object sender, RoutedEventArgs e)
        {
            await ExecuteSpeedTestAsync(SpeedTestSource.Speedtest);
        }

        private async void FetchServer(object sender, RoutedEventArgs e)
        {
            try
            {
                _server = await SpeedTestClient.GetServer();
                MessageBox.Show($"Server Fetched: {_server.Host} ({_server.Id})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucu alınamadı: {ex.Message}");
            }
        }

        private async Task ExecuteSpeedTestAsync(SpeedTestSource source, Server? server = null)
        {
            if (source == SpeedTestSource.Speedtest && server == null)
            {
                try
                {
                    server = _server ?? await SpeedTestClient.GetServer();
                    _server = server; // cache
                }
                catch (Exception ex)
                {
                    txtDownload.Text = $"Sunucu hatası: {ex.Message}";
                    return;
                }
            }

            DownloadSpeed? download = null;
            try
            {
                if (source == SpeedTestSource.Speedtest)
                {
                    download = await SpeedTestClient.GetDownloadSpeed(server, SpeedTestUnit.KiloBitsPerSecond);
                }
                else
                {
                    download = await FastClient.GetDownloadSpeed(SpeedTestUnit.KiloBitsPerSecond);
                }
            }
            catch (Exception ex)
            {
                txtDownload.Text = $"İndirme hatası: {ex.Message}";
                return;
            }

            if (download == null)
            {
                txtDownload.Text = "İndirme: ölçülemedi";
                return;
            }
            server = _server ?? await SpeedTestClient.GetServer();

            double ping = 0;
            if (server != null)
            {
                ping = server.Latitude; // placeholder
                txtPing.Text = $"Ping: {ping:F0} ms ({server.Id})";
            }
            else
            {
                txtPing.Text = "Ping: N/A";
            }

            double downloadMbps = download.Speed;
            if (download.Unit == SpeedTestUnit.MegaBytesPerSecond.ToString())
            {
                downloadMbps = download.Speed / 1000.0; // kbps -> Mbps
            }
            txtDownload.Text = $"İndirme: {downloadMbps:F2} Mbps";

            _history.Insert(0, new SpeedTestResult
            {
                Tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                Ping = ping,
                Download = Math.Round(downloadMbps, 2)
            });

            if (_history.Count > 20)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    public class SpeedTestResult
    {
        public string Tarih { get; set; } = string.Empty;
        public double Ping { get; set; }
        public double Download { get; set; }
        public double Upload { get; set; }
    }
}