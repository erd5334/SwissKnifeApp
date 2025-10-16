using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class TextSummarizerPage : Page
    {
        public TextSummarizerPage()
        {
            InitializeComponent();
            TxtInputText.TextChanged += (s, e) => UpdateInputStats();
        }

        // ============================================
        // DOSYA YÜKLEME VE TEMİZLEME
        // ============================================

        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Metin Dosyaları|*.txt|Tüm Dosyalar|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                    TxtInputText.Text = text;
                    MessageBox.Show($"Dosya başarıyla yüklendi!\n\n{Path.GetFileName(dlg.FileName)}", 
                        "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya yüklenirken hata oluştu:\n{ex.Message}", 
                        "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearInput_Click(object sender, RoutedEventArgs e)
        {
            TxtInputText.Clear();
            TxtSummary.Clear();
            LstKeywords.ItemsSource = null;
            LstSentences.ItemsSource = null;
            TxtSummaryStats.Text = "Özet oluşturulmadı.";
        }

        private void UpdateInputStats()
        {
            var text = TxtInputText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                TxtInputStats.Text = "Kelime: 0 | Cümle: 0 | Karakter: 0";
                return;
            }

            var wordCount = GetWords(text).Count;
            var sentenceCount = GetSentences(text).Count;
            var charCount = text.Length;

            TxtInputStats.Text = $"Kelime: {wordCount} | Cümle: {sentenceCount} | Karakter: {charCount}";
        }

        private void SliderSummaryRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtSummaryRatio != null)
                TxtSummaryRatio.Text = $"{(int)SliderSummaryRatio.Value}%";
        }

        // ============================================
        // METİN ÖZETLEYİCİ
        // ============================================

        private void BtnSummarize_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtInputText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Lütfen özetlenecek metni girin!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ratio = (int)SliderSummaryRatio.Value / 100.0;
                var language = CmbLanguage.SelectedIndex == 0 ? "tr" : "en";
                var summary = SummarizeText(text, ratio, language);

                TxtSummary.Text = summary;

                var originalWords = GetWords(text).Count;
                var summaryWords = GetWords(summary).Count;
                var reduction = (1 - (double)summaryWords / originalWords) * 100;

                TxtSummaryStats.Text = $"✅ Özet oluşturuldu! | Orijinal: {originalWords} kelime → Özet: {summaryWords} kelime | Azalma: %{reduction:F1}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Özet oluşturulurken hata:\n{ex.Message}", 
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SummarizeText(string text, double ratio, string language)
        {
            // Cümlelere ayır
            var sentences = GetSentences(text);
            if (sentences.Count == 0) return "";

            // Her cümlenin skorunu hesapla
            var sentenceScores = new Dictionary<string, double>();
            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);

            foreach (var sentence in sentences)
            {
                var sentenceWords = GetWords(sentence);
                double score = 0;

                foreach (var word in sentenceWords)
                {
                    var normalizedWord = NormalizeWord(word, language);
                    if (wordFreq.ContainsKey(normalizedWord))
                    {
                        score += wordFreq[normalizedWord];
                    }
                }

                // Cümle uzunluğuna göre normalize et
                if (sentenceWords.Count > 0)
                    score /= sentenceWords.Count;

                sentenceScores[sentence] = score;
            }

            // En yüksek skorlu cümleleri seç
            var targetCount = Math.Max(1, (int)(sentences.Count * ratio));
            var selectedSentences = sentenceScores
                .OrderByDescending(x => x.Value)
                .Take(targetCount)
                .Select(x => x.Key)
                .ToList();

            // Orijinal sıralamayı koru
            var summary = new List<string>();
            foreach (var sentence in sentences)
            {
                if (selectedSentences.Contains(sentence))
                {
                    summary.Add(sentence);
                }
            }

            return string.Join(" ", summary);
        }

        // ============================================
        // ANAHTAR KELİME BULUCU
        // ============================================

        private void BtnFindKeywords_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtInputText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Lütfen metni girin!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var count = (int)(NumKeywordCount.Value ?? 10);
                var language = CmbLanguage.SelectedIndex == 0 ? "tr" : "en";
                var keywords = FindKeywords(text, count, language);

                var keywordList = keywords.Select((kv, index) => new KeywordItem
                {
                    Rank = $"#{index + 1}",
                    Word = kv.Key,
                    Score = kv.Value
                }).ToList();

                LstKeywords.ItemsSource = keywordList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Anahtar kelimeler bulunurken hata:\n{ex.Message}", 
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Dictionary<string, double> FindKeywords(string text, int count, string language)
        {
            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);

            return wordFreq
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        // ============================================
        // ÖNEMLİ CÜMLE BULUCU
        // ============================================

        private void BtnFindSentences_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtInputText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Lütfen metni girin!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var count = (int)(NumSentenceCount.Value ?? 5);
                var language = CmbLanguage.SelectedIndex == 0 ? "tr" : "en";
                var sentences = FindImportantSentences(text, count, language);

                var sentenceList = sentences.Select((kv, index) => new SentenceItem
                {
                    Rank = $"#{index + 1}",
                    Text = kv.Key,
                    Score = kv.Value
                }).ToList();

                LstSentences.ItemsSource = sentenceList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önemli cümleler bulunurken hata:\n{ex.Message}", 
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Dictionary<string, double> FindImportantSentences(string text, int count, string language)
        {
            var sentences = GetSentences(text);
            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);

            var sentenceScores = new Dictionary<string, double>();

            foreach (var sentence in sentences)
            {
                var sentenceWords = GetWords(sentence);
                double score = 0;

                // Kelime frekansına göre skor
                foreach (var word in sentenceWords)
                {
                    var normalizedWord = NormalizeWord(word, language);
                    if (wordFreq.ContainsKey(normalizedWord))
                    {
                        score += wordFreq[normalizedWord];
                    }
                }

                // Cümle pozisyonu bonusu (başlangıç cümleleri önemli)
                var position = sentences.IndexOf(sentence);
                if (position < sentences.Count * 0.1) // İlk %10
                    score *= 1.2;

                // Normalize et
                if (sentenceWords.Count > 0)
                    score /= sentenceWords.Count;

                sentenceScores[sentence] = score;
            }

            return sentenceScores
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        // ============================================
        // YARDIMCI FONKSİYONLAR
        // ============================================

        private List<string> GetSentences(string text)
        {
            // Cümleleri ayır (. ! ? ile biten)
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
            return sentences;
        }

        private List<string> GetWords(string text)
        {
            // Kelimeleri ayır
            var words = Regex.Split(text, @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
                .ToList();
            return words;
        }

        private Dictionary<string, double> CalculateWordFrequency(List<string> words, string language)
        {
            var stopWords = GetStopWords(language);
            var wordFreq = new Dictionary<string, double>();

            foreach (var word in words)
            {
                var normalizedWord = NormalizeWord(word, language);

                // Stop word kontrolü
                if (stopWords.Contains(normalizedWord))
                    continue;

                if (!wordFreq.ContainsKey(normalizedWord))
                    wordFreq[normalizedWord] = 0;

                wordFreq[normalizedWord]++;
            }

            // Normalize et (TF-IDF benzeri)
            var maxFreq = wordFreq.Values.Max();
            var normalizedFreq = new Dictionary<string, double>();

            foreach (var kv in wordFreq)
            {
                normalizedFreq[kv.Key] = kv.Value / maxFreq;
            }

            return normalizedFreq;
        }

        private string NormalizeWord(string word, string language)
        {
            var normalized = word.ToLowerInvariant();

            if (language == "tr")
            {
                // Türkçe karakterler
                normalized = normalized
                    .Replace('ı', 'i')
                    .Replace('ğ', 'g')
                    .Replace('ü', 'u')
                    .Replace('ş', 's')
                    .Replace('ö', 'o')
                    .Replace('ç', 'c');
            }

            return normalized;
        }

        private HashSet<string> GetStopWords(string language)
        {
            if (language == "tr")
            {
                return new HashSet<string>
                {
                    "bir", "ve", "veya", "ancak", "fakat", "çünkü", "için", "ile", "bu", "şu", "o",
                    "ben", "sen", "biz", "siz", "onlar", "şey", "var", "yok", "gibi", "kadar",
                    "daha", "en", "çok", "az", "şimdi", "sonra", "önce", "burada", "orada",
                    "her", "bazı", "hiç", "de", "da", "mi", "mı", "mu", "mü", "ki", "ise",
                    "olan", "olarak", "ama", "sadece", "bile", "artık", "hala", "dahi",
                    "ne", "nasıl", "neden", "niçin", "nerede", "kim", "kime", "ne zaman"
                };
            }
            else // İngilizce
            {
                return new HashSet<string>
                {
                    "the", "a", "an", "and", "or", "but", "if", "then", "else", "when",
                    "at", "from", "by", "on", "off", "for", "in", "out", "over", "to",
                    "into", "with", "is", "are", "was", "were", "be", "been", "being",
                    "have", "has", "had", "do", "does", "did", "will", "would", "should",
                    "could", "may", "might", "must", "can", "this", "that", "these", "those",
                    "i", "you", "he", "she", "it", "we", "they", "what", "which", "who",
                    "where", "when", "why", "how", "all", "each", "every", "both", "few",
                    "more", "most", "other", "some", "such", "no", "nor", "not", "only",
                    "own", "same", "so", "than", "too", "very", "just", "as"
                };
            }
        }

        // ============================================
        // KOPYALAMA FONKSİYONLARI
        // ============================================

        private void BtnCopySummary_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSummary.Text))
            {
                MessageBox.Show("Önce özet oluşturun!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Clipboard.SetText(TxtSummary.Text);
            MessageBox.Show("Özet panoya kopyalandı!", "Başarılı", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCopyKeywords_Click(object sender, RoutedEventArgs e)
        {
            var keywords = LstKeywords.ItemsSource as List<KeywordItem>;
            if (keywords == null || keywords.Count == 0)
            {
                MessageBox.Show("Önce anahtar kelimeleri bulun!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = string.Join("\n", keywords.Select(k => $"{k.Rank} {k.Word} ({k.Score:F2})"));
            Clipboard.SetText(text);
            MessageBox.Show("Anahtar kelimeler panoya kopyalandı!", "Başarılı", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCopySentences_Click(object sender, RoutedEventArgs e)
        {
            var sentences = LstSentences.ItemsSource as List<SentenceItem>;
            if (sentences == null || sentences.Count == 0)
            {
                MessageBox.Show("Önce önemli cümleleri bulun!", "Uyarı", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = string.Join("\n\n", sentences.Select(s => $"{s.Rank} {s.Text}"));
            Clipboard.SetText(text);
            MessageBox.Show("Önemli cümleler panoya kopyalandı!", "Başarılı", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ============================================
    // YARDIMCI SINIFLAR
    // ============================================

    public class KeywordItem
    {
        public string Rank { get; set; } = "";
        public string Word { get; set; } = "";
        public double Score { get; set; }
    }

    public class SentenceItem
    {
        public string Rank { get; set; } = "";
        public string Text { get; set; } = "";
        public double Score { get; set; }
    }
}
