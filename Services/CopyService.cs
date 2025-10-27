using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Services;

public class CopyService : ICopyService
{
    private static readonly string[] DefaultImageExts = new[] { ".jpg", ".jpeg", ".png", ".bmp" };

    private static bool Matches(string path, HashSet<string>? extsOrNullMeansAll)
    {
        if (extsOrNullMeansAll is null) return true;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return extsOrNullMeansAll.Contains(ext);
    }

    public async Task<(long totalFiles, long totalBytes)> CountAsync(string sourceRoot, IEnumerable<string> extensionsOrAll, CancellationToken ct)
    {
        HashSet<string>? exts = BuildExtSet(extensionsOrAll);
        long files = 0;
        long bytes = 0;
        await Task.Run(() =>
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                if (!Matches(file, exts)) continue;
                try
                {
                    var info = new FileInfo(file);
                    files++;
                    bytes += info.Length;
                }
                catch { /* skip */ }
            }
        }, ct);
        return (files, bytes);
    }

    private static HashSet<string>? BuildExtSet(IEnumerable<string> extensionsOrAll)
    {
        var list = extensionsOrAll?.ToArray() ?? Array.Empty<string>();
        if (list.Length == 0) return new(DefaultImageExts);
        if (list.Any(x => x == "*.*")) return null; // null means all files
        return new(list.Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant()));
    }

    public async Task CopyAsync(
        IEnumerable<CopyItem> items,
        int maxDegreeOfParallelism,
        bool overwrite,
        TimeSpan retryWindow,
        Action<long, long, string>? onProgress,
        Action<string>? onLog,
        Action<string, Exception>? onError,
        CancellationToken ct)
    {
        long copied = 0;
        long copiedBytes = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var options = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism)
        };

        var exceptions = new ConcurrentBag<(string file, Exception ex)>();

        await Task.Run(() =>
        {
            Parallel.ForEach(items, options, item =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                try
                {
                    CopyWithRetry(item, overwrite, retryWindow, options.CancellationToken, onLog);
                    Interlocked.Increment(ref copied);
                    try
                    {
                        var fi = new FileInfo(item.TargetPath);
                        var len = fi.Length;
                        Interlocked.Add(ref copiedBytes, len);
                        var fileName = Path.GetFileName(item.TargetPath);
                        onLog?.Invoke($"Kopyalandı: {fileName} ({FormatBytes(len)})");
                    }
                    catch { }
                    onProgress?.Invoke(copied, copiedBytes, item.TargetPath);
                }
                catch (OperationCanceledException)
                {
                    // bubble up
                    throw;
                }
                catch (Exception ex)
                {
                    exceptions.Add((item.SourcePath, ex));
                    onError?.Invoke(item.SourcePath, ex);
                }
            });
        }, ct);

        sw.Stop();
        onLog?.Invoke($"Tamamlandı: {copied} dosya, {FormatBytes(copiedBytes)} kopyalandı. Süre: {sw.Elapsed}.");

        if (!exceptions.IsEmpty)
        {
            onLog?.Invoke($"Hata sayısı: {exceptions.Count}");
        }
    }

    private static void CopyWithRetry(CopyItem item, bool overwrite, TimeSpan retryWindow, CancellationToken ct, Action<string>? onLog)
    {
        var start = DateTime.UtcNow;
        var dir = Path.GetDirectoryName(item.TargetPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var destExists = File.Exists(item.TargetPath);
                if (destExists && !overwrite)
                {
                    // Skip if no overwrite
                    onLog?.Invoke($"Atlandı (mevcut): {item.TargetPath}");
                    return;
                }

                // Use CopyTo for metadata preserve similar to copy2
                File.Copy(item.SourcePath, item.TargetPath, overwrite);
                // Preserve timestamps
                try
                {
                    var srcInfo = new FileInfo(item.SourcePath);
                    File.SetCreationTime(item.TargetPath, srcInfo.CreationTime);
                    File.SetLastWriteTime(item.TargetPath, srcInfo.LastWriteTime);
                }
                catch { }
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                if (DateTime.UtcNow - start > retryWindow)
                    throw; // Give up

                // Sleep in small slices to be cancellation responsive
                var until = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                while (DateTime.UtcNow < until)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
            }
        }
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
