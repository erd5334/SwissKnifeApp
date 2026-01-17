using Microsoft.Win32;
using SwissKnifeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class YouTubeClipDownloaderPage : Page
    {
        private readonly YoutubeTxtClipDownloaderService _service = new();
        private CancellationTokenSource? _cts;
        private readonly ObservableCollection<TimeSpan> _starts = new();
        private readonly ObservableCollection<TimeSpan> _ends = new();

        public YouTubeClipDownloaderPage()
        {
            InitializeComponent();
            TxtOutput.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "SwissKnife_Downloads");
            LbStarts.ItemsSource = _starts;
            LbEnds.ItemsSource = _ends;

            RbTxt.Checked += (s, e) => { PanelTxt.Visibility = Visibility.Visible; PanelManual.Visibility = Visibility.Collapsed; };
            RbManual.Checked += (s, e) => { PanelTxt.Visibility = Visibility.Collapsed; PanelManual.Visibility = Visibility.Visible; };
            
            // Cookie panel değişimleri
            RbCookieBrowser.Checked += (s, e) => { PanelCookieBrowser.Visibility = Visibility.Visible; PanelCookieFile.Visibility = Visibility.Collapsed; };
            RbCookieFile.Checked += (s, e) => { PanelCookieBrowser.Visibility = Visibility.Collapsed; PanelCookieFile.Visibility = Visibility.Visible; };
        }

        #region UI Event Handlers
        private void BtnPickTxt_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) TxtTxtPath.Text = dialog.FileName;
        }

        private void BtnPickFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                TxtOutput.Text = dialog.FileName;
            }
        }

        private void BtnPickCookie_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) TxtCookie.Text = dialog.FileName;
        }

        private void BtnAddInterval_Click(object sender, RoutedEventArgs e)
        {
            if (YoutubeTxtClipDownloaderService.TryParsePublic(TxtStart.Text, out var s) &&
                YoutubeTxtClipDownloaderService.TryParsePublic(TxtEnd.Text, out var end))
            {
                _starts.Add(s);
                _ends.Add(end);
                TxtStart.Clear();
                TxtEnd.Clear();
            }
            else MessageBox.Show("Geçersiz zaman formatı (oo:ss veya ss:dd:ss)");
        }

        private void BtnRemoveInterval_Click(object sender, RoutedEventArgs e)
        {
            if (LbStarts.SelectedIndex >= 0)
            {
                var idx = LbStarts.SelectedIndex;
                _starts.RemoveAt(idx);
                _ends.RemoveAt(idx);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            BtnCancel.IsEnabled = false;
            TxtLog.AppendText("\n🛑 İptal ediliyor...\n");
        }

        private async void BtnInstallTools_Click(object sender, RoutedEventArgs e)
        {
            try {
                TxtLog.Clear();
                TxtLog.AppendText("🔧 Araçlar kontrol ediliyor ve güncelleniyor...\n");
                
                var toolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
                var ytdlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
                
                // yt-dlp varsa güncelle, yoksa indir
                if (File.Exists(ytdlpPath))
                {
                    TxtLog.AppendText("📥 yt-dlp güncelleniyor (nightly)...\n");
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ytdlpPath,
                        Arguments = "--update-to nightly",
                        WorkingDirectory = toolsDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var p = System.Diagnostics.Process.Start(psi);
                    if (p != null)
                    {
                        string output = await p.StandardOutput.ReadToEndAsync();
                        string error = await p.StandardError.ReadToEndAsync();
                        await p.WaitForExitAsync();
                        TxtLog.AppendText(output + "\n");
                        if (!string.IsNullOrEmpty(error)) TxtLog.AppendText(error + "\n");
                        TxtLog.AppendText("✓ yt-dlp güncellendi.\n");
                    }
                }
                else
                {
                    var installer = new ToolInstallerService();
                    await installer.InstallYtDlpAsync(new Progress<string>(s => TxtLog.AppendText(s + "\n")));
                }
                
                // ffmpeg kontrol
                var installer2 = new ToolInstallerService();
                await installer2.InstallFfmpegAsync(new Progress<string>(s => TxtLog.AppendText(s + "\n")));
                
                TxtLog.AppendText("\n✅ Tüm araçlar hazır!\n");
                MessageBox.Show("Araçlar başarıyla güncellendi/kuruldu.");
            } catch (Exception ex) { 
                TxtLog.AppendText($"❌ Hata: {ex.Message}\n");
                MessageBox.Show("Kurulum hatası: " + ex.Message); 
            }
        }
        #endregion

        #region Download Logic

        // 1. GENEL İNDİRME (Video, Playlist, Channel)
        private async void BtnStartGeneral_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtGeneralUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) { MessageBox.Show("Lütfen bir URL girin."); return; }

            await ExecuteDownload(async (progress, log, ct) =>
            {
                var quality = (YouTubeQuality)CmbQuality.SelectedIndex;
                var format = (CmbGeneralFormat.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "mp4";
                
                // Cookie ayarları
                if (RbCookieBrowser.IsChecked == true && CmbBrowser.SelectedIndex > 0)
                {
                    _service.BrowserCookie = (CmbBrowser.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower();
                    _service.CookieFilePath = null;
                }
                else if (RbCookieFile.IsChecked == true)
                {
                    _service.BrowserCookie = null;
                    _service.CookieFilePath = TxtCookie.Text;
                }
                else
                {
                    _service.BrowserCookie = null;
                    _service.CookieFilePath = null;
                }

                await _service.DownloadVideosAsync(
                    url,
                    TxtOutput.Text,
                    quality,
                    format,
                    ChkDownloadSubs.IsChecked == true,
                    ChkDownloadThumb.IsChecked == true,
                    ChkExportMetadata.IsChecked == true,
                    progress,
                    log,
                    ct);
            });
        }

        // 2. KESİT İNDİRME
        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUrl.Text)) { MessageBox.Show("URL girin."); return; }
            
            await ExecuteDownload(async (progress, log, ct) =>
            {
                _service.CookieFilePath = TxtCookie.Text;

                if (RbTxt.IsChecked == true)
                {
                    await _service.DownloadSegmentsAsync(TxtUrl.Text, TxtTxtPath.Text, TxtOutput.Text, progress: progress, log: log, cancellationToken: ct);
                }
                else
                {
                    var list = _starts.Zip(_ends).Select(x => new ClipInterval(x.First, x.Second)).ToList();
                    await _service.DownloadSegmentsAsync(TxtUrl.Text, list, TxtOutput.Text, progress: progress, log: log, cancellationToken: ct);
                }
            });
        }

        private async Task ExecuteDownload(Func<IProgress<ClipProgress>, IProgress<string>, CancellationToken, Task> action)
        {
            _cts = new CancellationTokenSource();
            SetUiState(false);
            TxtLog.Clear();
            PbTotal.Value = 0;

            var progress = new Progress<ClipProgress>(p => {
                PbTotal.Value = p.CurrentClipPercent;
                TxtProgressStatus.Text = p.Message;
            });

            var log = new Progress<string>(msg => {
                TxtLog.AppendText(msg + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });

            try
            {
                await action(progress, log, _cts.Token);
                MessageBox.Show("İşlem başarıyla tamamlandı!");
            }
            catch (OperationCanceledException)
            {
                TxtLog.AppendText("\n❌ İşlem kullanıcı tarafından iptal edildi.\n");
            }
            catch (Exception ex)
            {
                TxtLog.AppendText($"\n🔥 HATA: {ex.Message}\n");
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
            finally
            {
                SetUiState(true);
                _cts.Dispose();
                _cts = null;
            }
        }

        private void SetUiState(bool active)
        {
            BtnStartGeneral.IsEnabled = active;
            BtnStartClips.IsEnabled = active;
            BtnCancel.IsEnabled = !active;
            TxtGeneralUrl.IsEnabled = active;
            TxtUrl.IsEnabled = active;
        }

        #endregion
    }
}
