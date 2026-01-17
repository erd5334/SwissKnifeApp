using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SwissKnifeApp.ViewModels
{
    public partial class ScreenCaptureViewModel : ObservableObject
    {
        private readonly ScreenCaptureService _captureService;

        [ObservableProperty]
        private ScreenCaptureSettings _settings = new();

        [ObservableProperty]
        private ObservableCollection<CaptureResult> _captureHistory = new();

        [ObservableProperty]
        private string _statusMessage = "Ekran görüntüsü almaya hazır";

        [ObservableProperty]
        private int _totalCaptures = 0;

        [ObservableProperty]
        private bool _isBusy = false;

        public ObservableCollection<string> ImageFormats { get; } = new() { "PNG", "JPG", "BMP", "GIF" };

        public ScreenCaptureViewModel()
        {
            _captureService = new ScreenCaptureService();
        }

        [RelayCommand]
        private async Task CaptureFullScreenAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Tam ekran görüntüsü alınıyor...";

                // Small delay for UI to hide (optional)
                await Task.Delay(100);

                var result = await Task.Run(() => _captureService.CaptureFullScreen(Settings));
                
                CaptureHistory.Insert(0, result);
                TotalCaptures++;
                StatusMessage = $"✅ Ekran görüntüsü kaydedildi: {result.FilePath}";

                if (Settings.ShowPreview)
                {
                    MessageBox.Show(
                        $"📸 Ekran görüntüsü alındı!\n\n" +
                        $"Dosya: {result.FilePath}\n" +
                        $"Boyut: {FormatFileSize(result.FileSize)}\n" +
                        $"Çözünürlük: {result.Width}x{result.Height}",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Ekran görüntüsü alınamadı!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CaptureAllScreensAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Tüm ekranlar yakalanıyor...";

                await Task.Delay(100);

                var result = await Task.Run(() => _captureService.CaptureAllScreens(Settings));

                CaptureHistory.Insert(0, result);
                TotalCaptures++;
                StatusMessage = $"✅ Tüm ekranların görüntüsü kaydedildi: {result.FilePath}";

                if (Settings.ShowPreview)
                {
                    MessageBox.Show(
                        $"📸 Tüm ekranlar yakalandı!\n\n" +
                        $"Dosya: {result.FilePath}\n" +
                        $"Boyut: {FormatFileSize(result.FileSize)}\n" +
                        $"Çözünürlük: {result.Width}x{result.Height}",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Ekran görüntüsü alınamadı!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CaptureActiveWindowAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Aktif pencere yakalanıyor...";

                // Delay to allow window to be focused
                await Task.Delay(5000);

                var result = await Task.Run(() => _captureService.CaptureActiveWindow(Settings));

                CaptureHistory.Insert(0, result);
                TotalCaptures++;
                StatusMessage = $"✅ Pencere görüntüsü kaydedildi: {result.FilePath}";

                if (Settings.ShowPreview)
                {
                    MessageBox.Show(
                        $"📸 Pencere yakalandı!\n\n" +
                        $"Dosya: {result.FilePath}\n" +
                        $"Boyut: {FormatFileSize(result.FileSize)}\n" +
                        $"Çözünürlük: {result.Width}x{result.Height}",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Pencere görüntüsü alınamadı!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void BrowseSaveDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Ekran görüntülerinin kaydedileceği klasörü seçin",
                InitialDirectory = Settings.SaveDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                Settings.SaveDirectory = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenSaveDirectory()
        {
            try
            {
                _captureService.OpenSaveDirectory(Settings.SaveDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Klasör açılamadı!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenCapturedFile(CaptureResult? capture)
        {
            if (capture == null) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = capture.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya açılamadı!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void DeleteCapture(CaptureResult? capture)
        {
            if (capture == null) return;

            var result = MessageBox.Show(
                $"Bu dosyayı silmek istediğinize emin misiniz?\n\n{capture.FilePath}",
                "Onay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    System.IO.File.Delete(capture.FilePath);
                    CaptureHistory.Remove(capture);
                    StatusMessage = "✅ Dosya silindi";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya silinemedi!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ClearHistory()
        {
            CaptureHistory.Clear();
            TotalCaptures = 0;
            StatusMessage = "Geçmiş temizlendi";
        }

        [RelayCommand]
        private void CaptureRegionSelection()
        {
            try
            {
                StatusMessage = "Alan seçimi başlatılıyor...";

                var selectionWindow = new SwissKnifeApp.Views.RegionSelectionWindow();
                selectionWindow.RegionSelected += OnRegionSelected;
                selectionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Region selection hatası!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRegionSelected(object? sender, SwissKnifeApp.Views.RegionSelectedEventArgs e)
        {
            try
            {
                if (e.Bitmap == null) return;

                // Copy to clipboard
                _captureService.CopyToClipboard(e.Bitmap);
                StatusMessage = "📋 Seçilen alan panoya kopyalandı!";

                // Save to file
                if (Settings.AutoSave)
                {
                    var result = _captureService.SaveBitmapToFile(e.Bitmap, Settings, "RegionSelection");
                    CaptureHistory.Insert(0, result);
                    TotalCaptures++;
                    StatusMessage = $"✅ Seçilen alan kaydedildi ve panoya kopyalandı: {result.FilePath}";
                }

                // Show preview
                if (Settings.ShowPreview)
                {
                    MessageBox.Show(
                        $"📸 Seçilen alan yakalandı!\n\n" +
                        $"📋 Panoya kopyalandı - Ctrl+V ile yapıştırabilirsiniz\n\n" +
                        $"Çözünürlük: {e.Width}x{e.Height}",
                        "Başarılı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                e.Bitmap.Dispose();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Region kaydetme hatası!\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
