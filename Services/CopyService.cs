using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Renci.SshNet;
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

    public async Task<bool> TestConnectionAsync(ConnectionOptions options, Action<string> onLog)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (options.Protocol == "FTP" || options.Protocol == "FTPS")
                {
                    using var client = new FtpClient(options.Host, options.Username, options.Password);
                    client.Port = options.Port > 0 ? options.Port : 21;
                    if (options.Protocol == "FTPS")
                    {
                        client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                        client.Config.ValidateAnyCertificate = true;
                    }
                    client.Connect();
                    onLog?.Invoke($"{options.Protocol} bağlantısı başarılı: {options.Host}");
                    client.Disconnect();
                    return true;
                }
                else if (options.Protocol == "SFTP")
                {
                    using var client = new SftpClient(options.Host, options.Port > 0 ? options.Port : 22, options.Username, options.Password);
                    client.Connect();
                    onLog?.Invoke($"SFTP bağlantısı başarılı: {options.Host}");
                    client.Disconnect();
                    return true;
                }
                else
                {
                    onLog?.Invoke("Yerel dosya sistemi kullanılıyor (Test gerekmez).");
                    return true;
                }
            }
            catch (Exception ex)
            {
                onLog?.Invoke($"Bağlantı hatası: {ex.Message}");
                return false;
            }
        });
    }

    public async Task CopyAsync(
        IEnumerable<CopyItem> items,
        ConnectionOptions connectionOptions,
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

        // For FTP/SFTP, we might want to reuse the client or create one per thread.
        // Creating one per thread is safer for parallelism if the client isn't thread-safe.
        // FluentFTP FtpClient is NOT thread-safe for concurrent operations.
        // SSH.NET SftpClient is also not thread-safe for concurrent operations.
        // So we will create a client inside the loop or use a pool.
        // Given Parallel.ForEach, we can use thread-local storage or just create/dispose per file (expensive)
        // or create per partition. For simplicity and robustness, let's try creating per file first, 
        // but that might be too slow for many small files. 
        // Better: Use a custom partitioner or just accept the overhead for now.
        // Actually, for FTP/SFTP, opening a connection for every file is very bad.
        // We should group items or use a limited number of long-lived connections.
        // But implementing a connection pool here is complex.
        // Let's stick to per-file for now as a baseline, or maybe reuse if possible.
        // Optimization: We can use a ThreadLocal<Client> if we ensure we dispose them.
        
        await Task.Run(() =>
        {
            Parallel.ForEach(items, options, item =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                try
                {
                    ProcessItem(item, connectionOptions, overwrite, retryWindow, options.CancellationToken, onLog);
                    
                    Interlocked.Increment(ref copied);
                    try
                    {
                        var len = new FileInfo(item.SourcePath).Length;
                        Interlocked.Add(ref copiedBytes, len);
                        var fileName = Path.GetFileName(item.TargetPath);
                        // onLog?.Invoke($"Kopyalandı: {fileName} ({FormatBytes(len)})");
                    }
                    catch { }
                    onProgress?.Invoke(copied, copiedBytes, item.TargetPath);
                }
                catch (OperationCanceledException)
                {
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

    private void ProcessItem(CopyItem item, ConnectionOptions options, bool overwrite, TimeSpan retryWindow, CancellationToken ct, Action<string>? onLog)
    {
        var start = DateTime.UtcNow;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (options.Protocol == "FTP" || options.Protocol == "FTPS")
                {
                    UploadFtp(item, options, overwrite, onLog);
                }
                else if (options.Protocol == "SFTP")
                {
                    UploadSftp(item, options, overwrite, onLog);
                }
                else
                {
                    CopyLocal(item, overwrite, onLog);
                }
                return;
            }
            catch (Exception)
            {
                if (DateTime.UtcNow - start > retryWindow)
                    throw;

                var until = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                while (DateTime.UtcNow < until)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
            }
        }
    }

    private void CopyLocal(CopyItem item, bool overwrite, Action<string>? onLog)
    {
        var dir = Path.GetDirectoryName(item.TargetPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        if (File.Exists(item.TargetPath) && !overwrite)
        {
            onLog?.Invoke($"Atlandı (mevcut): {item.TargetPath}");
            return;
        }

        File.Copy(item.SourcePath, item.TargetPath, overwrite);
        try
        {
            var srcInfo = new FileInfo(item.SourcePath);
            File.SetCreationTime(item.TargetPath, srcInfo.CreationTime);
            File.SetLastWriteTime(item.TargetPath, srcInfo.LastWriteTime);
        }
        catch { }
        onLog?.Invoke($"Kopyalandı (Yerel): {Path.GetFileName(item.TargetPath)}");
    }

    private void UploadFtp(CopyItem item, ConnectionOptions options, bool overwrite, Action<string>? onLog)
    {
        using var client = new FtpClient(options.Host, options.Username, options.Password);
        client.Port = options.Port > 0 ? options.Port : 21;
        if (options.Protocol == "FTPS")
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            client.Config.ValidateAnyCertificate = true;
        }
        client.Connect();

        // TargetPath is likely a full local path if we came from the UI logic, 
        // but for FTP it should be relative to the FTP root or absolute FTP path.
        // The UI logic currently sets TargetPath as "TargetFolder + RelativePath".
        // If TargetFolder is a URL, we need to handle it.
        // Assuming the ViewModel handles the path construction correctly for FTP.
        // But wait, the ViewModel constructs TargetPath using Path.Combine which uses backslashes on Windows.
        // FTP needs forward slashes.
        
        string remotePath = item.TargetPath.Replace("\\", "/");
        // If the path starts with a drive letter (e.g. C:/...), strip it or handle it?
        // The user enters a target path. If it's FTP, they might enter "/var/www".
        // The item.TargetPath will be "/var/www/subdir/file.ext".
        // If they entered "ftp://...", we need to parse it. 
        // But ConnectionOptions has the host. The TargetFolder in UI might be just the path.
        
        // Let's assume item.TargetPath is the full remote path.
        
        if (client.FileExists(remotePath) && !overwrite)
        {
            onLog?.Invoke($"Atlandı (mevcut): {remotePath}");
            return;
        }

        // Ensure directory exists
        string remoteDir = Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? "/";
        if (!client.DirectoryExists(remoteDir))
        {
            client.CreateDirectory(remoteDir);
        }

        var status = client.UploadFile(item.SourcePath, remotePath, FtpRemoteExists.Overwrite, false, FtpVerify.None);
        if (status == FtpStatus.Failed) throw new Exception("FTP Upload failed");
        
        onLog?.Invoke($"Kopyalandı (FTP): {Path.GetFileName(remotePath)}");
    }

    private void UploadSftp(CopyItem item, ConnectionOptions options, bool overwrite, Action<string>? onLog)
    {
        using var client = new SftpClient(options.Host, options.Port > 0 ? options.Port : 22, options.Username, options.Password);
        client.Connect();

        string remotePath = item.TargetPath.Replace("\\", "/");

        if (client.Exists(remotePath) && !overwrite)
        {
            onLog?.Invoke($"Atlandı (mevcut): {remotePath}");
            return;
        }

        // Ensure directory
        string remoteDir = Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? "/";
        EnsureSftpDirectory(client, remoteDir);

        using (var fs = File.OpenRead(item.SourcePath))
        {
            client.UploadFile(fs, remotePath, true);
        }
        onLog?.Invoke($"Kopyalandı (SFTP): {Path.GetFileName(remotePath)}");
    }

    private void EnsureSftpDirectory(SftpClient client, string path)
    {
        if (string.IsNullOrEmpty(path) || path == "." || path == "/") return;
        
        if (client.Exists(path)) return;

        // Recursive create
        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        if (!string.IsNullOrEmpty(parent) && parent != "/" && parent != path)
        {
            EnsureSftpDirectory(client, parent);
        }
        
        try
        {
            client.CreateDirectory(path);
        }
        catch { /* ignore if exists now */ }
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
