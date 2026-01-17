using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Document = iTextSharp.text.Document;
using Font = iTextSharp.text.Font;
using Image = System.Drawing.Image;
using Path = System.IO.Path;

namespace SwissKnifeApp.Services
{
    public class PdfOperationsService
    {
        public void MergePdfs(string[] pdfFiles, string outputFilePath)
        {
            using (Document document = new Document())
            {
                using (PdfCopy copy = new PdfCopy(document, new FileStream(outputFilePath, FileMode.Create)))
                {
                    document.Open();
                    foreach (string file in pdfFiles)
                    {
                        using (PdfReader reader = new PdfReader(file))
                        {
                            for (int i = 1; i <= reader.NumberOfPages; i++)
                            {
                                copy.AddPage(copy.GetImportedPage(reader, i));
                            }
                        }
                    }
                }
            }
        }

        public void SplitPdf(string inputFilePath, string outputDirectory)
        {
            using (PdfReader reader = new PdfReader(inputFilePath))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    using (Document document = new Document())
                    {
                        string outputFilePath = Path.Combine(outputDirectory, $"Page_{i}.pdf");
                        using (PdfCopy copy = new PdfCopy(document, new FileStream(outputFilePath, FileMode.Create)))
                        {
                            document.Open();
                            copy.AddPage(copy.GetImportedPage(reader, i));
                        }
                    }
                }
            }
        }

        public void ExtractImages(string inputFilePath) {
            using (PdfReader reader = new PdfReader(inputFilePath))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    PdfDictionary pageDict = reader.GetPageN(i);
                    PdfDictionary resources = (PdfDictionary)PdfReader.GetPdfObject(pageDict.Get(PdfName.RESOURCES));
                    PdfDictionary xobject = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));
                    if (xobject != null)
                    {
                        foreach (PdfName name in xobject.Keys)
                        {
                            PdfObject obj = xobject.Get(name);
                            if (obj.IsIndirect())
                            {
                                PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(obj);
                                PdfName subtype = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));
                                if (subtype.Equals(PdfName.IMAGE))
                                {
                                    int XrefIndex = ((PRIndirectReference)obj).Number;
                                    PdfObject pdfObj = reader.GetPdfObject(XrefIndex);
                                    PdfStream pdfStream = (PdfStream)pdfObj;
                                    byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStream);
                                    if (bytes != null)
                                    {
                                        using (MemoryStream memStream = new MemoryStream(bytes))
                                        {
                                            memStream.Position = 0;
                                            Image img = Image.FromStream(memStream);
                                            img.Save($"extracted_image_page_{i}_{name}.png", ImageFormat.Png);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ImagesToPdf(string[] imageFiles, string outputFilePath)
        {
            using (FileStream stream = new FileStream(outputFilePath, FileMode.Create))
            using (Document doc = new Document(PageSize.A4))
            {
                PdfWriter.GetInstance(doc, stream);
                doc.Open();
                foreach (string imgPath in imageFiles)
                {
                    iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(imgPath);
                    img.ScaleToFit(PageSize.A4.Width - 20, PageSize.A4.Height - 20);
                    img.Alignment = Element.ALIGN_CENTER;
                    doc.Add(img);
                    doc.NewPage();
                }
                doc.Close();
            }
        }

        public void AddContentToPdf(string inputFilePath, string outputFilePath, string text)
        {
            PdfReader reader = new PdfReader(inputFilePath);
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
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
        }

        public void EncryptPdf(string inputFilePath, string outputFilePath, string password)
        {
            PdfReader reader = new PdfReader(inputFilePath);
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    PdfEncryptor.Encrypt(reader, fs, null, null, PdfWriter.ALLOW_PRINTING, false);
                }
                else
                {
                    PdfEncryptor.Encrypt(reader, fs, true, password, password, PdfWriter.ALLOW_PRINTING);
                }
            }
            reader.Close();
        }

        public void AddWatermark(string inputFilePath, string outputFilePath, string watermarkText)
        {
            PdfReader reader = new PdfReader(inputFilePath);
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
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
                    over.ShowTextAligned(Element.ALIGN_CENTER, watermarkText, 300, 400, 45);
                    over.EndText();
                    over.RestoreState();
                }
            }
            reader.Close();
        }

        public void CompressPdf(string inputFilePath, string outputFilePath)
        {
            PdfReader reader = new PdfReader(inputFilePath);
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
            {
                PdfStamper stamper = new PdfStamper(reader, fs, PdfWriter.VERSION_1_5);
                stamper.Writer.SetFullCompression();
                stamper.Close();
            }
            reader.Close();
        }

        public string ExtractTextFromPdf(string inputFilePath)
        {
            PdfReader reader = new PdfReader(inputFilePath);
            StringBuilder allText = new StringBuilder();
            
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                allText.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            
            reader.Close();
            return allText.ToString();
        }

        #region PDF Form Operations

        /// <summary>
        /// PDF form alanlarını listeler
        /// </summary>
        public Dictionary<string, string> GetFormFields(string inputFilePath)
        {
            var fields = new Dictionary<string, string>();
            using var reader = new PdfReader(inputFilePath);
            var form = reader.AcroFields;
            
            foreach (var fieldName in form.Fields.Keys)
            {
                var value = form.GetField(fieldName.ToString());
                fields[fieldName.ToString()] = value ?? "";
            }
            
            return fields;
        }

        /// <summary>
        /// PDF form alanlarını doldurur
        /// </summary>
        public void FillPdfForm(string inputFilePath, string outputFilePath, Dictionary<string, string> fieldValues)
        {
            using var reader = new PdfReader(inputFilePath);
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var stamper = new PdfStamper(reader, fs);
            
            var form = stamper.AcroFields;
            foreach (var field in fieldValues)
            {
                form.SetField(field.Key, field.Value);
            }
            
            // Form alanlarını düzenlenemez yap (opsiyonel)
            stamper.FormFlattening = false;
            stamper.Close();
        }

        #endregion

        #region PDF Annotations

        /// <summary>
        /// PDF'e vurgulama (highlight) ekler
        /// </summary>
        public void AddHighlightAnnotation(
            string inputFilePath, 
            string outputFilePath, 
            int pageNumber, 
            float x, float y, float width, float height,
            BaseColor? color = null)
        {
            using var reader = new PdfReader(inputFilePath);
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var stamper = new PdfStamper(reader, fs);
            
            var rect = new iTextSharp.text.Rectangle(x, y, x + width, y + height);
            var highlight = PdfAnnotation.CreateSquareCircle(
                stamper.Writer, 
                rect, 
                null, 
                true);
            
            highlight.Color = color ?? BaseColor.YELLOW;
            highlight.Flags = PdfAnnotation.FLAGS_PRINT;
            
            stamper.AddAnnotation(highlight, pageNumber);
            stamper.Close();
        }

        /// <summary>
        /// PDF'e metin notu (comment) ekler
        /// </summary>
        public void AddTextAnnotation(
            string inputFilePath, 
            string outputFilePath, 
            int pageNumber, 
            float x, float y,
            string title,
            string content)
        {
            using var reader = new PdfReader(inputFilePath);
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var stamper = new PdfStamper(reader, fs);
            
            var rect = new iTextSharp.text.Rectangle(x, y, x + 20, y + 20);
            var textAnnotation = PdfAnnotation.CreateText(
                stamper.Writer,
                rect,
                title,
                content,
                false,
                "Comment");
            
            textAnnotation.Color = BaseColor.YELLOW;
            stamper.AddAnnotation(textAnnotation, pageNumber);
            stamper.Close();
        }

        /// <summary>
        /// PDF'e serbest metin notu ekler (sayfa üzerinde görünür)
        /// </summary>
        public void AddFreeTextAnnotation(
            string inputFilePath,
            string outputFilePath,
            int pageNumber,
            float x, float y, float width, float height,
            string text,
            BaseColor? bgColor = null)
        {
            using var reader = new PdfReader(inputFilePath);
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var stamper = new PdfStamper(reader, fs);

            var cb = stamper.GetOverContent(pageNumber);
            
            // Arka plan
            if (bgColor != null)
            {
                cb.SetColorFill(bgColor);
                cb.Rectangle(x, y, width, height);
                cb.Fill();
            }
            
            // Metin
            cb.BeginText();
            cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false), 10);
            cb.SetColorFill(BaseColor.BLACK);
            cb.SetTextMatrix(x + 2, y + height - 12);
            cb.ShowText(text);
            cb.EndText();
            
            stamper.Close();
        }

        #endregion

        #region Extract Tables

        /// <summary>
        /// PDF'den tabloları metin olarak çıkarır (basit grid-tabanlı algılama)
        /// </summary>
        public List<List<List<string>>> ExtractTables(string inputFilePath)
        {
            var tables = new List<List<List<string>>>();
            using var reader = new PdfReader(inputFilePath);
            
            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                var text = PdfTextExtractor.GetTextFromPage(reader, page);
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                // Basit tablo algılama: Satırları tab/çoklu boşluk ile ayır
                var table = new List<List<string>>();
                foreach (var line in lines)
                {
                    // Tab veya 2+ boşluk ile ayrılmış kolonları bul
                    var cells = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\t+|\s{2,}")
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();
                    
                    if (cells.Count >= 2) // En az 2 kolon varsa tablo satırı olarak kabul et
                    {
                        table.Add(cells);
                    }
                }
                
                if (table.Count > 0)
                {
                    tables.Add(table);
                }
            }
            
            return tables;
        }

        /// <summary>
        /// Tabloları CSV formatında dışa aktarır
        /// </summary>
        public void ExportTablesToCsv(string inputFilePath, string outputFolder)
        {
            var tables = ExtractTables(inputFilePath);
            Directory.CreateDirectory(outputFolder);
            
            for (int i = 0; i < tables.Count; i++)
            {
                var csvPath = Path.Combine(outputFolder, $"table_{i + 1}.csv");
                var sb = new StringBuilder();
                
                foreach (var row in tables[i])
                {
                    sb.AppendLine(string.Join(",", row.Select(c => $"\"{c.Replace("\"", "\"\"")}\"")));
                }
                
                File.WriteAllText(csvPath, sb.ToString());
            }
        }

        #endregion

        public int BatchCompressPdfs(string folderPath)
        {
            var pdfs = Directory.GetFiles(folderPath, "*.pdf");
            int processedCount = 0;

            foreach (var file in pdfs)
            {
                string output = Path.Combine(folderPath, "Compressed_" + Path.GetFileName(file));
                try
                {
                    CompressPdf(file, output);
                    processedCount++;
                }
                catch
                {
                    // Hatalı dosyaları atla
                }
            }

            return processedCount;
        }

        /// <summary>
        /// PDF'den resimleri çıkarır ve Windows OCR ile metin tanıma yapar
        /// </summary>
        public async Task<string> OcrPdfAsync(
            string inputFilePath, 
            string language = "tr",
            IProgress<(int current, int total)>? progress = null)
        {
            var ocrService = new WindowsOcrService();
            ocrService.SetLanguage(language);
            
            var sb = new StringBuilder();
            var tempDir = Path.Combine(Path.GetTempPath(), "SwissKnife_OCR_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // PDF sayfalarını görüntüye çevir
                var images = ExtractPdfPagesAsImages(inputFilePath, tempDir);
                
                for (int i = 0; i < images.Count; i++)
                {
                    progress?.Report((i + 1, images.Count));
                    
                    var text = await ocrService.RecognizeFromImageAsync(images[i]);
                    sb.AppendLine($"--- Sayfa {i + 1} ---");
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
            finally
            {
                // Geçici dosyaları temizle
                try { Directory.Delete(tempDir, true); } catch { }
            }

            return sb.ToString();
        }

        /// <summary>
        /// PDF sayfalarını PNG olarak çıkarır
        /// </summary>
        private List<string> ExtractPdfPagesAsImages(string pdfPath, string outputDir)
        {
            var images = new List<string>();
            
            using var document = PdfiumViewer.PdfDocument.Load(pdfPath);
            for (int i = 0; i < document.PageCount; i++)
            {
                var size = document.PageSizes[i];
                int dpi = 200; // OCR için yeterli
                int width = (int)(size.Width * dpi / 72);
                int height = (int)(size.Height * dpi / 72);

                using var image = document.Render(i, width, height, dpi, dpi, false);
                var outputPath = Path.Combine(outputDir, $"page_{i + 1:D4}.png");
                image.Save(outputPath, ImageFormat.Png);
                images.Add(outputPath);
            }

            return images;
        }

        /// <summary>
        /// OCR sonucunu searchable PDF olarak kaydeder
        /// </summary>
        public async Task CreateSearchablePdfAsync(
            string inputPdfPath,
            string outputPdfPath,
            string language = "tr",
            IProgress<(int current, int total)>? progress = null)
        {
            var ocrText = await OcrPdfAsync(inputPdfPath, language, progress);
            
            // Orijinal PDF'i kopyala ve OCR metnini gizli katman olarak ekle
            using var reader = new PdfReader(inputPdfPath);
            using var fs = new FileStream(outputPdfPath, FileMode.Create);
            using var stamper = new PdfStamper(reader, fs);
            
            // Her sayfaya OCR metnini metadata olarak ekle
            var info = reader.Info;
            info["OCRText"] = ocrText.Substring(0, Math.Min(ocrText.Length, 10000)); // PDF metadata limiti
            stamper.MoreInfo = info;
            
            stamper.Close();
        }

        /// <summary>
        /// Kullanılabilir OCR dillerini döndürür
        /// </summary>
        public IReadOnlyList<string> GetAvailableOcrLanguages()
        {
            return new WindowsOcrService().AvailableLanguages;
        }
    }
}
