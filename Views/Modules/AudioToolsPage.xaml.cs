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
    public partial class AudioToolsPage : Page
    {
        private readonly SwissKnifeApp.Services.AudioToolsService _service = new SwissKnifeApp.Services.AudioToolsService();
        private CancellationTokenSource? _cts;
        private readonly List<string> _selectedFiles = new();

        public AudioToolsPage()
        {
            InitializeComponent();
        }

        private void BtnPickFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Ses Dosyaları|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.ogg;*.opus|Tüm Dosyalar|*.*",
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
            }
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
            var format = ParseFormat((CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString());
            var quality = ParseQuality((CmbQuality.SelectedItem as ComboBoxItem)?.Content?.ToString());
            var normalize = ChkNormalize.IsChecked == true;

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
            var prog = new Progress<SwissKnifeApp.Services.AudioJobProgress>(p =>
            {
                PbTotal.Value = p.CurrentFilePercent;
            });

            try
            {
                if (mode.StartsWith("Dön", StringComparison.OrdinalIgnoreCase))
                {
                    await _service.ConvertAsync(_selectedFiles, TxtOutput.Text!, format, quality, normalize, prog, log, _cts.Token);
                }
                else
                {
                    // Trim modunda birden fazla dosyayı aynı aralıklarla işler ve aynı klasöre yazar
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

                    foreach (var file in _selectedFiles)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var outPath = Path.Combine(TxtOutput.Text!, $"{name}_trim.{GetExt(format)}");
                        var singleProg = new Progress<double>(v => { PbTotal.Value = v; });
                        await _service.TrimAsync(file, outPath, start, end, format, quality, normalize, singleProg, log, _cts.Token);
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

        private static SwissKnifeApp.Services.AudioFormat ParseFormat(string? s)
        {
            return s?.ToUpperInvariant() switch
            {
                "MP3" => SwissKnifeApp.Services.AudioFormat.Mp3,
                "AAC" => SwissKnifeApp.Services.AudioFormat.Aac,
                "WAV" => SwissKnifeApp.Services.AudioFormat.Wav,
                "FLAC" => SwissKnifeApp.Services.AudioFormat.Flac,
                "OPUS" => SwissKnifeApp.Services.AudioFormat.Opus,
                _ => SwissKnifeApp.Services.AudioFormat.Mp3
            };
        }

        private static SwissKnifeApp.Services.QualityPreset ParseQuality(string? s)
        {
            return s switch
            {
                "Highest" => SwissKnifeApp.Services.QualityPreset.Highest,
                "High" => SwissKnifeApp.Services.QualityPreset.High,
                "Medium" => SwissKnifeApp.Services.QualityPreset.Medium,
                "Low" => SwissKnifeApp.Services.QualityPreset.Low,
                "Lossless" => SwissKnifeApp.Services.QualityPreset.Lossless,
                _ => SwissKnifeApp.Services.QualityPreset.High
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

        private static string GetExt(SwissKnifeApp.Services.AudioFormat f) => f switch
        {
            SwissKnifeApp.Services.AudioFormat.Mp3 => "mp3",
            SwissKnifeApp.Services.AudioFormat.Aac => "m4a",
            SwissKnifeApp.Services.AudioFormat.Wav => "wav",
            SwissKnifeApp.Services.AudioFormat.Flac => "flac",
            SwissKnifeApp.Services.AudioFormat.Opus => "opus",
            _ => "mp3"
        };
    }
}
