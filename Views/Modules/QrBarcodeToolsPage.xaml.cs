using Microsoft.Win32;
using QRCoder;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;

namespace SwissKnifeApp.Views.Modules
{
    public partial class QrBarcodeToolsPage : Page
    {
        public QrBarcodeToolsPage()
        {
            InitializeComponent();
            // LoadDefaultFields çağrısını buradan kaldır
            Loaded += QrBarcodeToolsPage_Loaded;
        }

        private void QrBarcodeToolsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Sayfa tamamen yüklendikten sonra default alanları yükle
            LoadDefaultFields();
        }

        // 🔸 QR Türüne göre alanları oluştur
        private void CmbQrType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDefaultFields();
        }

        private void LoadDefaultFields()
        {
            // Null kontrolü ekle
            if (PanelInputFields == null || CmbQrType == null)
                return;

            PanelInputFields.Children.Clear();
            
            if (CmbQrType.SelectedItem == null) return;
            
            string selected = ((ComboBoxItem)CmbQrType.SelectedItem).Content.ToString();

            switch (selected)
            {
                case "Wi-Fi":
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Wi-Fi Adı (SSID):", "TxtSSID"));
                    PanelInputFields.Children.Add(CreateLabeledPasswordBox("Şifre:", "TxtPassword"));
                    break;

                case "vCard":
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Ad:", "TxtName"));
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Soyad:", "TxtSurname"));
                    PanelInputFields.Children.Add(CreateLabeledTextBox("E-posta:", "TxtEmail"));
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Telefon:", "TxtPhone"));
                    break;

                case "E-posta":
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Alıcı:", "TxtTo"));
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Konu:", "TxtSubject"));
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Mesaj:", "TxtBody"));
                    break;

                default:
                    PanelInputFields.Children.Add(CreateLabeledTextBox("Metin/URL:", "TxtPlainText"));
                    break;
            }
        }

        private StackPanel CreateLabeledTextBox(string label, string name)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 2) });
            panel.Children.Add(new TextBox { Name = name, Height = 25 });
            return panel;
        }

        private StackPanel CreateLabeledPasswordBox(string label, string name)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 2) });
            panel.Children.Add(new PasswordBox { Name = name, Height = 25 });
            return panel;
        }

        // 🔹 QR oluştur
        private void BtnGenerateQR_Click(object sender, RoutedEventArgs e)
        {
            if (CmbQrType?.SelectedItem == null) return;
            
            string selected = ((ComboBoxItem)CmbQrType.SelectedItem).Content.ToString();
            string payload = string.Empty;

            try
            {
                switch (selected)
                {
                    case "Wi-Fi":
                        string ssid = GetTextBoxValue(0);
                        string password = GetPasswordBoxValue(1); // Wi-Fi şifresi 1. index'te (PasswordBox)
                        var wifiGenerator = new PayloadGenerator.WiFi(ssid, password, PayloadGenerator.WiFi.Authentication.WPA, false);
                        payload = wifiGenerator.ToString();
                        break;

                    case "vCard":
                        string firstName = GetTextBoxValue(0);
                        string lastName = GetTextBoxValue(1);
                        string emailAddr = GetTextBoxValue(2);
                        string phone = GetTextBoxValue(3);
                        
                        // QRCoder'da ContactData kullanılır
                        var contactData = new PayloadGenerator.ContactData(
                            PayloadGenerator.ContactData.ContactOutputType.VCard3,
                            firstName,
                            lastName,
                            null, // nickname
                            phone,
                            null, // mobile
                            emailAddr,
                            null, // birthday
                            null, // website
                            null, // street
                            null, // houseNumber
                            null, // city
                            null, // zipCode
                            null, // country
                            null  // note
                        );
                        payload = contactData.ToString();
                        break;

                    case "E-posta":
                        string toAddress = GetTextBoxValue(0);
                        string subject = GetTextBoxValue(1);
                        string body = GetTextBoxValue(2);
                        var mailGenerator = new PayloadGenerator.Mail(toAddress, subject, body);
                        payload = mailGenerator.ToString();
                        break;

                    default:
                        payload = GetTextBoxValue(0);
                        break;
                }

                if (string.IsNullOrWhiteSpace(payload))
                {
                    MessageBox.Show("Lütfen gerekli alanları doldurun!");
                    return;
                }

                using var qrGen = new QRCodeGenerator();
                using var data = qrGen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using var qr = new QRCode(data);
                using var bmp = qr.GetGraphic(20);
                ImgQrCode.Source = BitmapToImageSource(bmp);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"QR oluşturulamadı: {ex.Message}");
            }
        }

        private string GetTextBoxValue(int index)
        {
            if (PanelInputFields == null || index >= PanelInputFields.Children.Count)
                return string.Empty;

            if (PanelInputFields.Children[index] is StackPanel panel && 
                panel.Children.Count > 1 &&
                panel.Children[1] is TextBox textBox)
            {
                return textBox.Text ?? string.Empty;
            }
            return string.Empty;
        }

        private string GetPasswordBoxValue(int index)
        {
            if (PanelInputFields == null || index >= PanelInputFields.Children.Count)
                return string.Empty;

            if (PanelInputFields.Children[index] is StackPanel panel && 
                panel.Children.Count > 1 &&
                panel.Children[1] is PasswordBox passwordBox)
            {
                return passwordBox.Password ?? string.Empty;
            }
            return string.Empty;
        }

        // 🔹 QR Kaydet
        private void BtnSaveQR_Click(object sender, RoutedEventArgs e)
        {
            if (ImgQrCode?.Source == null)
            {
                MessageBox.Show("Önce bir QR kod oluşturun!");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PNG Dosyası|*.png|JPEG Dosyası|*.jpg|BMP Dosyası|*.bmp",
                FileName = "qr_code.png"
            };

            if (sfd.ShowDialog() == true)
            {
                SaveImageSource(ImgQrCode.Source, sfd.FileName);
                MessageBox.Show("QR kod başarıyla kaydedildi!");
            }
        }

        // 🔹 Barkod Kaydet
        private void BtnSaveBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (ImgBarcode?.Source == null)
            {
                MessageBox.Show("Önce bir barkod oluşturun!");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PNG Dosyası|*.png|JPEG Dosyası|*.jpg|BMP Dosyası|*.bmp",
                FileName = "barcode.png"
            };

            if (sfd.ShowDialog() == true)
            {
                SaveImageSource(ImgBarcode.Source, sfd.FileName);
                MessageBox.Show("Barkod başarıyla kaydedildi!");
            }
        }

        // 🔹 Görselden QR/Barkod oku
        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Görseller|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (ofd.ShowDialog() != true) return;

            using var bitmap = (Bitmap)System.Drawing.Image.FromFile(ofd.FileName);
            if (ImgQrCode != null)
                ImgQrCode.Source = BitmapToImageSource(bitmap);

            try
            {
                var source = new BitmapLuminanceSource(bitmap);
                var binarizer = new HybridBinarizer(source);
                var binaryBitmap = new BinaryBitmap(binarizer);

                var reader = new MultiFormatReader();
                var result = reader.decode(binaryBitmap);

                if (TxtDecoded != null)
                    TxtDecoded.Text = result != null ? $"✅ Okunan Veri: {result.Text}" : "⚠️ Kod okunamadı.";
            }
            catch (Exception ex)
            {
                if (TxtDecoded != null)
                    TxtDecoded.Text = $"Hata: {ex.Message}";
            }
        }

        // 🔹 Barkod oluştur
        private void BtnGenerateBarcode_Click(object sender, RoutedEventArgs e)
        {
            string text = TxtBarcodeText?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Barkod metni giriniz.");
                return;
            }

            if (CmbBarcodeFormat?.SelectedItem == null) return;

            string formatStr = ((ComboBoxItem)CmbBarcodeFormat.SelectedItem).Content.ToString();
            BarcodeFormat format = formatStr switch
            {
                "EAN_13" => BarcodeFormat.EAN_13,
                "UPC_A" => BarcodeFormat.UPC_A,
                "CODE_39" => BarcodeFormat.CODE_39,
                "ITF" => BarcodeFormat.ITF,
                _ => BarcodeFormat.CODE_128
            };

            try
            {
                using var bmp = GenerateBarcodeBitmap(text, format, 600, 200);
                if (ImgBarcode != null)
                    ImgBarcode.Source = BitmapToImageSource(bmp);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Barkod oluşturulamadı: {ex.Message}");
            }
        }

        // 🔸 Barkod oluşturma yardımcı
        private Bitmap GenerateBarcodeBitmap(string text, BarcodeFormat format, int width = 400, int height = 120)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 10,
                    PureBarcode = false
                },
                Renderer = new PixelDataRenderer()
            };

            var pixelData = writer.Write(text);
            var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            return bitmap;
        }

        // 🔸 Bitmap -> ImageSource dönüştürücü
        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using MemoryStream ms = new();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            BitmapImage img = new();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }

        // 🔸 ImageSource'u dosyaya kaydet
        private void SaveImageSource(System.Windows.Media.ImageSource imageSource, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imageSource));
            using var fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);
        }
    }
}
