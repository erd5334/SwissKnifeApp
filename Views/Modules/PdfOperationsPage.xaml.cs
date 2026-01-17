using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SwissKnifeApp.Services;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PdfOperationsPage : Page
    {
        private readonly PdfOperationsService _pdfService = new PdfOperationsService();

        public PdfOperationsPage()
        {
            InitializeComponent();
            // Encoding provider'ı kaydet
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // 📄 PDF Birleştirme
        private void BtnMerge_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() == true)
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        _pdfService.MergePdfs(ofd.FileNames, sfd.FileName);
                        MessageBox.Show("PDF dosyaları başarıyla birleştirildi!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Birleştirme sırasında hata oluştu: {ex.Message}");
                    }
                }
            }
        }

        // ✂️ PDF Bölme
        private void BtnSplit_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Bölmek istediğiniz sayfa aralıklarını girin (örnek: 1-3,4-6):",
                "Sayfa Aralığı", "1-3");

            if (string.IsNullOrWhiteSpace(input)) return;

           _pdfService.SplitPdf(ofd.FileName, input);
            MessageBox.Show("PDF başarıyla bölündü!");
        }

        // 🔄 PDF’den Görsel Çıkarma (basit placeholder)
        private void BtnExtractImages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

           _pdfService.ExtractImages(ofd.FileName);

            MessageBox.Show($"PDF {ofd} klasörüne başarıyla görsel olarak dışa aktarıldı!");
        }

        // 🖼️ Görsellerden PDF Oluşturma
        private void BtnImagesToPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "Resim Dosyaları|*.jpg;*.png" };
            if (ofd.ShowDialog() == true)
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
                if (sfd.ShowDialog() == true)
                {
                    try
                    {
                        _pdfService.ImagesToPdf(ofd.FileNames, sfd.FileName);
                        MessageBox.Show("Görsellerden PDF başarıyla oluşturuldu!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"PDF oluşturma sırasında hata: {ex.Message}");
                    }
                }
            }
        }

        // Diğer butonlar placeholder
        private void BtnAddContent_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            string text = Microsoft.VisualBasic.Interaction.InputBox("PDF'e eklenecek metni girin:", "Metin Ekle", "SwissKnifeApp");

            try
            {
                _pdfService.AddContentToPdf(ofd.FileName, sfd.FileName, text);
                MessageBox.Show("PDF'e metin başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Metin ekleme sırasında hata: {ex.Message}");
            }
        }

        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            string pass = Microsoft.VisualBasic.Interaction.InputBox("Şifre girin (boş bırakılırsa şifre kaldırılır):", "PDF Şifreleme");

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            try
            {
                _pdfService.EncryptPdf(ofd.FileName, sfd.FileName, pass);
                MessageBox.Show("İşlem tamamlandı!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifreleme sırasında hata: {ex.Message}");
            }
        }
        private void BtnWatermark_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            string watermark = Microsoft.VisualBasic.Interaction.InputBox("Filigran metni girin:", "Filigran", "Gizli");

            try
            {
                _pdfService.AddWatermark(ofd.FileName, sfd.FileName, watermark);
                MessageBox.Show("Filigran başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filigran ekleme sırasında hata: {ex.Message}");
            }
        }
        private void BtnCompress_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            try
            {
                _pdfService.CompressPdf(ofd.FileName, sfd.FileName);
                MessageBox.Show("PDF başarıyla sıkıştırıldı!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sıkıştırma sırasında hata: {ex.Message}");
            }
        }
        private void BtnReadText_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            try
            {
                string allText = _pdfService.ExtractTextFromPdf(ofd.FileName);
                File.WriteAllText(System.IO.Path.ChangeExtension(ofd.FileName, ".txt"), allText);
                MessageBox.Show("PDF metni başarıyla dışa aktarıldı!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Metin çıkarma sırasında hata: {ex.Message}");
            }
        }
        private void BtnBatchProcess_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "PDF klasörünü seçin"
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            try
            {
                int processedCount = _pdfService.BatchCompressPdfs(dialog.FileName);
                MessageBox.Show($"{processedCount} adet PDF sıkıştırıldı!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Toplu işlem sırasında hata: {ex.Message}");
            }
        }

        #region Form & Annotation & Table Operations

        private void BtnFillForm_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            try
            {
                // Form alanlarını oku
                var fields = _pdfService.GetFormFields(ofd.FileName);
                
                if (fields.Count == 0)
                {
                    MessageBox.Show("Bu PDF'de doldurulabilir form alanı bulunamadı.");
                    return;
                }

                // Form alanlarını göster
                var sb = new StringBuilder();
                sb.AppendLine("Bulunan form alanları:");
                foreach (var field in fields)
                {
                    sb.AppendLine($"  {field.Key}: {field.Value}");
                }

                string fieldName = Microsoft.VisualBasic.Interaction.InputBox(
                    sb.ToString() + "\n\nDoldurmak istediğiniz alan adını girin:",
                    "Form Doldur", fields.Keys.FirstOrDefault() ?? "");
                
                if (string.IsNullOrWhiteSpace(fieldName)) return;

                string fieldValue = Microsoft.VisualBasic.Interaction.InputBox(
                    $"'{fieldName}' alanı için değer girin:",
                    "Form Değeri", "");

                SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
                if (sfd.ShowDialog() != true) return;

                var values = new Dictionary<string, string> { { fieldName, fieldValue } };
                _pdfService.FillPdfForm(ofd.FileName, sfd.FileName, values);
                MessageBox.Show("Form başarıyla dolduruldu!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Form doldurma hatası: {ex.Message}");
            }
        }

        private void BtnAddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            string noteTitle = Microsoft.VisualBasic.Interaction.InputBox(
                "Not başlığını girin:",
                "Not Ekle", "Not");
            
            if (string.IsNullOrWhiteSpace(noteTitle)) return;

            string noteContent = Microsoft.VisualBasic.Interaction.InputBox(
                "Not içeriğini girin:",
                "Not İçeriği", "");

            string pageStr = Microsoft.VisualBasic.Interaction.InputBox(
                "Hangi sayfaya eklensin? (1 = ilk sayfa):",
                "Sayfa Numarası", "1");

            if (!int.TryParse(pageStr, out int pageNumber) || pageNumber < 1)
                pageNumber = 1;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            try
            {
                _pdfService.AddTextAnnotation(ofd.FileName, sfd.FileName, pageNumber, 50, 700, noteTitle, noteContent);
                MessageBox.Show("Not başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Not ekleme hatası: {ex.Message}");
            }
        }

        private void BtnExtractTables_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "CSV dosyalarını kaydetmek için klasör seçin"
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            try
            {
                _pdfService.ExportTablesToCsv(ofd.FileName, dialog.FileName);
                
                var tables = _pdfService.ExtractTables(ofd.FileName);
                if (tables.Count == 0)
                {
                    MessageBox.Show("PDF'de tablo bulunamadı.");
                }
                else
                {
                    MessageBox.Show($"{tables.Count} adet tablo CSV olarak dışa aktarıldı!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tablo çıkarma hatası: {ex.Message}");
            }
        }

        #endregion

        #region OCR Operations

        private async void BtnOcrPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            var language = (CmbOcrLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "tr";
            
            BtnOcrPdf.IsEnabled = false;
            BtnOcrImage.IsEnabled = false;
            PbOcr.Visibility = Visibility.Visible;
            PbOcr.IsIndeterminate = true;
            TxtOcrResult.Text = "OCR işlemi yapılıyor...";

            try
            {
                var progress = new Progress<(int current, int total)>(p =>
                {
                    PbOcr.IsIndeterminate = false;
                    PbOcr.Value = (double)p.current / p.total * 100;
                    TxtOcrResult.Text = $"Sayfa {p.current}/{p.total} işleniyor...";
                });

                string result = await _pdfService.OcrPdfAsync(ofd.FileName, language, progress);
                TxtOcrResult.Text = result;
                MessageBox.Show("OCR işlemi tamamlandı!");
            }
            catch (Exception ex)
            {
                TxtOcrResult.Text = $"HATA: {ex.Message}";
                MessageBox.Show($"OCR sırasında hata: {ex.Message}");
            }
            finally
            {
                BtnOcrPdf.IsEnabled = true;
                BtnOcrImage.IsEnabled = true;
                PbOcr.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnOcrImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog 
            { 
                Filter = "Görüntü Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.gif",
                Multiselect = true
            };
            if (ofd.ShowDialog() != true) return;

            var language = (CmbOcrLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "tr";
            var ocrService = new WindowsOcrService();
            ocrService.SetLanguage(language);

            BtnOcrPdf.IsEnabled = false;
            BtnOcrImage.IsEnabled = false;
            PbOcr.Visibility = Visibility.Visible;
            TxtOcrResult.Text = "OCR işlemi yapılıyor...";

            try
            {
                var sb = new StringBuilder();
                for (int i = 0; i < ofd.FileNames.Length; i++)
                {
                    PbOcr.Value = (double)(i + 1) / ofd.FileNames.Length * 100;
                    
                    string text = await ocrService.RecognizeFromImageAsync(ofd.FileNames[i]);
                    sb.AppendLine($"--- {System.IO.Path.GetFileName(ofd.FileNames[i])} ---");
                    sb.AppendLine(text);
                    sb.AppendLine();
                }

                TxtOcrResult.Text = sb.ToString();
                MessageBox.Show("OCR işlemi tamamlandı!");
            }
            catch (Exception ex)
            {
                TxtOcrResult.Text = $"HATA: {ex.Message}";
                MessageBox.Show($"OCR sırasında hata: {ex.Message}");
            }
            finally
            {
                BtnOcrPdf.IsEnabled = true;
                BtnOcrImage.IsEnabled = true;
                PbOcr.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCopyOcr_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtOcrResult.Text))
            {
                Clipboard.SetText(TxtOcrResult.Text);
                MessageBox.Show("OCR sonucu panoya kopyalandı!");
            }
        }

        private void BtnSaveOcr_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtOcrResult.Text)) return;

            SaveFileDialog sfd = new SaveFileDialog 
            { 
                Filter = "Metin Dosyası|*.txt|Word Belgesi|*.docx",
                DefaultExt = ".txt"
            };
            
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, TxtOcrResult.Text);
                    MessageBox.Show("OCR sonucu kaydedildi!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kaydetme hatası: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
