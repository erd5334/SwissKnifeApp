using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SwissKnifeApp.Services
{
    /// <summary>
    /// Windows 10/11 yerleşik OCR motorunu kullanarak görüntülerden metin çıkarır.
    /// Tesseract gibi harici bağımlılık gerektirmez.
    /// </summary>
    public class WindowsOcrService
    {
        private OcrEngine? _ocrEngine;
        private string _currentLanguage = "tr";

        public IReadOnlyList<string> AvailableLanguages => GetAvailableLanguages();

        private static IReadOnlyList<string> GetAvailableLanguages()
        {
            var languages = new List<string>();
            foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
            {
                languages.Add(lang.LanguageTag);
            }
            return languages;
        }

        public void SetLanguage(string languageTag)
        {
            _currentLanguage = languageTag;
            _ocrEngine = null; // Reset to reinitialize with new language
        }

        private OcrEngine GetEngine()
        {
            if (_ocrEngine != null) return _ocrEngine;

            // Önce istenen dili dene
            var language = new Windows.Globalization.Language(_currentLanguage);
            if (OcrEngine.IsLanguageSupported(language))
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(language);
            }

            // Fallback to default
            _ocrEngine ??= OcrEngine.TryCreateFromUserProfileLanguages();

            if (_ocrEngine == null)
                throw new InvalidOperationException("OCR motoru başlatılamadı. Windows dil paketlerini kontrol edin.");

            return _ocrEngine;
        }

        /// <summary>
        /// Görüntü dosyasından metin çıkarır
        /// </summary>
        public async Task<string> RecognizeFromImageAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Görüntü dosyası bulunamadı", imagePath);

            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var engine = GetEngine();
            var result = await engine.RecognizeAsync(softwareBitmap);

            return result.Text;
        }

        /// <summary>
        /// Byte array'den metin çıkarır
        /// </summary>
        public async Task<string> RecognizeFromBytesAsync(byte[] imageBytes)
        {
            using var memStream = new InMemoryRandomAccessStream();
            await memStream.WriteAsync(imageBytes.AsBuffer());
            memStream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(memStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var engine = GetEngine();
            var result = await engine.RecognizeAsync(softwareBitmap);

            return result.Text;
        }

        /// <summary>
        /// Stream'den metin çıkarır
        /// </summary>
        public async Task<string> RecognizeFromStreamAsync(Stream imageStream)
        {
            using var memStream = new InMemoryRandomAccessStream();
            var buffer = new byte[imageStream.Length];
            await imageStream.ReadAsync(buffer, 0, buffer.Length);
            await memStream.WriteAsync(buffer.AsBuffer());
            memStream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(memStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var engine = GetEngine();
            var result = await engine.RecognizeAsync(softwareBitmap);

            return result.Text;
        }

        /// <summary>
        /// Detaylı OCR sonucu döndürür (satırlar ve kelimeler ayrı)
        /// </summary>
        public async Task<OcrResultDetails> RecognizeDetailedAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Görüntü dosyası bulunamadı", imagePath);

            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            using var stream = await file.OpenAsync(FileAccessMode.Read);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var engine = GetEngine();
            var result = await engine.RecognizeAsync(softwareBitmap);

            var details = new OcrResultDetails
            {
                FullText = result.Text,
                TextAngle = result.TextAngle
            };

            foreach (var line in result.Lines)
            {
                var lineResult = new OcrLineResult { Text = line.Text };
                foreach (var word in line.Words)
                {
                    lineResult.Words.Add(new OcrWordResult
                    {
                        Text = word.Text,
                        BoundingRect = new System.Windows.Rect(
                            word.BoundingRect.X,
                            word.BoundingRect.Y,
                            word.BoundingRect.Width,
                            word.BoundingRect.Height)
                    });
                }
                details.Lines.Add(lineResult);
            }

            return details;
        }

        /// <summary>
        /// Birden fazla görüntüyü OCR ile işler
        /// </summary>
        public async Task<List<string>> RecognizeBatchAsync(
            IEnumerable<string> imagePaths,
            IProgress<(int current, int total, string file)>? progress = null)
        {
            var results = new List<string>();
            var paths = new List<string>(imagePaths);
            int total = paths.Count;

            for (int i = 0; i < paths.Count; i++)
            {
                progress?.Report((i + 1, total, Path.GetFileName(paths[i])));
                
                try
                {
                    var text = await RecognizeFromImageAsync(paths[i]);
                    results.Add(text);
                }
                catch (Exception ex)
                {
                    results.Add($"[HATA: {ex.Message}]");
                }
            }

            return results;
        }
    }

    public class OcrResultDetails
    {
        public string FullText { get; set; } = "";
        public double? TextAngle { get; set; }
        public List<OcrLineResult> Lines { get; set; } = new();
    }

    public class OcrLineResult
    {
        public string Text { get; set; } = "";
        public List<OcrWordResult> Words { get; set; } = new();
    }

    public class OcrWordResult
    {
        public string Text { get; set; } = "";
        public System.Windows.Rect BoundingRect { get; set; }
    }
}
