using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text; // Bu using'i ekle
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Document = iTextSharp.text.Document;
using Font = iTextSharp.text.Font;
using Path = System.IO.Path;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PdfOperationsPage : Page
    {
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
                    using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                    using (Document doc = new Document())
                    using (PdfCopy pdf = new PdfCopy(doc, stream))
                    {
                        doc.Open();
                        foreach (string file in ofd.FileNames)
                        {
                            PdfReader reader = new PdfReader(file);
                            for (int i = 1; i <= reader.NumberOfPages; i++)
                                pdf.AddPage(pdf.GetImportedPage(reader, i));
                            reader.Close();
                        }
                    }
                    MessageBox.Show("PDF dosyaları başarıyla birleştirildi!");
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

            var ranges = input.Split(',').Select(r => r.Split('-')).ToList();
            PdfReader reader = new PdfReader(ofd.FileName);

            int part = 1;
            foreach (var r in ranges)
            {
                int start = int.Parse(r[0]);
                int end = r.Length > 1 ? int.Parse(r[1]) : start;

                Document document = new Document();
                string output = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ofd.FileName), $"Parca_{part++}.pdf");
                PdfCopy copy = new PdfCopy(document, new FileStream(output, FileMode.Create));
                document.Open();

                for (int i = start; i <= end && i <= reader.NumberOfPages; i++)
                    copy.AddPage(copy.GetImportedPage(reader, i));

                document.Close();
            }

            reader.Close();
            MessageBox.Show("PDF başarıyla bölündü!");
        }

        // 🔄 PDF’den Görsel Çıkarma (basit placeholder)
        private void BtnExtractImages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ofd.FileName),
                Path.GetFileNameWithoutExtension(ofd.FileName) + "_Pages");
            Directory.CreateDirectory(folder);

            using (var doc = PdfiumViewer.PdfDocument.Load(ofd.FileName))
            {
                for (int i = 0; i < doc.PageCount; i++)
                {
                    using (var image = doc.Render(i, 300, 300, true))
                    {
                        string output = Path.Combine(folder, $"Sayfa_{i + 1}.png");
                        image.Save(output, ImageFormat.Png);
                    }
                }
            }

            MessageBox.Show($"PDF {folder} klasörüne başarıyla görsel olarak dışa aktarıldı!");
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
                    using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
                    using (Document doc = new Document(PageSize.A4))
                    {
                        PdfWriter.GetInstance(doc, stream);
                        doc.Open();
                        foreach (string imgPath in ofd.FileNames)
                        {
                            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(imgPath);
                            img.ScaleToFit(PageSize.A4.Width - 20, PageSize.A4.Height - 20);
                            img.Alignment = Element.ALIGN_CENTER;
                            doc.Add(img);
                            doc.NewPage();
                        }
                        doc.Close();
                    }
                    MessageBox.Show("Görsellerden PDF başarıyla oluşturuldu!");
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

            PdfReader reader = new PdfReader(ofd.FileName);
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
            using (PdfStamper stamper = new PdfStamper(reader, fs))
            {
                PdfContentByte cb = stamper.GetOverContent(1);
                
                // Built-in font kullan (encoding sorunu olmaz)
                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                cb.BeginText();
                cb.SetFontAndSize(baseFont, 18);
                cb.SetTextMatrix(200, 500);
                cb.ShowText(text);
                cb.EndText();
            }
            reader.Close();

            MessageBox.Show("PDF'e metin başarıyla eklendi!");
        }

        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            string pass = Microsoft.VisualBasic.Interaction.InputBox("Şifre girin (boş bırakılırsa şifre kaldırılır):", "PDF Şifreleme");

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            PdfReader reader = new PdfReader(ofd.FileName);
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
            {
                if (string.IsNullOrWhiteSpace(pass))
                {
                    PdfEncryptor.Encrypt(reader, fs, null, null, PdfWriter.ALLOW_PRINTING, false);
                }
                else
                {
                    PdfEncryptor.Encrypt(reader, fs, true, pass, pass, PdfWriter.ALLOW_PRINTING);
                }
            }
            reader.Close();
            MessageBox.Show("İşlem tamamlandı!");
        }
        private void BtnWatermark_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            string watermark = Microsoft.VisualBasic.Interaction.InputBox("Filigran metni girin:", "Filigran", "Gizli");

            PdfReader reader = new PdfReader(ofd.FileName);
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
            using (PdfStamper stamper = new PdfStamper(reader, fs))
            {
                int pageCount = reader.NumberOfPages;
                
                // Built-in font kullan (Türkçe karakter için SYMBOL veya HELVETICA)
                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                
                for (int i = 1; i <= pageCount; i++)
                {
                    PdfContentByte over = stamper.GetOverContent(i);
                    over.SaveState();
                    PdfGState gs = new PdfGState { FillOpacity = 0.2f };
                    over.SetGState(gs);
                    over.BeginText();
                    over.SetFontAndSize(baseFont, 60);
                    over.ShowTextAligned(Element.ALIGN_CENTER, watermark, 300, 400, 45);
                    over.EndText();
                    over.RestoreState();
                }
            }

            reader.Close();
            MessageBox.Show("Filigran başarıyla eklendi!");
        }
        private void BtnCompress_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (sfd.ShowDialog() != true) return;

            PdfReader reader = new PdfReader(ofd.FileName);
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
            {
                PdfStamper stamper = new PdfStamper(reader, fs, PdfWriter.VERSION_1_5);
                stamper.Writer.SetFullCompression();
                stamper.Close();
            }
            reader.Close();

            MessageBox.Show("PDF başarıyla sıkıştırıldı!");
        }
        private void BtnReadText_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Dosyaları|*.pdf" };
            if (ofd.ShowDialog() != true) return;

            PdfReader reader = new PdfReader(ofd.FileName);
            string allText = "";
            for (int i = 1; i <= reader.NumberOfPages; i++)
                allText += PdfTextExtractor.GetTextFromPage(reader, i);

            reader.Close();

            File.WriteAllText(System.IO.Path.ChangeExtension(ofd.FileName, ".txt"), allText);
            MessageBox.Show("PDF metni başarıyla dışa aktarıldı!");
        }
        private void BtnBatchProcess_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "PDF klasörünü seçin"
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            string selectedFolder = dialog.FileName;
            var pdfs = Directory.GetFiles(selectedFolder, "*.pdf");

            foreach (var file in pdfs)
            {
                string output = Path.Combine(selectedFolder, "Compressed_" + Path.GetFileName(file));
                PdfReader reader = new PdfReader(file);
                using (FileStream fs = new FileStream(output, FileMode.Create))
                {
                    PdfStamper stamper = new PdfStamper(reader, fs, PdfWriter.VERSION_1_5);
                    stamper.Writer.SetFullCompression();
                    stamper.Close();
                }
                reader.Close();
            }

            MessageBox.Show($"{pdfs.Length} adet PDF sıkıştırıldı!");
        }
    }
}
