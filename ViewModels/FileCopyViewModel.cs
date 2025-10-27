using SwissKnifeApp.Models;
using SwissKnifeApp.Services;
using SwissKnifeApp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace SwissKnifeApp.ViewModels;

public class FileCopyViewModel : INotifyPropertyChanged
{
    private readonly IFolderDialogService _folderDialog;
    private readonly ICopyService _copyService;
    private readonly IConfigService _configService;

    public FileCopyViewModel() : this(new FolderDialogService(), new CopyService(), new ConfigService()) { }

    public FileCopyViewModel(IFolderDialogService folderDialog, ICopyService copyService, IConfigService configService)
    {
        _folderDialog = folderDialog;
        _copyService = copyService;
        _configService = configService;

        BrowseSourceCommand = new RelayCommand(BrowseSource);
        BrowseTargetCommand = new RelayCommand(BrowseTarget);
        StartCommand = new RelayCommand(async () => await StartAsync(), () => CanStart);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        LoadListCommand = new RelayCommand(async () => await LoadListAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(SourceFolder));
        CopySelectedCommand = new RelayCommand(async () => await CopySelectedAsync(), () => !IsBusy && HasSelection && !string.IsNullOrWhiteSpace(TargetFolder));

        // Initialize collection view for filtering
        FilesView = CollectionViewSource.GetDefaultView(Files);
        FilesView.Filter = FilterFiles;

        // defaults
        WorkerCount = Environment.ProcessorCount;
        Overwrite = false;

        // Load config
        var cfg = _configService.Load();
        RememberLast = cfg.RememberLast;
        if (RememberLast)
        {
            if (!string.IsNullOrWhiteSpace(cfg.LastSource)) SourceFolder = cfg.LastSource;
            if (!string.IsNullOrWhiteSpace(cfg.LastTarget)) TargetFolder = cfg.LastTarget;
        }
        ImageJpg = true; ImageJpeg = true; ImagePng = true; ImageBmp = true;
    }

    private string? _sourceFolder;
    public string? SourceFolder { get => _sourceFolder; set { if (Set(ref _sourceFolder, value)) RefreshCanExecutes(); } }

    private string? _targetFolder;
    public string? TargetFolder { get => _targetFolder; set { if (Set(ref _targetFolder, value)) RefreshCanExecutes(); } }

    private int _workerCount;
    public int WorkerCount { get => _workerCount; set { Set(ref _workerCount, value); } }

    private bool _overwrite;
    public bool Overwrite { get => _overwrite; set { Set(ref _overwrite, value); } }

    private bool _rememberLast;
    public bool RememberLast { get => _rememberLast; set { if (Set(ref _rememberLast, value)) SaveConfig(); } }

    // Extensions
    private bool _allFiles;
    public bool AllFiles { get => _allFiles; set { if (Set(ref _allFiles, value)) OnAllFilesToggled(); } }
    public bool ImageJpg { get => _imageJpg; set { if (Set(ref _imageJpg, value)) RefreshCanExecutes(); } }
    public bool ImageJpeg { get => _imageJpeg; set { if (Set(ref _imageJpeg, value)) RefreshCanExecutes(); } }
    public bool ImagePng { get => _imagePng; set { if (Set(ref _imagePng, value)) RefreshCanExecutes(); } }
    public bool ImageBmp { get => _imageBmp; set { if (Set(ref _imageBmp, value)) RefreshCanExecutes(); } }
    public bool VideoMp4 { get => _videoMp4; set { if (Set(ref _videoMp4, value)) RefreshCanExecutes(); } }
    public bool AudioMp3 { get => _audioMp3; set { if (Set(ref _audioMp3, value)) RefreshCanExecutes(); } }
    public bool AnyExe { get => _anyExe; set { if (Set(ref _anyExe, value)) RefreshCanExecutes(); } }

    private bool _imageJpg, _imageJpeg, _imagePng, _imageBmp, _videoMp4, _audioMp3, _anyExe;

    public ObservableCollection<string> LogLines { get; } = new();
    public ObservableCollection<string> ErrorLines { get; } = new();
    public ObservableCollection<FileListItem> Files { get; } = new();
    public ICollectionView FilesView { get; private set; }

