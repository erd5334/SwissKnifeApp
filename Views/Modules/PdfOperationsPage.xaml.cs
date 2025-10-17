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
    }
}
