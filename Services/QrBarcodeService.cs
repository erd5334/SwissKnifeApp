using QRCoder;
using System;
using System.Drawing;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using ZXing.Windows.Compatibility;

namespace SwissKnifeApp.Services
{
    public class QrBarcodeService
    {
        // Payload helpers
        public string CreateWifiPayload(string ssid, string password, PayloadGenerator.WiFi.Authentication auth = PayloadGenerator.WiFi.Authentication.WPA, bool hidden = false)
        {
            var wifi = new PayloadGenerator.WiFi(ssid ?? string.Empty, password ?? string.Empty, auth, hidden);
            return wifi.ToString();
        }

        public string CreateVCardPayload(string firstName, string lastName, string email, string phone)
        {
            var contact = new PayloadGenerator.ContactData(
                PayloadGenerator.ContactData.ContactOutputType.VCard3,
                firstName ?? string.Empty,
                lastName ?? string.Empty,
                null,
                phone ?? string.Empty,
                null,
                email ?? string.Empty,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            return contact.ToString();
        }

        public string CreateMailPayload(string to, string subject, string body)
        {
            var mail = new PayloadGenerator.Mail(to ?? string.Empty, subject ?? string.Empty, body ?? string.Empty);
            return mail.ToString();
        }

        // QR generate
        public Bitmap GenerateQrBitmap(string payload, int pixelsPerModule = 20)
        {
            if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Payload is empty", nameof(payload));
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            // QRCode.GetGraphic returns a new Bitmap which we must clone to avoid disposing when leaving using scope
            using var tmp = qr.GetGraphic(pixelsPerModule);
            return (Bitmap)tmp.Clone();
        }

        // Barcode generate
        public Bitmap GenerateBarcodeBitmap(string text, BarcodeFormat format, int width = 400, int height = 120)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is empty", nameof(text));

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

        // Decode
        public string? DecodeFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            var source = new BitmapLuminanceSource(bitmap);
            var binarizer = new HybridBinarizer(source);
            var binaryBitmap = new BinaryBitmap(binarizer);
            var reader = new MultiFormatReader();
            var result = reader.decode(binaryBitmap);
            return result?.Text;
        }

        public string? DecodeFromFile(string filePath)
        {
            using var bitmap = (Bitmap)Image.FromFile(filePath);
            return DecodeFromBitmap(bitmap);
        }

        // Save
        public void SaveBitmap(Bitmap bitmap, string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var format = ext switch
            {
                ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
                ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };
            bitmap.Save(filePath, format);
        }
    }
}
