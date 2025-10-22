using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class VideoToolsPage : Page
    {
        private readonly SwissKnifeApp.Services.VideoToolsService _service = new SwissKnifeApp.Services.VideoToolsService();
        private CancellationTokenSource? _cts;
        private readonly List<string> _selectedFiles = new();

        private bool _isSliderDragging = false;
        private bool _isMediaOpened = false;
        private System.Windows.Threading.DispatcherTimer? _timer;

        public VideoToolsPage()
        {
            InitializeComponent();
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            VideoPlayer.MediaOpened += VideoPlayer_MediaOpened;
            VideoPlayer.MediaEnded += VideoPlayer_MediaEnded;
            SliderPosition.AddHandler(Slider.PreviewMouseDownEvent, new System.Windows.Input.MouseButtonEventHandler(Slider_MouseDown), true);
            SliderPosition.AddHandler(Slider.PreviewMouseUpEvent, new System.Windows.Input.MouseButtonEventHandler(Slider_MouseUp), true);
        }

        private void BtnPickFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Video Dosyaları|*.mp4;*.mkv;*.webm;*.mov;*.avi;*.flv|Tüm Dosyalar|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                _selectedFiles.Clear();
                _selectedFiles.AddRange(dlg.FileNames);
                LblSelectedCount.Text = _selectedFiles.Count.ToString();
                if (_selectedFiles.Count > 0 && string.IsNullOrWhiteSpace(TxtOutput.Text))
                {
                    TxtOutput.Text = Path.GetDirectoryName(_selectedFiles[0]) ?? "";
                }
                // Seçilen ilk videoyu oynatıcıya yükle
                if (_selectedFiles.Count > 0)
                {
                    VideoPlayer.Source = new Uri(_selectedFiles[0]);
                    VideoPlayer.Position = TimeSpan.Zero;
                    _isMediaOpened = false;
                }
            }
        }
        // Video oynatıcı kontrolleri
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Source != null)
            {
                VideoPlayer.Play();
                _timer?.Start();
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Pause();
            _timer?.Stop();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            _timer?.Stop();
            SliderPosition.Value = 0;
        }

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            if (_isMediaOpened)
            {
                var pos = VideoPlayer.Position + TimeSpan.FromSeconds(5);
                if (pos > VideoPlayer.NaturalDuration.TimeSpan)
                    pos = VideoPlayer.NaturalDuration.TimeSpan;
                VideoPlayer.Position = pos;
            }
        }

        private void BtnBackward_Click(object sender, RoutedEventArgs e)
        {
            if (_isMediaOpened)
            {
                var pos = VideoPlayer.Position - TimeSpan.FromSeconds(5);
                if (pos < TimeSpan.Zero) pos = TimeSpan.Zero;
                VideoPlayer.Position = pos;
            }
        }

        private void VideoPlayer_MediaOpened(object? sender, RoutedEventArgs e)
        {
            _isMediaOpened = true;
            SliderPosition.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            SliderPosition.Value = 0;
            TxtTotalTime.Text = FormatTime(VideoPlayer.NaturalDuration.TimeSpan);
            TxtCurrentTime.Text = "00:00";
        }

        private void VideoPlayer_MediaEnded(object? sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            SliderPosition.Value = 0;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isSliderDragging && _isMediaOpened)
            {
                SliderPosition.Value = VideoPlayer.Position.TotalSeconds;
                TxtCurrentTime.Text = FormatTime(VideoPlayer.Position);
            }
        }
        private string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"hh\:mm\:ss");
            else
                return ts.ToString(@"mm\:ss");
        }

        private void Slider_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSliderDragging = true;
        }

        private void Slider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isMediaOpened)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(SliderPosition.Value);
            }
            _isSliderDragging = false;
        }

        private void BtnPickFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtOutput.Text = dlg.SelectedPath;
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show("En az bir dosya seçin.");
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtOutput.Text))
            {
                MessageBox.Show("Çıkış klasörünü seçin.");
                return;
            }

            var mode = (CmbMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dönüştür";
            var quality = ParseQuality((CmbQuality.SelectedItem as ComboBoxItem)?.Content?.ToString());

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            BtnStart.IsEnabled = false;
            BtnCancel.IsEnabled = true;
            TxtLog.Clear();
            PbTotal.Value = 0;

            var log = new Progress<string>(s =>
            {
                TxtLog.AppendText(s + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });
            var prog = new Progress<SwissKnifeApp.Services.VideoJobProgress>(p =>
            {
                PbTotal.Value = p.CurrentFilePercent;
            });

            try
            {
                if (mode.Contains("Ses", StringComparison.OrdinalIgnoreCase))
                {
                    // Ses çıkarma modu
                    var audioFormat = ParseAudioFormat((CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    await _service.ExtractAudioAsync(_selectedFiles, TxtOutput.Text!, audioFormat, quality, prog, log, _cts.Token);
                }
                else if (mode.StartsWith("Dön", StringComparison.OrdinalIgnoreCase))
                {
                    // Video dönüştürme modu
                    var format = ParseFormat((CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    var codec = ParseCodec((CmbCodec.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    var resolution = ParseResolution((CmbResolution.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    await _service.ConvertAsync(_selectedFiles, TxtOutput.Text!, format, codec, quality, resolution, prog, log, _cts.Token);
                }
                else
                {
                    // Trim/Kırp modu
                    var format = ParseFormat((CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    var codec = ParseCodec((CmbCodec.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    var resolution = ParseResolution((CmbResolution.SelectedItem as ComboBoxItem)?.Content?.ToString());
                    
                    if (!TryParseTs(TxtStart.Text, out var start) && !string.IsNullOrWhiteSpace(TxtStart.Text))
                    {
                        MessageBox.Show("Başlangıç formatı geçersiz. Örn: 00:15 veya 01:02:03");
                        return;
                    }
                    if (!TryParseTs(TxtEnd.Text, out var end) && !string.IsNullOrWhiteSpace(TxtEnd.Text))
                    {
                        MessageBox.Show("Bitiş formatı geçersiz. Örn: 02:45 veya 01:02:59");
                        return;
                    }
                    if (start.HasValue && end.HasValue && end <= start)
                    {
                        MessageBox.Show("Bitiş, başlangıçtan büyük olmalı.");
                        return;
                    }

                    if (!SwissKnifeApp.Services.VideoToolsService.TryParseCrop(TxtCrop.Text, out var crop))
                    {
                        MessageBox.Show("Kırpma formatı geçersiz. Örn: 100,200,1280,720");
                        return;
                    }

                    foreach (var file in _selectedFiles)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var outPath = Path.Combine(TxtOutput.Text!, $"{name}_edit.{GetExt(format)}");
                        var singleProg = new Progress<double>(v => { PbTotal.Value = v; });
                        await _service.TrimCropAsync(file, outPath, format, codec, quality, resolution, start, end, crop, singleProg, log, _cts.Token);
                    }
                }

                MessageBox.Show("İşlem tamamlandı.");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("İşlem iptal edildi.");
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

        private static SwissKnifeApp.Services.VideoFormat ParseFormat(string? s)
        {
            return (s ?? "").ToUpperInvariant() switch
            {
                "MP4" => SwissKnifeApp.Services.VideoFormat.Mp4,
                "MKV" => SwissKnifeApp.Services.VideoFormat.Mkv,
                "WEBM" => SwissKnifeApp.Services.VideoFormat.WebM,
                "MOV" => SwissKnifeApp.Services.VideoFormat.Mov,
                "TS" => SwissKnifeApp.Services.VideoFormat.Ts,
                "AVI" => SwissKnifeApp.Services.VideoFormat.Avi,
                "FLV" => SwissKnifeApp.Services.VideoFormat.Flv,
                _ => SwissKnifeApp.Services.VideoFormat.Mp4
            };
        }

        private static SwissKnifeApp.Services.VideoCodec ParseCodec(string? s)
        {
            return (s ?? "").ToUpperInvariant() switch
            {
                "H264" => SwissKnifeApp.Services.VideoCodec.H264,
                "H265" => SwissKnifeApp.Services.VideoCodec.H265,
                "VP9" => SwissKnifeApp.Services.VideoCodec.Vp9,
                "COPY" => SwissKnifeApp.Services.VideoCodec.Copy,
                _ => SwissKnifeApp.Services.VideoCodec.H264
            };
        }

        // Yeni: Mode değişikliğinde UI'yi güncelle
        private void CmbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFormat == null || CmbCodec == null || LblFormat == null || LblCodec == null || LblResolution == null || CmbResolution == null) return;
            
            var mode = (CmbMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dönüştür";
            
            if (mode.Contains("Ses", StringComparison.OrdinalIgnoreCase))
            {
                // Ses çıkarma modu - Format ComboBox'ı ses formatlarıyla doldur
                CmbFormat.Items.Clear();
                CmbFormat.Items.Add(new ComboBoxItem { Content = "MP3", IsSelected = true });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "WAV" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "FLAC" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "M4A" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "OGG" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "OPUS" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "AAC" });
                CmbFormat.SelectedIndex = 0;
                
                // Codec ve Resolution'ı gizle
                LblCodec.Visibility = Visibility.Collapsed;
                CmbCodec.Visibility = Visibility.Collapsed;
                LblResolution.Visibility = Visibility.Collapsed;
                CmbResolution.Visibility = Visibility.Collapsed;
                LblFormat.Text = "Ses Formatı:";
            }
            else
            {
                // Video modu - Format ComboBox'ı video formatlarıyla doldur
                CmbFormat.Items.Clear();
                CmbFormat.Items.Add(new ComboBoxItem { Content = "MP4", IsSelected = true });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "MKV" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "WEBM" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "MOV" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "TS" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "AVI" });
                CmbFormat.Items.Add(new ComboBoxItem { Content = "FLV" });
                CmbFormat.SelectedIndex = 0;
                
                // Codec ve Resolution'ı göster
                LblCodec.Visibility = Visibility.Visible;
                CmbCodec.Visibility = Visibility.Visible;
                LblResolution.Visibility = Visibility.Visible;
                CmbResolution.Visibility = Visibility.Visible;
                LblFormat.Text = "Format:";
            }
        }

        // Yeni: Ses formatı parse
        private static SwissKnifeApp.Services.AudioFormat ParseAudioFormat(string? s)
        {
            return (s ?? "MP3").ToUpperInvariant() switch
            {
                "MP3" => SwissKnifeApp.Services.AudioFormat.Mp3,
                "WAV" => SwissKnifeApp.Services.AudioFormat.Wav,
                "FLAC" => SwissKnifeApp.Services.AudioFormat.Flac,
                "M4A" => SwissKnifeApp.Services.AudioFormat.M4a,
                "OGG" => SwissKnifeApp.Services.AudioFormat.Ogg,
                "OPUS" => SwissKnifeApp.Services.AudioFormat.Opus,
                "AAC" => SwissKnifeApp.Services.AudioFormat.Aac,
                _ => SwissKnifeApp.Services.AudioFormat.Mp3
            };
        }

        private static SwissKnifeApp.Services.VideoQuality ParseQuality(string? s)
        {
            return s switch
            {
                "Highest" => SwissKnifeApp.Services.VideoQuality.Highest,
                "High" => SwissKnifeApp.Services.VideoQuality.High,
                "Medium" => SwissKnifeApp.Services.VideoQuality.Medium,
                "Low" => SwissKnifeApp.Services.VideoQuality.Low,
                "Lossless" => SwissKnifeApp.Services.VideoQuality.Lossless,
                _ => SwissKnifeApp.Services.VideoQuality.High
            };
        }

        private static SwissKnifeApp.Services.ResolutionPreset ParseResolution(string? s)
        {
            return (s ?? "").ToLowerInvariant() switch
            {
                "original" => SwissKnifeApp.Services.ResolutionPreset.Original,
                "2160p" => SwissKnifeApp.Services.ResolutionPreset.P2160,
                "1440p" => SwissKnifeApp.Services.ResolutionPreset.P1440,
                "1080p" => SwissKnifeApp.Services.ResolutionPreset.P1080,
                "720p" => SwissKnifeApp.Services.ResolutionPreset.P720,
                "480p" => SwissKnifeApp.Services.ResolutionPreset.P480,
                _ => SwissKnifeApp.Services.ResolutionPreset.Original
            };
        }

        private static bool TryParseTs(string? text, out TimeSpan? ts)
        {
            ts = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            var parts = text.Trim().Split(':');
            try
            {
                if (parts.Length == 2)
                {
                    ts = new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1]));
                    return true;
                }
                if (parts.Length == 3)
                {
                    ts = new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static string GetExt(SwissKnifeApp.Services.VideoFormat f) => f switch
        {
            SwissKnifeApp.Services.VideoFormat.Mp4 => "mp4",
            SwissKnifeApp.Services.VideoFormat.Mkv => "mkv",
            SwissKnifeApp.Services.VideoFormat.WebM => "webm",
            SwissKnifeApp.Services.VideoFormat.Mov => "mov",
            SwissKnifeApp.Services.VideoFormat.Ts => "ts",
            SwissKnifeApp.Services.VideoFormat.Avi => "avi",
            SwissKnifeApp.Services.VideoFormat.Flv => "flv",
            _ => "mp4"
        };
    }
}
