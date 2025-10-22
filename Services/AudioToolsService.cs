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
    public enum AudioFormat { Mp3, Aac, Wav, Flac, Opus, M4a, Ogg }
    public enum QualityPreset { Highest, High, Medium, Low, Lossless }

    public class AudioJobProgress
    {
        public int TotalFiles { get; init; }
        public int CurrentFileIndex { get; init; } // 1-based
        public double CurrentFilePercent { get; init; } // 0..100
        public string? Message { get; init; }
    }

    public class AudioToolsService
    {
        private string? _ffmpegPath;
        private string? _ffprobePath;

        public async Task ConvertAsync(
            IList<string> inputFiles,
            string outputFolder,
            AudioFormat format,
            QualityPreset quality,
            bool normalize,
            IProgress<AudioJobProgress>? progress = null,
            IProgress<string>? log = null,
            CancellationToken cancellationToken = default)
        {
            if (inputFiles is null || inputFiles.Count == 0)
                throw new ArgumentException("En az bir dosya seçin", nameof(inputFiles));

            Directory.CreateDirectory(outputFolder);

            await EnsureFfmpegAsync(log, cancellationToken);

            for (int i = 0; i < inputFiles.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = inputFiles[i];
                if (!File.Exists(input))
                    throw new FileNotFoundException("Girdi dosyası bulunamadı", input);

                var outPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(input) + "." + GetExtension(format));

                await ConvertSingleAsync(
                    input,
                    outPath,
                    format,
                    quality,
                    normalize,
                    new Progress<double>(p =>
                    {
                        progress?.Report(new AudioJobProgress
                        {
                            TotalFiles = inputFiles.Count,
                            CurrentFileIndex = i + 1,
                            CurrentFilePercent = p,
                        });
                    }),
                    log,
                    cancellationToken);

                progress?.Report(new AudioJobProgress
                {
                    TotalFiles = inputFiles.Count,
                    CurrentFileIndex = i + 1,
                    CurrentFilePercent = 100,
                    Message = $"{Path.GetFileName(outPath)} tamamlandı"
                });
            }
        }

        public async Task TrimAsync(
            string inputFile,
            string outputFile,
            TimeSpan? start,
            TimeSpan? end,
            AudioFormat format,
            QualityPreset quality,
            bool normalize,
            IProgress<double>? fileProgress = null,
            IProgress<string>? log = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(inputFile)) throw new FileNotFoundException("Girdi dosyası bulunamadı", inputFile);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
            await EnsureFfmpegAsync(log, cancellationToken);

            await ConvertOrTrimInternalAsync(inputFile, outputFile, format, quality, normalize, start, end, fileProgress, log, cancellationToken);
        }

        private async Task ConvertSingleAsync(
            string input,
            string output,
            AudioFormat format,
            QualityPreset quality,
            bool normalize,
            IProgress<double>? fileProgress,
            IProgress<string>? log,
            CancellationToken ct)
        {
            await ConvertOrTrimInternalAsync(input, output, format, quality, normalize, null, null, fileProgress, log, ct);
        }

        private async Task ConvertOrTrimInternalAsync(
            string input,
            string output,
            AudioFormat format,
            QualityPreset quality,
            bool normalize,
            TimeSpan? start,
            TimeSpan? end,
            IProgress<double>? fileProgress,
            IProgress<string>? log,
            CancellationToken ct)
        {
            double? duration = await ProbeDurationSecondsAsync(input, ct);

            var args = new StringBuilder();
            args.Append("-y ");
            if (start.HasValue) args.Append($"-ss {FormatTs(start.Value)} ");
            args.Append($"-i \"{input}\" ");
            if (end.HasValue)
            {
                if (start.HasValue)
                {
                    var dur = end.Value - start.Value;
                    if (dur.TotalSeconds > 0) args.Append($"-t {FormatTs(dur)} ");
                }
                else
                {
                    args.Append($"-to {FormatTs(end.Value)} ");
                }
            }
            args.Append("-vn "); // no video

            // codec and quality
            switch (format)
            {
                case AudioFormat.Mp3:
                    args.Append("-c:a libmp3lame ");
                    args.Append($"-b:a {GetBitrateKbps(quality, defaultFor: 320)}k ");
                    break;
                case AudioFormat.Aac:
                    args.Append("-c:a aac ");
                    args.Append($"-b:a {GetBitrateKbps(quality, defaultFor: 256)}k ");
                    break;
                case AudioFormat.Opus:
                    args.Append("-c:a libopus ");
                    args.Append($"-b:a {GetBitrateKbps(quality, defaultFor: 192)}k ");
                    break;
                case AudioFormat.Flac:
                    args.Append("-c:a flac ");
                    break;
                case AudioFormat.Wav:
                    args.Append("-c:a pcm_s16le ");
                    break;
            }

            if (normalize)
            {
                args.Append("-af loudnorm ");
            }

            args.Append($"\"{output}\"");

            await RunProcessAsync(
                fileName: _ffmpegPath!,
                arguments: args.ToString(),
                workingDir: Path.GetDirectoryName(output)!,
                onStdOut: line => { if (!string.IsNullOrWhiteSpace(line)) log?.Report(line); },
                onStdErr: line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        log?.Report(line);
                        var p = TryParseFfmpegProgress(line, duration);
                        if (p.HasValue)
                            fileProgress?.Report(Math.Max(0, Math.Min(100, p.Value)));
                    }
                },
                cancellationToken: ct);
        }

        private static int GetBitrateKbps(QualityPreset preset, int defaultFor)
        {
            return preset switch
            {
                QualityPreset.Highest => Math.Max(defaultFor, 320),
                QualityPreset.High => Math.Min(defaultFor, 256),
                QualityPreset.Medium => 192,
                QualityPreset.Low => 128,
                QualityPreset.Lossless => defaultFor, // ignored for lossless codecs
                _ => defaultFor
            };
        }

        private static string GetExtension(AudioFormat format) => format switch
        {
            AudioFormat.Mp3 => "mp3",
            AudioFormat.Aac => "m4a",
            AudioFormat.Wav => "wav",
            AudioFormat.Flac => "flac",
            AudioFormat.Opus => "opus",
            _ => "mp3"
        };

        private static string FormatTs(TimeSpan ts)
        {
            return ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        }

        private double? TryParseFfmpegProgress(string line, double? totalSeconds)
        {
            if (totalSeconds is null || totalSeconds <= 0) return null;
            var m = Regex.Match(line, @"time=(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})[\.](?<ms>\d+)");
            if (m.Success)
            {
                var h = int.Parse(m.Groups["h"].Value);
                var mi = int.Parse(m.Groups["m"].Value);
                var s = int.Parse(m.Groups["s"].Value);
                var ms = int.Parse(m.Groups["ms"].Value);
                var seconds = h * 3600 + mi * 60 + s + (ms / 100.0);
                return seconds / totalSeconds.Value * 100.0;
            }
            return null;
        }

        private async Task<double?> ProbeDurationSecondsAsync(string input, CancellationToken ct)
        {
            await EnsureFfmpegAsync(null, ct);
            // prefer ffprobe if available
            await EnsureFfprobeAsync(ct);
            if (_ffprobePath != null)
            {
                try
                {
                    var output = await RunProcessCaptureAsync(_ffprobePath, $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{input}\"", Path.GetDirectoryName(input)!, ct);
                    if (double.TryParse(output.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var sec))
                        return sec;
                }
                catch { }
            }

            // fallback: parse ffmpeg -i
            try
            {
                var info = await RunProcessCaptureAsync(_ffmpegPath!, $"-i \"{input}\"", Path.GetDirectoryName(input)!, ct, captureStdErr: true);
                var m = Regex.Match(info, @"Duration: (?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})[\.,](?<ms>\d+)");
                if (m.Success)
                {
                    var h = int.Parse(m.Groups["h"].Value);
                    var mi = int.Parse(m.Groups["m"].Value);
                    var s = int.Parse(m.Groups["s"].Value);
                    var ms = int.Parse(m.Groups["ms"].Value);
                    return h * 3600 + mi * 60 + s + (ms / 100.0);
                }
            }
            catch { }

            return null;
        }

        private async Task EnsureFfmpegAsync(IProgress<string>? log, CancellationToken ct)
        {
            if (_ffmpegPath is not null) return;
            _ffmpegPath = await FindToolPath("ffmpeg", log, ct);
        }

        private async Task EnsureFfprobeAsync(CancellationToken ct)
        {
            if (_ffprobePath is not null) return;
            try
            {
                _ffprobePath = await FindToolPath("ffprobe", null, ct);
            }
            catch
            {
                _ffprobePath = null;
            }
        }

        private static async Task<string> FindToolPath(string toolName, IProgress<string>? log, CancellationToken ct)
        {
            var exe = toolName + ".exe";
            // 1) PATH
            try
            {
                await RunProcessAsync(toolName, "-version", Directory.GetCurrentDirectory(), _ => { }, _ => { }, ct, silent: true);
                return toolName;
            }
            catch { }

            // 2) common dirs
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new[]
            {
                Path.Combine(appDir, "Tools"),
                @"C:\\Tools",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), toolName),
            };
            foreach (var dir in searchPaths)
            {
                var full = Path.Combine(dir, exe);
                if (File.Exists(full))
                {
                    log?.Report($"'{toolName}' bulundu: {full}");
                    return full;
                }
            }

            throw new InvalidOperationException($"'{toolName}' bulunamadı. 'Araçları Kur' ile indirebilir veya [APPDIR]\\Tools içine ekleyebilirsiniz.");
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
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data) && !silent) onStdOut(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data) && !silent) onStdErr(e.Data); };
            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0) tcs.TrySetResult(null);
                else tcs.TrySetException(new Exception($"Process '{fileName}' exit code {process.ExitCode}"));
                process.Dispose();
            };

            if (!process.Start()) throw new InvalidOperationException($"'{fileName}' başlatılamadı.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } });

            return tcs.Task;
        }

        private static async Task<string> RunProcessCaptureAsync(
            string fileName,
            string arguments,
            string workingDir,
            CancellationToken cancellationToken,
            bool captureStdErr = false)
        {
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

            using var p = new Process { StartInfo = psi };
            var sb = new StringBuilder();
            p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) sb.AppendLine(e.Data); };
            p.ErrorDataReceived += (s, e) => { if (captureStdErr && !string.IsNullOrEmpty(e.Data)) sb.AppendLine(e.Data); };
            if (!p.Start()) throw new InvalidOperationException($"'{fileName}' başlatılamadı.");
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await p.WaitForExitAsync(cancellationToken);
            return sb.ToString();
        }
    }
}
