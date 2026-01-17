using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SwissKnifeApp.Services
{
    /// <summary>
    /// yt-dlp ve ffmpeg araçlarını otomatik indirip kuran servis
    /// İlk açılışta veya "Araçları Kur" butonu ile kullanılır
    /// </summary>
    public class ToolInstallerService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> EnsureToolsInstalledAsync(
            string targetDirectory,
            IProgress<string>? log = null,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(targetDirectory);
            bool ytdlp = await InstallYtDlpAsync(log, targetDirectory, progress, cancellationToken);
            bool ffmpeg = await InstallFfmpegAsync(log, targetDirectory, progress, cancellationToken);
            return ytdlp && ffmpeg;
        }

        public async Task<bool> InstallYtDlpAsync(IProgress<string>? log = null, string? targetDir = null, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            targetDir ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
            Directory.CreateDirectory(targetDir);
            var path = Path.Combine(targetDir, "yt-dlp.exe");
            
            if (File.Exists(path)) { log?.Report("✓ yt-dlp zaten kurulu."); return true; }

            log?.Report("yt-dlp indiriliyor...");
            try {
                await DownloadFileAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", path, progress, ct);
                log?.Report("✓ yt-dlp başarıyla kuruldu.");
                return true;
            } catch (Exception ex) { log?.Report($"❌ yt-dlp hatası: {ex.Message}"); return false; }
        }

        public async Task<bool> InstallFfmpegAsync(IProgress<string>? log = null, string? targetDir = null, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            targetDir ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
            Directory.CreateDirectory(targetDir);
            var path = Path.Combine(targetDir, "ffmpeg.exe");

            if (File.Exists(path)) { log?.Report("✓ ffmpeg zaten kurulu."); return true; }

            log?.Report("ffmpeg indiriliyor...");
            try {
                await DownloadAndExtractFFmpegAsync(path, log, progress, ct);
                log?.Report("✓ ffmpeg başarıyla kuruldu.");
                return true;
            } catch (Exception ex) { log?.Report($"❌ ffmpeg hatası: {ex.Message}"); return false; }
        }

        private async Task DownloadFileAsync(
            string url,
            string outputPath,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (double)totalRead / totalBytes * 100.0;
                    progress?.Report(percent);
                }
            }
        }

        private async Task DownloadAndExtractFFmpegAsync(
            string ffmpegExePath,
            IProgress<string>? log,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            var tempZip = Path.Combine(Path.GetTempPath(), "ffmpeg_temp.zip");
            var tempExtract = Path.Combine(Path.GetTempPath(), "ffmpeg_extract_" + Guid.NewGuid().ToString("N"));

            try
            {
                // İndir
                log?.Report("  İndiriliyor...");
                await DownloadFileAsync(
                    "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
                    tempZip,
                    progress,
                    cancellationToken);

                // Çıkart
                log?.Report("  Çıkarılıyor...");
                ZipFile.ExtractToDirectory(tempZip, tempExtract);

                // ffmpeg.exe'yi bul
                var ffmpegExe = Directory.GetFiles(tempExtract, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (ffmpegExe == null)
                    throw new FileNotFoundException("ffmpeg.exe arşiv içinde bulunamadı");

                File.Copy(ffmpegExe, ffmpegExePath, true);
            }
            finally
            {
                // Temizlik
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
                if (Directory.Exists(tempExtract))
                    Directory.Delete(tempExtract, true);
            }
        }

        /// <summary>
        /// Araçların kurulu olup olmadığını kontrol eder
        /// </summary>
        public static (bool ytdlp, bool ffmpeg) CheckToolsInstalled(string toolsDirectory)
        {
            var ytdlpPath = Path.Combine(toolsDirectory, "yt-dlp.exe");
            var ffmpegPath = Path.Combine(toolsDirectory, "ffmpeg.exe");

            return (File.Exists(ytdlpPath), File.Exists(ffmpegPath));
        }
    }
}
