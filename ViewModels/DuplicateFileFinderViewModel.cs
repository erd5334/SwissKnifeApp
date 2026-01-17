using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SwissKnifeApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace SwissKnifeApp.ViewModels
{
    public partial class DuplicateFileFinderViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchPath = "";

        [ObservableProperty]
        private bool _isScanning = false;

        [ObservableProperty]
        private bool _includeSubfolders = true;

        [ObservableProperty]
        private string _selectedHashAlgorithm = "MD5";

        [ObservableProperty]
        private bool _useSizeComparison = false;

        [ObservableProperty]
        private string _filePattern = "*.*";

        [ObservableProperty]
        private int _scannedFiles = 0;

        [ObservableProperty]
        private int _duplicateGroups = 0;

        [ObservableProperty]
        private long _totalWastedSpace = 0;

        [ObservableProperty]
        private string _totalWastedSpaceFormatted = "0 B";

        [ObservableProperty]
        private string _statusMessage = "Tarama başlatmak için klasör seçin";

        [ObservableProperty]
        private ObservableCollection<DuplicateGroup> _duplicateGroupsList = new();

        public ObservableCollection<string> HashAlgorithms { get; } = new() { "MD5", "SHA256", "SHA1" };

        public DuplicateFileFinderViewModel()
        {
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Taranacak Klasörü Seçin"
            };

            if (dialog.ShowDialog() == true)
            {
                SearchPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private async Task ScanForDuplicatesAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchPath) || !Directory.Exists(SearchPath))
            {
                MessageBox.Show("Lütfen geçerli bir klasör seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsScanning = true;
            ScannedFiles = 0;
            DuplicateGroups = 0;
            TotalWastedSpace = 0;
            DuplicateGroupsList.Clear();
            StatusMessage = "Dosyalar taranıyor...";

            try
            {
                await Task.Run(async () =>
                {
                    // Get all files
                    var searchOption = IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var files = Directory.GetFiles(SearchPath, FilePattern, searchOption);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ScannedFiles = files.Length;
                        StatusMessage = $"{files.Length} dosya bulundu, hash hesaplanıyor...";
                    });

                    // Group by size first (optimization)
                    var filesBySize = new Dictionary<long, List<string>>();
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            var size = fileInfo.Length;

                            if (!filesBySize.ContainsKey(size))
                                filesBySize[size] = new List<string>();

                            filesBySize[size].Add(file);
                        }
                        catch { }
                    }

                    // Only calculate hash for files with same size
                    var duplicates = new Dictionary<string, List<DuplicateFileInfo>>();
                    int processed = 0;

                    foreach (var sizeGroup in filesBySize.Where(g => g.Value.Count > 1))
                    {
                        foreach (var file in sizeGroup.Value)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                string key;

                                if (UseSizeComparison)
                                {
                                    // Size + Name comparison
                                    key = $"{fileInfo.Length}_{Path.GetFileName(file)}";
                                }
                                else
                                {
                                    // Hash comparison
                                    key = await CalculateFileHashAsync(file, SelectedHashAlgorithm);
                                }

                                if (!duplicates.ContainsKey(key))
                                    duplicates[key] = new List<DuplicateFileInfo>();

                                duplicates[key].Add(new DuplicateFileInfo
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileSize = fileInfo.Length,
                                    FileSizeFormatted = FormatFileSize(fileInfo.Length),
                                    Hash = key,
                                    LastModified = fileInfo.LastWriteTime,
                                    GroupKey = key
                                });

                                processed++;
                                if (processed % 10 == 0)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        StatusMessage = $"{processed}/{files.Length} dosya işlendi...";
                                    });
                                }
                            }
                            catch { }
                        }
                    }

                    // Filter only duplicates (count > 1)
                    var duplicateGroups = duplicates.Where(g => g.Value.Count > 1).ToList();

                    // Create groups
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DuplicateGroupsList.Clear();
                        long totalWasted = 0;

                        foreach (var group in duplicateGroups)
                        {
                            var firstFile = group.Value.First();
                            long wastedSpace = firstFile.FileSize * (group.Value.Count - 1);
                            totalWasted += wastedSpace;

                            var duplicateGroup = new DuplicateGroup
                            {
                                GroupKey = group.Key,
                                RepresentativeFile = firstFile.FileName,
                                FileSize = firstFile.FileSize,
                                FileSizeFormatted = firstFile.FileSizeFormatted,
                                Count = group.Value.Count,
                                TotalWastedSpace = wastedSpace,
                                TotalWastedSpaceFormatted = FormatFileSize(wastedSpace),
                                Files = new ObservableCollection<DuplicateFileInfo>(group.Value)
                            };

                            DuplicateGroupsList.Add(duplicateGroup);
                        }

                        DuplicateGroups = duplicateGroups.Count;
                        TotalWastedSpace = totalWasted;
                        TotalWastedSpaceFormatted = FormatFileSize(totalWasted);
                        StatusMessage = $"Tarama tamamlandı! {DuplicateGroups} grup, {FormatFileSize(totalWasted)} alan kazancı";
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tarama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        [RelayCommand]
        private void DeleteSelected()
        {
            var selectedFiles = DuplicateGroupsList
                .SelectMany(g => g.Files)
                .Where(f => f.IsSelected)
                .ToList();

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Silinecek dosya seçilmedi!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"{selectedFiles.Count} dosya silinecek. Emin misiniz?",
                "Onay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            int deleted = 0;
            long freedSpace = 0;

            foreach (var file in selectedFiles)
            {
                try
                {
                    var size = file.FileSize;
                    File.Delete(file.FilePath);
                    deleted++;
                    freedSpace += size;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya silinemedi: {file.FileName}\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show(
                $"{deleted} dosya silindi.\n{FormatFileSize(freedSpace)} alan boşaltıldı.",
                "Başarılı",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Refresh
            ScanForDuplicatesAsync();
        }

        [RelayCommand]
        private void ClearResults()
        {
            DuplicateGroupsList.Clear();
            DuplicateGroups = 0;
            TotalWastedSpace = 0;
            TotalWastedSpaceFormatted = "0 B";
            ScannedFiles = 0;
            StatusMessage = "Sonuçlar temizlendi";
        }

        private async Task<string> CalculateFileHashAsync(string filePath, string algorithm)
        {
            using var stream = File.OpenRead(filePath);
            return await Task.Run(() =>
            {
                byte[] hash = algorithm switch
                {
                    "MD5" => MD5.HashData(stream),
                    "SHA256" => SHA256.HashData(stream),
                    "SHA1" => SHA1.HashData(stream),
                    _ => MD5.HashData(stream)
                };

                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            });
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
