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

            var ytdlpPath = Path.Combine(targetDirectory, "yt-dlp.exe");
            var ffmpegPath = Path.Combine(targetDirectory, "ffmpeg.exe");

            bool ytdlpExists = File.Exists(ytdlpPath);
            bool ffmpegExists = File.Exists(ffmpegPath);

            if (ytdlpExists && ffmpegExists)
            {
                log?.Report("✓ Tüm araçlar zaten kurulu.");
                return true;
            }

            log?.Report("Gerekli araçlar indiriliyor...");

            // yt-dlp indir
            if (!ytdlpExists)
            {
                log?.Report("yt-dlp indiriliyor... (~10 MB)");
                try
                {
                    await DownloadFileAsync(
                        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
                        ytdlpPath,
                        progress,
                        cancellationToken);
                    log?.Report("✓ yt-dlp indirildi");
                }
                catch (Exception ex)
                {
                    log?.Report($"❌ yt-dlp indirilemedi: {ex.Message}");
                    return false;
                }
            }

            // ffmpeg indir
            if (!ffmpegExists)
            {
                log?.Report("ffmpeg indiriliyor... (~120 MB, biraz sürebilir)");
                try
                {
                    await DownloadAndExtractFFmpegAsync(ffmpegPath, log, progress, cancellationToken);
                    log?.Report("✓ ffmpeg indirildi");
                }
                catch (Exception ex)
                {
                    log?.Report($"❌ ffmpeg indirilemedi: {ex.Message}");
                    return false;
                }
            }

            log?.Report("✅ Kurulum tamamlandı!");
            return true;
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
