using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SwissKnifeApp.Services
{
    public record ClipInterval(TimeSpan Start, TimeSpan End)
    {
        public override string ToString() => $"{Start:c} - {End:c}";
    }

    public class ClipProgress
    {
        public int TotalClips { get; init; }
        public int CurrentClipIndex { get; init; } // 1-based
        public double CurrentClipPercent { get; init; } // 0..100
        public string? Message { get; init; }
    }

    public class YoutubeTxtClipDownloaderService
    {
        private string? _ytDlpPath;
        private string? _ffmpegPath;

        public async Task<List<ClipInterval>> ParseIntervalsAsync(string txtPath)
        {
            var list = new List<ClipInterval>();
            foreach (var line in await File.ReadAllLinesAsync(txtPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                // support: mm:ss HH:MM:SS; allow whitespace between
                var parts = Regex.Split(trimmed, "\\s+");
                if (parts.Length >= 2 && TryParseTimestamp(parts[0], out var s) && TryParseTimestamp(parts[1], out var e))
                {
                    if (e > s)
                        list.Add(new ClipInterval(s, e));
                }
            }
            return list;
        }

    private static bool TryParseTimestamp(string text, out TimeSpan ts)
        {
            // Accept HH:MM:SS or MM:SS
            ts = TimeSpan.Zero;
            var parts = text.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var s))
                {
                    ts = new TimeSpan(0, m, s);
                    return true;
                }
            }
            else if (parts.Length == 3)
            {
                if (int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m) && int.TryParse(parts[2], out var s))
                {
                    ts = new TimeSpan(h, m, s);
                    return true;
                }
            }
            return false;
        }

        // Public wrapper for UI validation without exposing the internal parsing logic
        public static bool TryParsePublic(string text, out TimeSpan ts) => TryParseTimestamp(text, out ts);

        public async Task DownloadSegmentsAsync(
            string youtubeUrl,
            string intervalsTxtPath,
            string outputFolder,
            IProgress<ClipProgress>? progress = null,
            IProgress<string>? log = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl)) throw new ArgumentException("URL boş", nameof(youtubeUrl));
            if (!File.Exists(intervalsTxtPath)) throw new FileNotFoundException("TXT bulunamadı", intervalsTxtPath);
            Directory.CreateDirectory(outputFolder);

            // Araç yollarını hazırla
            _ytDlpPath ??= await FindToolPath("yt-dlp", log, cancellationToken);
            _ffmpegPath ??= await FindToolPath("ffmpeg", log, cancellationToken);

            var intervals = await ParseIntervalsAsync(intervalsTxtPath);
            if (intervals.Count == 0) throw new InvalidOperationException("TXT içinde geçerli aralık bulunamadı.");

            await DownloadSegmentsAsync(youtubeUrl, intervals, outputFolder, progress, log, cancellationToken);
        }

        public async Task DownloadSegmentsAsync(
            string youtubeUrl,
            IList<ClipInterval> intervals,
            string outputFolder,
            IProgress<ClipProgress>? progress = null,
            IProgress<string>? log = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl)) throw new ArgumentException("URL boş", nameof(youtubeUrl));
            if (intervals is null || intervals.Count == 0) throw new ArgumentException("Geçerli aralık bulunamadı", nameof(intervals));
            Directory.CreateDirectory(outputFolder);

            // Araç yollarını hazırla
            _ytDlpPath ??= await FindToolPath("yt-dlp", log, cancellationToken);
            _ffmpegPath ??= await FindToolPath("ffmpeg", log, cancellationToken);

            for (int i = 0; i < intervals.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clip = intervals[i];
                var index = i + 1;
                var outName = Path.Combine(outputFolder, $"kesit_{index:00}.mp4");
                var section = $"{FormatTs(clip.Start)}-{FormatTs(clip.End)}";

                // Build yt-dlp command using download-sections with time ranges in seconds
                var args = new StringBuilder();
                args.Append("-f \"bestvideo[height<=1080]+bestaudio/best[height<=1080]\" ");
                args.Append("--merge-output-format mp4 ");
                args.Append($"--download-sections \"*{section}\" ");  // '*' prefix indicates time-based range, not chapter name
                args.Append("--force-keyframes-at-cuts ");  // Ensure clean cuts at exact timestamps
                args.Append("--no-playlist ");
                args.Append("--force-overwrites ");
                args.Append($"-o \"{outName}\" ");
                args.Append($"\"{youtubeUrl}\"");

                await RunProcessAsync(
                    fileName: _ytDlpPath!,
                    arguments: args.ToString(),
                    workingDir: outputFolder,
                    onStdOut: line =>
                    {
                        log?.Report(line);
                        var pct = ParseYtDlpProgress(line);
                        if (pct is not null)
                        {
                            progress?.Report(new ClipProgress
                            {
                                TotalClips = intervals.Count,
                                CurrentClipIndex = index,
                                CurrentClipPercent = pct.Value,
                                Message = line
                            });
                        }
                        else
                        {
                            progress?.Report(new ClipProgress
                            {
                                TotalClips = intervals.Count,
                                CurrentClipIndex = index,
                                CurrentClipPercent = 0,
                                Message = line
                            });
                        }
                    },
                    onStdErr: line => log?.Report(line),
                    cancellationToken: cancellationToken);

                // After each clip, report completion of that clip
                progress?.Report(new ClipProgress
                {
                    TotalClips = intervals.Count,
                    CurrentClipIndex = index,
                    CurrentClipPercent = 100,
                    Message = $"Clip {index}/{intervals.Count} tamamlandı."
                });
            }

            log?.Report("Tüm kesitler tamamlandı.");
        }

        private static async Task<string> FindToolPath(string toolName, IProgress<string>? log, CancellationToken ct)
        {
            var exeName = toolName + ".exe";
            
            // 1. Try PATH first (standard approach)
            try
            {
                await RunProcessAsync(toolName, "--version", Directory.GetCurrentDirectory(), _ => { }, _ => { }, ct, silent: true);
                return toolName; // Found in PATH
            }
            catch { }

            // 2. Check common locations (including portable mode)
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new[]
            {
                Path.Combine(appDir, "Tools"),  // Portable: next to exe
                @"C:\Tools",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "yt-dlp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", toolName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin")
            };

            foreach (var searchPath in searchPaths)
            {
                var fullPath = Path.Combine(searchPath, exeName);
                if (File.Exists(fullPath))
                {
                    log?.Report($"'{toolName}' bulundu: {fullPath}");
                    return fullPath;
                }
            }

            // 3. Not found anywhere
            throw new InvalidOperationException(
                $"'{toolName}' bulunamadı.\n\n" +
                $"🔧 ÇÖZÜMLER:\n\n" +
                $"1️⃣ PORTABLE KULLANIM (önerilen):\n" +
                $"   • {exeName} dosyasını şu klasöre kopyalayın:\n" +
                $"   {Path.Combine(appDir, "Tools")}\n\n" +
                $"2️⃣ SİSTEM KURULUMU:\n" +
                $"   • C:\\Tools\\{exeName} konumuna kopyalayın\n" +
                $"   • Veya PATH'e ekleyin\n\n" +
                $"3️⃣ OTOMATIK KURULUM:\n" +
                $"   • Komut: winget install {toolName}\n\n" +
                $"İndirme linkleri:\n" +
                $"  yt-dlp: github.com/yt-dlp/yt-dlp/releases\n" +
                $"  ffmpeg: github.com/BtbN/FFmpeg-Builds/releases");
        }

        private static string FormatTs(TimeSpan ts)
        {
            // yt-dlp --download-sections expects seconds as decimal (e.g., "150.5")
            return ts.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture);
        }

        private static double? ParseYtDlpProgress(string line)
        {
            // sample: [download]  12.3% of ...
            var m = Regex.Match(line, @"\[download\]\s+(?<p>\d{1,3}(?:\.\d+)?)%", RegexOptions.IgnoreCase);
            if (m.Success && double.TryParse(m.Groups["p"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                return Math.Max(0, Math.Min(100, pct));
            return null;
        }

        private static Task RunProcessAsync(
            string fileName,
            string arguments,
            string workingDir,
            Action<string> onStdOut,
            Action<string> onStdErr,
            CancellationToken cancellationToken,
            bool silent = false)
        {
            var tcs = new TaskCompletionSource<object?>();
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && !silent) onStdOut(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && !silent) onStdErr(e.Data);
            };
            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0)
                    tcs.TrySetResult(null);
                else
                    tcs.TrySetException(new Exception($"Process '{fileName}' exit code {process.ExitCode}"));
                process.Dispose();
            };

            if (!process.Start())
                throw new InvalidOperationException($"'{fileName}' başlatılamadı.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            });

            return tcs.Task;
        }
    }
}
