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
    }
}
