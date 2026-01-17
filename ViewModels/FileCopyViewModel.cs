using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SwissKnifeApp.ViewModels
{
    public partial class FileCopyViewModel : ObservableObject
    {
        private readonly ICopyService _copyService;
        private CancellationTokenSource? _cts;

        [ObservableProperty] private string _sourceFolder = string.Empty;
        [ObservableProperty] private string _targetFolder = string.Empty;
        
        // Protocol & Credentials
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(IsNetworkProtocol))]
        private string _protocol = "Local"; // Local, FTP, FTPS, SFTP
        
        public bool IsNetworkProtocol => Protocol != "Local";

        [ObservableProperty] private string _ftpUser = string.Empty;
        [ObservableProperty] private string _ftpPassword = string.Empty;
        [ObservableProperty] private string _ftpHost = string.Empty;
        [ObservableProperty] private int _ftpPort = 0; // 0 = default

        public ObservableCollection<string> Protocols { get; } = new() { "Local", "FTP", "FTPS", "SFTP" };

        // Filters
        [ObservableProperty] private bool _allFiles = false;
        [ObservableProperty] private bool _imageJpg = true;
        [ObservableProperty] private bool _imageJpeg = true;
        [ObservableProperty] private bool _imagePng = true;
        [ObservableProperty] private bool _imageBmp = true;
        [ObservableProperty] private bool _videoMp4 = false;
        [ObservableProperty] private bool _audioMp3 = false;
        [ObservableProperty] private bool _anyExe = false;

        // Options
        [ObservableProperty] private int _workerCount = 4;
        [ObservableProperty] private bool _overwrite = false;
        [ObservableProperty] private bool _rememberLast = true;

        // Progress
        [ObservableProperty] private long _totalFiles;
        [ObservableProperty] private long _copiedFiles;
        [ObservableProperty] private long _totalBytes;
        [ObservableProperty] private long _copiedBytes;
        [ObservableProperty] private string _totalBytesHuman = "0 B";
        [ObservableProperty] private string _copiedBytesHuman = "0 B";
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Hazır";

        // Logs
        public ObservableCollection<string> LogLines { get; } = new();
        public ObservableCollection<string> ErrorLines { get; } = new();

        // Manual Selection
        [ObservableProperty] private string _fileFilter = string.Empty;
        public ObservableCollection<FileListItem> Files { get; } = new();
        public CollectionViewSource FilesViewSource { get; } = new();
        public System.ComponentModel.ICollectionView FilesView => FilesViewSource.View;

        public FileCopyViewModel()
        {
            _copyService = new CopyService();
            FilesViewSource.Source = Files;
            FilesViewSource.Filter += OnFilesFilter;
        }

        private void OnFilesFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileFilter))
            {
                e.Accepted = true;
                return;
            }
            if (e.Item is FileListItem item)
            {
                e.Accepted = item.RelativePath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase) ||
                             item.FullPath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase);
            }
        }

        partial void OnFileFilterChanged(string value)
        {
            FilesView.Refresh();
        }

        [RelayCommand]
        private void BrowseSource()
        {
            var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SourceFolder = dlg.FileName;
            }
        }

        [RelayCommand]
        private void BrowseTarget()
        {
            var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TargetFolder = dlg.FileName;
            }
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            if (Protocol == "Local")
            {
                Log("Yerel protokol seçili, test gerekmez.");
                return;
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                Log("Hata: Host adresi giriniz.");
                return;
            }

            var opts = new ConnectionOptions
            {
                Protocol = Protocol,
                Host = FtpHost,
                Port = FtpPort,
                Username = FtpUser,
                Password = FtpPassword
            };

            Log($"Bağlantı test ediliyor ({Protocol})...");
            bool success = await _copyService.TestConnectionAsync(opts, Log);
            if (success) MessageBox.Show("Bağlantı Başarılı!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show("Bağlantı Başarısız!", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        [RelayCommand]
        private async Task Start()
        {
            if (IsBusy) return;
            if (string.IsNullOrWhiteSpace(SourceFolder) || string.IsNullOrWhiteSpace(TargetFolder))
            {
                MessageBox.Show("Kaynak ve Hedef klasörleri seçiniz.");
                return;
            }

            IsBusy = true;
            LogLines.Clear();
            ErrorLines.Clear();
            _cts = new CancellationTokenSource();

            try
            {
                var exts = GetSelectedExtensions();
                Log("Dosyalar sayılıyor...");
                var (count, bytes) = await _copyService.CountAsync(SourceFolder, exts, _cts.Token);
                TotalFiles = count;
                TotalBytes = bytes;
                TotalBytesHuman = FormatBytes(bytes);
                CopiedFiles = 0;
                CopiedBytes = 0;
                CopiedBytesHuman = "0 B";

                Log($"Toplam {count} dosya, {TotalBytesHuman} kopyalanacak.");

                var items = new List<CopyItem>();
                
                await Task.Run(() =>
                {
                    var extSet = exts.Any(x => x == "*.*") ? null : new HashSet<string>(exts.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()));
                    foreach (var file in Directory.EnumerateFiles(SourceFolder, "*", SearchOption.AllDirectories))
                    {
                        if (extSet != null && !extSet.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                        
                        string rel = Path.GetRelativePath(SourceFolder, file);
                        string targetPath;
                        if (Protocol == "Local")
                        {
                            targetPath = Path.Combine(TargetFolder, rel);
                        }
                        else
                        {
                            targetPath = Path.Combine(TargetFolder, rel).Replace("\\", "/"); 
                        }
                        items.Add(new CopyItem(file, targetPath));
                    }
                });

                var connOpts = new ConnectionOptions
                {
                    Protocol = Protocol,
                    Host = FtpHost,
                    Port = FtpPort,
                    Username = FtpUser,
                    Password = FtpPassword
                };

                await _copyService.CopyAsync(
                    items,
                    connOpts,
                    WorkerCount,
                    Overwrite,
                    TimeSpan.FromSeconds(15),
                    OnProgress,
                    Log,
                    OnError,
                    _cts.Token);
            }
            catch (OperationCanceledException)
            {
                Log("İşlem iptal edildi.");
            }
            catch (Exception ex)
            {
                Log($"Hata: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _cts = null;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
        }

        [RelayCommand]
        private async Task LoadList()
        {
            if (string.IsNullOrWhiteSpace(SourceFolder)) return;
            IsBusy = true;
            Files.Clear();
            try
            {
                var exts = GetSelectedExtensions();
                await Task.Run(() =>
                {
                    var extSet = exts.Any(x => x == "*.*") ? null : new HashSet<string>(exts.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()));
                    foreach (var file in Directory.EnumerateFiles(SourceFolder, "*", SearchOption.AllDirectories))
                    {
                        if (extSet != null && !extSet.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                        var fi = new FileInfo(file);
                        var rel = Path.GetRelativePath(SourceFolder, file);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Files.Add(new FileListItem(file, rel, fi.Length, FormatBytes(fi.Length)) { Selected = true });
                        });
                    }
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CopySelected()
        {
            var selected = Files.Where(x => x.Selected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Lütfen dosya seçiniz.");
                return;
            }

            IsBusy = true;
            _cts = new CancellationTokenSource();
            LogLines.Clear();

            try
            {
                TotalFiles = selected.Count;
                TotalBytes = selected.Sum(x => x.Size);
                TotalBytesHuman = FormatBytes(TotalBytes);
                CopiedFiles = 0;
                CopiedBytes = 0;

                var items = selected.Select(x => 
                {
                    string targetPath;
                    if (Protocol == "Local")
                    {
                        targetPath = Path.Combine(TargetFolder, x.RelativePath);
                    }
                    else
                    {
                        targetPath = Path.Combine(TargetFolder, x.RelativePath).Replace("\\", "/");
                    }
                    return new CopyItem(x.FullPath, targetPath);
                }).ToList();

                var connOpts = new ConnectionOptions
                {
                    Protocol = Protocol,
                    Host = FtpHost,
                    Port = FtpPort,
                    Username = FtpUser,
                    Password = FtpPassword
                };

                await _copyService.CopyAsync(
                    items,
                    connOpts,
                    WorkerCount,
                    Overwrite,
                    TimeSpan.FromSeconds(15),
                    OnProgress,
                    Log,
                    OnError,
                    _cts.Token);
            }
            catch (OperationCanceledException)
            {
                Log("İptal edildi.");
            }
            finally
            {
                IsBusy = false;
                _cts = null;
            }
        }

        [RelayCommand]
        private async Task ShowMissingFiles()
        {
            if (Protocol != "Local")
            {
                MessageBox.Show("Eksik dosya kontrolü sadece Yerel protokolde çalışır.");
                return;
            }
            if (string.IsNullOrWhiteSpace(SourceFolder) || string.IsNullOrWhiteSpace(TargetFolder)) return;

            IsBusy = true;
            Files.Clear();
            try
            {
                await Task.Run(() =>
                {
                    var exts = GetSelectedExtensions();
                    var extSet = exts.Any(x => x == "*.*") ? null : new HashSet<string>(exts.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()));

                    var targetFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (Directory.Exists(TargetFolder))
                    {
                        foreach (var f in Directory.EnumerateFiles(TargetFolder, "*", SearchOption.AllDirectories))
                        {
                            targetFiles.Add(Path.GetRelativePath(TargetFolder, f));
                        }
                    }

                    foreach (var file in Directory.EnumerateFiles(SourceFolder, "*", SearchOption.AllDirectories))
                    {
                        if (extSet != null && !extSet.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                        
                        var rel = Path.GetRelativePath(SourceFolder, file);
                        if (!targetFiles.Contains(rel))
                        {
                            var fi = new FileInfo(file);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Files.Add(new FileListItem(file, rel, fi.Length, FormatBytes(fi.Length)) { Selected = true });
                            });
                        }
                    }
                });
                MessageBox.Show($"Hedefte olmayan {Files.Count} dosya bulundu.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private IEnumerable<string> GetSelectedExtensions()
        {
            var list = new List<string>();
            if (AllFiles) { list.Add("*.*"); return list; }
            if (ImageJpg) list.Add(".jpg");
            if (ImageJpeg) list.Add(".jpeg");
            if (ImagePng) list.Add(".png");
            if (ImageBmp) list.Add(".bmp");
            if (VideoMp4) list.Add(".mp4");
            if (AudioMp3) list.Add(".mp3");
            if (AnyExe) list.Add(".exe");
            return list;
        }

        private void OnProgress(long files, long bytes, string currentFile)
        {
            CopiedFiles = files;
            CopiedBytes = bytes;
            CopiedBytesHuman = FormatBytes(bytes);
            StatusMessage = $"Kopyalanıyor: {Path.GetFileName(currentFile)}";
        }

        private void Log(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogLines.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (LogLines.Count > 1000) LogLines.RemoveAt(LogLines.Count - 1);
            });
        }

        private void OnError(string file, Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorLines.Add($"{file}: {ex.Message}");
            });
        }

        private static string FormatBytes(long bytes)
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
