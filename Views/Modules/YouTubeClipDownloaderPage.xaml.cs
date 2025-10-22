using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class YouTubeClipDownloaderPage : Page
    {
        private readonly SwissKnifeApp.Services.YoutubeTxtClipDownloaderService _service = new SwissKnifeApp.Services.YoutubeTxtClipDownloaderService();
        private CancellationTokenSource? _cts;

        public YouTubeClipDownloaderPage()
        {
            InitializeComponent();
            RbTxt.Checked += ModeChanged;
            RbManual.Checked += ModeChanged;
            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // İlk açılışta araçları kontrol et
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var toolsDir = Path.Combine(appDir, "Tools");
            var (ytdlp, ffmpeg) = SwissKnifeApp.Services.ToolInstallerService.CheckToolsInstalled(toolsDir);

            if (!ytdlp || !ffmpeg)
            {
                var missing = !ytdlp && !ffmpeg ? "yt-dlp ve ffmpeg" : (!ytdlp ? "yt-dlp" : "ffmpeg");
                var result = MessageBox.Show(
                    $"YouTube kesit indirici için gerekli araçlar eksik: {missing}\n\n" +
                    $"İndirme boyutu: ~130 MB\n" +
                    $"Konum: {toolsDir}\n\n" +
                    $"Şimdi indirmek ister misiniz?",
                    "Gerekli Araçlar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await InstallToolsAsync(toolsDir);
                }
                else
                {
                    TxtLog.AppendText($"⚠️ Araçlar kurulmadı. YouTube modülü çalışmayabilir.\n");
                    TxtLog.AppendText($"Manuel kurulum için 'Araçları Kur' butonunu kullanın.\n\n");
                }
            }
        }

        private void BtnPickTxt_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "TXT Dosyası|*.txt|Tüm Dosyalar|*.*" };
            if (dlg.ShowDialog() == true)
                TxtTxtPath.Text = dlg.FileName;
        }

        private void BtnPickFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtOutput.Text = dlg.SelectedPath;
        }

        private void BtnPickCookie_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Cookie Dosyası (*.txt)|*.txt|Tüm Dosyalar|*.*" };
            if (dlg.ShowDialog() == true)
                TxtCookie.Text = dlg.FileName;
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var url = TxtUrl.Text?.Trim();
            var output = TxtOutput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(output))
            {
                MessageBox.Show("URL ve Çıkış klasörü zorunludur.");
                return;
            }

            // Format seçimi
            string format = "mp4";
            string formatExt = "mp4";
            if (CmbFormat.SelectedIndex > 0)
            {
                var selectedFormat = (CmbFormat.SelectedItem as ComboBoxItem)?.Content.ToString();
                format = selectedFormat?.ToLower() switch
                {
                    "mp3" => "mp3",
                    "wav" => "wav",
                    "flac" => "flac",
                    "m4a" => "m4a",
                    "ogg" => "ogg",
                    "opus" => "opus",
                    _ => "mp4"
                };
                formatExt = format;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            BtnStart.IsEnabled = false;
            BtnCancel.IsEnabled = true;
            TxtLog.Clear();
            PbPart.Value = 0; PbTotal.Value = 0;

            // Cookie dosyası set et (opsiyonel)
            var cookiePath = TxtCookie.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(cookiePath) && File.Exists(cookiePath))
            {
                _service.CookieFilePath = cookiePath;
            }
            else
            {
                _service.CookieFilePath = null;
            }

            IProgress<string> log = new Progress<string>(s =>
            {
                TxtLog.AppendText(s + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });
            IProgress<SwissKnifeApp.Services.ClipProgress> prog = new Progress<SwissKnifeApp.Services.ClipProgress>(p =>
            {
                if (p.TotalClips > 0)
                    PbTotal.Value = Math.Min(100, (double)(p.CurrentClipIndex - 1) / p.TotalClips * 100.0 + p.CurrentClipPercent / p.TotalClips);
                PbPart.Value = p.CurrentClipPercent;
            });

            try
            {
                if (RbTxt.IsChecked == true)
                {
                    var txtPath = TxtTxtPath.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(txtPath))
                    {
                        MessageBox.Show("TXT dosyası seçin veya Manuel modunu kullanın.");
                        return;
                    }
                    await _service.DownloadSegmentsAsync(url!, txtPath!, output!, format, formatExt, prog, log, _cts.Token);
                }
                else
                {
                    var list = new System.Collections.Generic.List<SwissKnifeApp.Services.ClipInterval>();
                    var count = Math.Min(LbStarts.Items.Count, LbEnds.Items.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var s = LbStarts.Items[i]?.ToString() ?? string.Empty;
                        var e2 = LbEnds.Items[i]?.ToString() ?? string.Empty;
                        if (SwissKnifeApp.Services.YoutubeTxtClipDownloaderService.TryParsePublic(s, out var ts1) &&
                            SwissKnifeApp.Services.YoutubeTxtClipDownloaderService.TryParsePublic(e2, out var ts2) && ts2 > ts1)
                        {
                            list.Add(new SwissKnifeApp.Services.ClipInterval(ts1, ts2));
                        }
                    }
                    if (list.Count == 0)
                    {
                        MessageBox.Show("Lütfen en az bir geçerli aralık ekleyin.");
                        return;
                    }
                    await _service.DownloadSegmentsAsync(url!, list, output!, format, formatExt, prog, log, _cts.Token);
                }
                MessageBox.Show("İndirme tamamlandı.");
            }
            catch (OperationCanceledException)
            {
                log.Report("İşlem iptal edildi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
            finally
            {
                BtnStart.IsEnabled = true;
                BtnCancel.IsEnabled = false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (RbTxt.IsChecked == true)
            {
                PanelTxt.Visibility = Visibility.Visible;
                PanelManual.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelTxt.Visibility = Visibility.Collapsed;
                PanelManual.Visibility = Visibility.Visible;
            }
        }

        private void BtnAddInterval_Click(object sender, RoutedEventArgs e)
        {
            var s = (TxtStart.Text ?? string.Empty).Trim();
            var e2 = (TxtEnd.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s) || string.IsNullOrWhiteSpace(e2)) return;
            LbStarts.Items.Add(s);
            LbEnds.Items.Add(e2);
            TxtStart.Clear();
            TxtEnd.Clear();
        }

        private void BtnRemoveInterval_Click(object sender, RoutedEventArgs e)
        {
            var i = LbStarts.SelectedIndex;
            if (i < 0 || i >= LbStarts.Items.Count) i = LbEnds.SelectedIndex;
            if (i >= 0 && i < LbStarts.Items.Count && i < LbEnds.Items.Count)
            {
                LbStarts.Items.RemoveAt(i);
                LbEnds.Items.RemoveAt(i);
            }
        }

        private async void BtnInstallTools_Click(object sender, RoutedEventArgs e)
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var toolsDir = Path.Combine(appDir, "Tools");
            await InstallToolsAsync(toolsDir);
        }

        private async Task InstallToolsAsync(string toolsDir)
        {
            var installer = new SwissKnifeApp.Services.ToolInstallerService();
            
            BtnStart.IsEnabled = false;
            BtnInstallTools.IsEnabled = false;
            TxtLog.Clear();
            PbTotal.Value = 0;
            TxtLog.AppendText("Araçlar indiriliyor...\n\n");

            var log = new Progress<string>(s =>
            {
                TxtLog.AppendText(s + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });

            var progress = new Progress<double>(p =>
            {
                PbTotal.Value = p;
            });

            try
            {
                var success = await installer.EnsureToolsInstalledAsync(toolsDir, log, progress, CancellationToken.None);
                
                if (success)
                {
                    MessageBox.Show(
                        $"Araçlar başarıyla kuruldu!\n\nKonum: {toolsDir}\n\nArtık YouTube kesit indiricisini kullanabilirsiniz.",
                        "Kurulum Tamamlandı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Araçlar kurulamadı. İnternet bağlantınızı kontrol edin veya manuel kurulum yapın.",
                        "Hata",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kurulum hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnStart.IsEnabled = true;
                BtnInstallTools.IsEnabled = true;
                PbTotal.Value = 0;
            }
        }

        private void BtnCookieHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new CookieHelpWindow
            {
                Owner = Window.GetWindow(this)
            };
            helpWindow.ShowDialog();
        }
    }
}