    private bool _hasSelection;
    public bool HasSelection { get => _hasSelection; set { if (Set(ref _hasSelection, value)) RefreshCanExecutes(); } }

    private string _fileFilter = "";
    public string FileFilter
    {
        get => _fileFilter;
        set
        {
            if (Set(ref _fileFilter, value))
            {
                FilesView?.Refresh();
            }
        }
    }

    private long _totalFiles;
    public long TotalFiles { get => _totalFiles; set { Set(ref _totalFiles, value); } }

    private long _totalBytes;
    public long TotalBytes { get => _totalBytes; set { Set(ref _totalBytes, value); } }

    private string _totalBytesHuman = "0 B";
    public string TotalBytesHuman { get => _totalBytesHuman; set { Set(ref _totalBytesHuman, value); } }

    private long _copiedFiles;
    public long CopiedFiles { get => _copiedFiles; set { Set(ref _copiedFiles, value); } }

    private long _copiedBytes;
    public long CopiedBytes { get => _copiedBytes; set { if (Set(ref _copiedBytes, value)) CopiedBytesHuman = FormatBytes(value); } }

    private string _copiedBytesHuman = "0 B";
    public string CopiedBytesHuman { get => _copiedBytesHuman; set { Set(ref _copiedBytesHuman, value); } }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set { if (Set(ref _isBusy, value)) RefreshCanExecutes(); } }

    public ICommand BrowseSourceCommand { get; }
    public ICommand BrowseTargetCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand LoadListCommand { get; }
    public ICommand CopySelectedCommand { get; }

    private CancellationTokenSource? _cts;

    private void BrowseSource()
    {
        var chosen = _folderDialog.PickFolder(SourceFolder);
        if (!string.IsNullOrWhiteSpace(chosen)) { SourceFolder = chosen; SaveConfig(); }
    }

    private void BrowseTarget()
    {
        var chosen = _folderDialog.PickFolder(TargetFolder);
        if (!string.IsNullOrWhiteSpace(chosen)) { TargetFolder = chosen; SaveConfig(); }
    }

    public bool CanStart => !IsBusy && !string.IsNullOrWhiteSpace(SourceFolder) && !string.IsNullOrWhiteSpace(TargetFolder);

    private async Task StartAsync()
    {
        if (!CanStart) return;
        IsBusy = true;
        LogLines.Clear(); ErrorLines.Clear();
        CopiedFiles = 0; CopiedBytes = 0; TotalFiles = 0; TotalBytes = 0;

        _cts = new CancellationTokenSource();

        try
        {
            // Build list of extensions
            var exts = BuildExtensions();

            // Validate target not under source
            try
            {
                var srcFull = Path.GetFullPath(SourceFolder!);
                var dstFull = Path.GetFullPath(TargetFolder!);
                if (dstFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog("Hedef klasör, kaynak klasörün içinde olamaz.");
                    return;
                }
            }
            catch { }

            // Count
            var countTask = _copyService.CountAsync(SourceFolder!, exts, _cts.Token);
            var (files, bytes) = await countTask;
            TotalFiles = files; TotalBytes = bytes; TotalBytesHuman = FormatBytes(bytes);
            AppendLog($"Toplam: {files} dosya, {FormatBytes(bytes)}");

            // Enumerate items preserving structure
            var items = EnumerateItems(SourceFolder!, TargetFolder!, exts);

            // Copy
            await _copyService.CopyAsync(
                items,
                Math.Max(1, WorkerCount),
                Overwrite,
                TimeSpan.FromSeconds(15),
                onProgress: (copied, copiedBytes, path) =>
                {
                    CopiedFiles = copied;
                    CopiedBytes = copiedBytes;
                },
                onLog: AppendLog,
                onError: (path, ex) => AppendError($"{path} -> {ex.Message}"),
                ct: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Ýptal edildi.");
        }
        catch (Exception ex)
        {
            AppendError(ex.Message);
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            SaveConfig();
        }
    }

    private IEnumerable<string> BuildExtensions()
    {
        if (AllFiles) return new[] { "*.*" };
        var list = new List<string>();
        if (ImageJpg) list.Add(".jpg");
        if (ImageJpeg) list.Add(".jpeg");
        if (ImagePng) list.Add(".png");
        if (ImageBmp) list.Add(".bmp");
        if (VideoMp4) list.Add(".mp4");
        if (AudioMp3) list.Add(".mp3");
        if (AnyExe) list.Add(".exe");
        if (list.Count == 0) list.AddRange(new[] { ".jpg", ".jpeg", ".png", ".bmp" });
        return list;
    }

    private static IEnumerable<CopyItem> EnumerateItems(string source, string target, IEnumerable<string> exts)
    {
        HashSet<string>? set = exts.Any(x => x == "*.*") ? null : exts.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()).ToHashSet();
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (set is not null)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!set.Contains(ext)) continue;
            }
            var rel = Path.GetRelativePath(source, file);
            var dest = Path.Combine(target, rel);
            yield return new CopyItem(file, dest);
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    private void OnAllFilesToggled()
    {
        // Deðerleri deðiþtirmiyoruz; sadece XAML tarafýnda IsEnabled ile pasifleþtiriyoruz.
        RefreshCanExecutes();
    }

    private void AppendLog(string message)
    {
        App.Current?.Dispatcher.Invoke(() => LogLines.Add(message));
    }

    private void AppendError(string message)
    {
        App.Current?.Dispatcher.Invoke(() => ErrorLines.Add(message));
    }

    private void SaveConfig()
    {
        var cfg = new Models.AppConfig
        {
            RememberLast = this.RememberLast,
            LastSource = this.RememberLast ? this.SourceFolder : null,
            LastTarget = this.RememberLast ? this.TargetFolder : null
        };
        _configService.Save(cfg);
    }

    private void RefreshCanExecutes()
    {
        (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadListCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CopySelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private async Task LoadListAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceFolder)) return;
        try
        {
            IsBusy = true;
            Files.Clear();
            var exts = BuildExtensions();
            var set = exts.Any(x => x == "*.*") ? null : exts.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()).ToHashSet();
            await Task.Run(() =>
            {
                foreach (var file in Directory.EnumerateFiles(SourceFolder!, "*", SearchOption.AllDirectories))
                {
                    if (set is not null)
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (!set.Contains(ext)) continue;
                    }
                    try
                    {
                        var info = new FileInfo(file);
                        var rel = Path.GetRelativePath(SourceFolder!, file);
                        var human = FormatBytes(info.Length);
                        var item = new FileListItem(file, rel, info.Length, human);
                        item.PropertyChanged += OnItemPropertyChanged;
                        App.Current?.Dispatcher.Invoke(() => Files.Add(item));
                    }
                    catch { }
                }
            });
            UpdateSelectionState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CopySelectedAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetFolder)) return;
        var selected = Files.Where(f => f.Selected).ToList();
        if (selected.Count == 0) return;

        try
        {
            var srcFull = Path.GetFullPath(SourceFolder!);
            var dstFull = Path.GetFullPath(TargetFolder!);
            if (dstFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog("Hedef klasör, kaynak klasörün içinde olamaz.");
                return;
            }
        }
        catch { }

        _cts = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            var items = selected.Select(s => new CopyItem(s.FullPath, Path.Combine(TargetFolder!, s.RelativePath)));
            long totalBytes = selected.Sum(s => s.Size);
            TotalFiles = selected.Count;
            TotalBytes = totalBytes;
            TotalBytesHuman = FormatBytes(totalBytes);

            await _copyService.CopyAsync(
                items,
                Math.Max(1, WorkerCount),
                Overwrite,
                TimeSpan.FromSeconds(15),
                onProgress: (copied, copiedBytes, path) =>
                {
                    CopiedFiles = copied;
                    CopiedBytes = copiedBytes;
                },
                onLog: AppendLog,
                onError: (path, ex) => AppendError($"{path} -> {ex.Message}"),
                ct: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("Ýptal edildi.");
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileListItem.Selected))
        {
            UpdateSelectionState();
        }
    }

    private void UpdateSelectionState()
    {
        HasSelection = Files.Any(f => f.Selected);
    }

    private bool FilterFiles(object item)
    {
        if (item is not FileListItem file) return false;
        if (string.IsNullOrWhiteSpace(FileFilter)) return true;

        return file.RelativePath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase) ||
               file.FullPath.Contains(FileFilter, StringComparison.OrdinalIgnoreCase);
    }
}
