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
    public enum VideoFormat { Mp4, Mkv, WebM, Mov, Ts, Avi, Flv }
    public enum VideoCodec { H264, H265, Vp9, Copy }
    public enum VideoQuality { Highest, High, Medium, Low, Lossless }
    public enum ResolutionPreset { Original, P2160, P1440, P1080, P720, P480 }

    public readonly struct CropRect
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public CropRect(int x, int y, int width, int height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    public class VideoJobProgress
    {
        public int TotalFiles { get; init; }
        public int CurrentFileIndex { get; init; } // 1-based
        public double CurrentFilePercent { get; init; } // 0..100
        public string? Message { get; init; }
    }

    public class VideoToolsService
    {
        private string? _ffmpegPath;
        private string? _ffprobePath;

        public async Task ConvertAsync(
            IList<string> inputFiles,
            string outputFolder,
            VideoFormat format,
            VideoCodec codec,
            VideoQuality quality,
            ResolutionPreset resolution,
            IProgress<VideoJobProgress>? progress = null,
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

                await ConvertOrTrimCropInternalAsync(
                    input,
                    outPath,
                    format,
                    codec,
                    quality,
                    resolution,
                    start: null,
                    end: null,
                    crop: null,
                    fileProgress: new Progress<double>(p =>
                    {
                        progress?.Report(new VideoJobProgress
                        {
                            TotalFiles = inputFiles.Count,
                            CurrentFileIndex = i + 1,
                            CurrentFilePercent = p
                        });
                    }),
                    log: log,
                    cancellationToken);

                progress?.Report(new VideoJobProgress
                {
                    TotalFiles = inputFiles.Count,
                    CurrentFileIndex = i + 1,
                    CurrentFilePercent = 100,
                    Message = $"{Path.GetFileName(outPath)} tamamlandı"
                });
            }
        }

        public async Task TrimCropAsync(
            string inputFile,
            string outputFile,
            VideoFormat format,
            VideoCodec codec,
            VideoQuality quality,
            ResolutionPreset resolution,
            TimeSpan? start,
            TimeSpan? end,
            CropRect? crop,
            IProgress<double>? fileProgress = null,
            IProgress<string>? log = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(inputFile)) throw new FileNotFoundException("Girdi dosyası bulunamadı", inputFile);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
            await EnsureFfmpegAsync(log, cancellationToken);

            await ConvertOrTrimCropInternalAsync(inputFile, outputFile, format, codec, quality, resolution, start, end, crop, fileProgress, log, cancellationToken);
        }

        private async Task ConvertOrTrimCropInternalAsync(
            string input,
            string output,
            VideoFormat format,
            VideoCodec codec,
            VideoQuality quality,
            ResolutionPreset resolution,
            TimeSpan? start,
            TimeSpan? end,
            CropRect? crop,
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

            // Build filters (crop, scale)
            var vf = new StringBuilder();
            if (crop.HasValue)
            {
                var c = crop.Value;
                vf.Append($"crop={c.Width}:{c.Height}:{c.X}:{c.Y}");
            }
            var scaleExpr = GetScaleExpression(resolution);
            if (!string.IsNullOrEmpty(scaleExpr))
            {
                if (vf.Length > 0) vf.Append(',');
                vf.Append(scaleExpr);
            }
            if (vf.Length > 0) args.Append($"-vf \"{vf}\" ");

            // Codec/container compatibility and copy handling
            bool filtersApplied = vf.Length > 0;
            var codecToUse = codec;
            // If WebM container, force VP9
            if (format == VideoFormat.WebM && (codecToUse == VideoCodec.H264 || codecToUse == VideoCodec.H265))
            {
                log?.Report("WebM kapsayıcı H264/H265 desteklemez, VP9 kullanılacak.");
                codecToUse = VideoCodec.Vp9;
            }
            // If MP4/MOV container with VP9, switch to H264
            if ((format == VideoFormat.Mp4 || format == VideoFormat.Mov) && codecToUse == VideoCodec.Vp9)
            {
                log?.Report("MP4/MOV kapsayıcı VP9 için uygun değil, H.264 kullanılacak.");
                codecToUse = VideoCodec.H264;
            }
            // If FLV, enforce H.264
            if (format == VideoFormat.Flv && (codecToUse == VideoCodec.H265 || codecToUse == VideoCodec.Vp9))
            {
                log?.Report("FLV kapsayıcı H.265/VP9 için uygun değil, H.264 kullanılacak.");
                codecToUse = VideoCodec.H264;
            }
            // If AVI, avoid H.265/VP9
            if (format == VideoFormat.Avi && (codecToUse == VideoCodec.H265 || codecToUse == VideoCodec.Vp9))
            {
                log?.Report("AVI kapsayıcı için H.265/VP9 uygun değil, H.264 kullanılacak.");
                codecToUse = VideoCodec.H264;
            }
            // If TS with VP9, switch to H.264
            if (format == VideoFormat.Ts && codecToUse == VideoCodec.Vp9)
            {
                log?.Report("TS kapsayıcı VP9 için uygun değil, H.264 kullanılacak.");
                codecToUse = VideoCodec.H264;
            }
            // If user requested copy but filters are applied or timings specified, fall back to re-encode
            if (codecToUse == VideoCodec.Copy && (filtersApplied || start.HasValue || end.HasValue))
            {
                // choose a sensible default based on container
                codecToUse = format == VideoFormat.WebM ? VideoCodec.Vp9 : VideoCodec.H264;
                log?.Report("Kopyalama (copy) filtre/trim ile uyumsuz; yeniden kodlama yapılacak.");
            }

            // Select codecs and quality
            AppendCodecArgs(args, format, codecToUse, quality);

            // select container/audio codec
            AppendAudioArgs(args, format);

            // ensure compatibility and faststart for mp4/mov
            if (format == VideoFormat.Mp4 || format == VideoFormat.Mov)
            {
                args.Append("-movflags +faststart ");
            }

            // Add pixel format for broad compatibility on H.264/H.265
            if (codecToUse == VideoCodec.H264 || codecToUse == VideoCodec.H265)
            {
                args.Append("-pix_fmt yuv420p ");
                if (codecToUse == VideoCodec.H265 && format == VideoFormat.Mp4)
                {
                    // better compatibility on some players
                    args.Append("-tag:v hvc1 ");
                }
            }

            args.Append($"\"{output}\"");

            var finalArgs = args.ToString();
            log?.Report($"ffmpeg {finalArgs}");

            await RunProcessAsync(
                fileName: _ffmpegPath!,
                arguments: finalArgs,
                workingDir: Path.GetDirectoryName(output)!,
                onStdOut: line => { if (!string.IsNullOrWhiteSpace(line)) log?.Report(line); },
                onStdErr: line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        log?.Report(line);
                        var p = TryParseFfmpegProgress(line, duration);
                        if (p.HasValue) fileProgress?.Report(Math.Max(0, Math.Min(100, p.Value)));
                    }
                },
                cancellationToken: ct);
        }

        private static void AppendCodecArgs(StringBuilder args, VideoFormat format, VideoCodec codec, VideoQuality quality)
        {
            // If filters applied or quality specified, avoid copy unless explicitly asked with no filters
            if (codec == VideoCodec.Copy)
            {
                args.Append("-c:v copy ");
                return;
            }

            switch (codec)
            {
                case VideoCodec.H264:
                    args.Append("-c:v libx264 ");
                    args.Append("-preset medium ");
                    args.Append(GetCrfArg(codec, quality));
                    break;
                case VideoCodec.H265:
                    args.Append("-c:v libx265 ");
                    args.Append("-preset medium ");
                    args.Append(GetCrfArg(codec, quality));
                    break;
                case VideoCodec.Vp9:
                    args.Append("-c:v libvpx-vp9 ");
                    args.Append(GetCrfArg(codec, quality));
                    args.Append("-b:v 0 "); // use CRF-based quality
                    break;
                default:
                    args.Append("-c:v libx264 -crf 23 ");
                    break;
            }
        }

        private static string GetCrfArg(VideoCodec codec, VideoQuality quality)
        {
            if (quality == VideoQuality.Lossless)
            {
                return codec switch
                {
                    VideoCodec.H264 => "-crf 0 ",
                    VideoCodec.H265 => "-x265-params lossless=1 ",
                    VideoCodec.Vp9 => "-lossless 1 ",
                    _ => "-crf 0 "
                };
            }

            int crf = codec switch
            {
                VideoCodec.H264 => quality switch
                {
                    VideoQuality.Highest => 18,
                    VideoQuality.High => 20,
                    VideoQuality.Medium => 23,
                    VideoQuality.Low => 28,
                    _ => 23
                },
                VideoCodec.H265 => quality switch
                {
                    VideoQuality.Highest => 20,
                    VideoQuality.High => 22,
                    VideoQuality.Medium => 26,
                    VideoQuality.Low => 30,
                    _ => 26
                },
                VideoCodec.Vp9 => quality switch
                {
                    VideoQuality.Highest => 18,
                    VideoQuality.High => 22,
                    VideoQuality.Medium => 28,
                    VideoQuality.Low => 33,
                    _ => 28
                },
                _ => 23
            };
            return $"-crf {crf} ";
        }

        private static void AppendAudioArgs(StringBuilder args, VideoFormat format)
        {
            switch (format)
            {
                case VideoFormat.WebM:
                    args.Append("-c:a libopus -b:a 160k ");
                    break;
                case VideoFormat.Avi:
                    args.Append("-c:a libmp3lame -b:a 192k ");
                    break;
                case VideoFormat.Flv:
                    args.Append("-c:a aac -ar 44100 -b:a 128k ");
                    break;
                case VideoFormat.Ts:
                    args.Append("-c:a aac -b:a 192k ");
                    // optional: args.Append("-muxdelay 0 -muxpreload 0 ");
                    break;
                default:
                    args.Append("-c:a aac -b:a 192k ");
                    break;
            }
        }

        private static string GetExtension(VideoFormat format) => format switch
        {
            VideoFormat.Mp4 => "mp4",
            VideoFormat.Mkv => "mkv",
            VideoFormat.WebM => "webm",
            VideoFormat.Mov => "mov",
            VideoFormat.Ts => "ts",
            VideoFormat.Avi => "avi",
            VideoFormat.Flv => "flv",
            _ => "mp4"
        };

        private static string GetScaleExpression(ResolutionPreset preset)
        {
            return preset switch
            {
                ResolutionPreset.Original => string.Empty,
                ResolutionPreset.P2160 => "scale=-2:2160",
                ResolutionPreset.P1440 => "scale=-2:1440",
                ResolutionPreset.P1080 => "scale=-2:1080",
                ResolutionPreset.P720 => "scale=-2:720",
                ResolutionPreset.P480 => "scale=-2:480",
                _ => string.Empty
            };
        }

        private static string FormatTs(TimeSpan ts)
        {
            return ts.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        }

        private double? TryParseFfmpegProgress(string line, double? totalSeconds)
        {
            if (totalSeconds is null || totalSeconds <= 0) return null;
            var m = Regex.Match(line, @"time=(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2})[\.]?(?<ms>\d+)?");
            if (m.Success)
            {
                var h = int.Parse(m.Groups["h"].Value);
                var mi = int.Parse(m.Groups["m"].Value);
                var s = int.Parse(m.Groups["s"].Value);
                var ms = 0;
                int.TryParse(m.Groups["ms"].Value, out ms);
                var seconds = h * 3600 + mi * 60 + s + (ms / 100.0);
                return seconds / totalSeconds.Value * 100.0;
            }
            return null;
        }

        private async Task<double?> ProbeDurationSecondsAsync(string input, CancellationToken ct)
        {
            await EnsureFfmpegAsync(null, ct);
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
            try
            {
                await RunProcessAsync(toolName, "-version", Directory.GetCurrentDirectory(), _ => { }, _ => { }, ct, silent: true);
                return toolName;
            }
            catch { }

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

        public static bool TryParseCrop(string? text, out CropRect? crop)
        {
            crop = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            var parts = text.Split(',');
            if (parts.Length != 4) return false;
            try
            {
                var x = int.Parse(parts[0].Trim());
                var y = int.Parse(parts[1].Trim());
                var w = int.Parse(parts[2].Trim());
                var h = int.Parse(parts[3].Trim());
                if (w <= 0 || h <= 0) return false;
                crop = new CropRect(x, y, w, h);
                return true;
            }
            catch { return false; }
        }
    }
}
