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
        private readonly SwissKnifeApp.Services.TextSummarizerService _summarizer = new SwissKnifeApp.Services.TextSummarizerService();
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
            BorderSummaryStats.Visibility = Visibility.Collapsed;
        }

        private void UpdateInputStats()
        {
            var text = TxtInputText.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                TxtInputStats.Text = "Kelime: 0 | Cümle: 0 | Karakter: 0";
                return;
            }

            var wordCount = _summarizer.GetWords(text).Count;
            var sentenceCount = _summarizer.GetSentences(text).Count;
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
                var summary = _summarizer.SummarizeText(text, ratio, language);

                TxtSummary.Text = summary;
                BorderSummaryStats.Visibility = Visibility.Visible;

                var originalWords = _summarizer.GetWords(text).Count;
                var summaryWords = _summarizer.GetWords(summary).Count;
                var reduction = (1 - (double)summaryWords / originalWords) * 100;

                TxtSummaryStats.Text = $"✅ Özet oluşturuldu! | Orijinal: {originalWords} kelime → Özet: {summaryWords} kelime | Azalma: %{reduction:F1}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Özet oluşturulurken hata:\n{ex.Message}", 
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var keywords = _summarizer.FindKeywords(text, count, language);

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
                var sentences = _summarizer.FindImportantSentences(text, count, language);

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

        

        // ============================================
        // YARDIMCI FONKSİYONLAR
        // ============================================

        

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
