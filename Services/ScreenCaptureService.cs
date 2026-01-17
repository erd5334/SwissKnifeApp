using SwissKnifeApp.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace SwissKnifeApp.Services
{
    public class ScreenCaptureService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public CaptureResult CaptureFullScreen(ScreenCaptureSettings settings)
        {
            int width = (int)SystemParameters.PrimaryScreenWidth;
            int height = (int)SystemParameters.PrimaryScreenHeight;
            return CaptureRegion(0, 0, width, height, settings, "FullScreen");
        }

        public CaptureResult CaptureAllScreens(ScreenCaptureSettings settings)
        {
            int left = (int)SystemParameters.VirtualScreenLeft;
            int top = (int)SystemParameters.VirtualScreenTop;
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;

            return CaptureRegion(left, top, width, height, settings, "AllScreens");
        }

        public CaptureResult CaptureActiveWindow(ScreenCaptureSettings settings)
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                throw new Exception("Aktif pencere bulunamadı!");
            }

            if (GetWindowRect(handle, out RECT rect))
            {
                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                return CaptureRegion(rect.Left, rect.Top, width, height, settings, "ActiveWindow");
            }

            throw new Exception("Pencere boyutları alınamadı!");
        }

        private CaptureResult CaptureRegion(int x, int y, int width, int height, ScreenCaptureSettings settings, string captureType)
        {
            using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);

                    if (settings.IncludeCursor)
                    {
                        DrawCursor(graphics, x, y);
                    }
                }

                // Save
                var filePath = GenerateFilePath(settings);
                var format = GetImageFormat(settings.ImageFormat);

                if (settings.ImageFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase) ||
                    settings.ImageFormat.Equals("JPEG", StringComparison.OrdinalIgnoreCase))
                {
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, settings.JpegQuality);
                    var jpegCodec = GetEncoder(ImageFormat.Jpeg);
                    bitmap.Save(filePath, jpegCodec, encoderParams);
                }
                else
                {
                    bitmap.Save(filePath, format);
                }

                var fileInfo = new FileInfo(filePath);

                return new CaptureResult
                {
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Width = width,
                    Height = height,
                    CapturedAt = DateTime.Now,
                    CaptureType = captureType
                };
            }
        }

        private void DrawCursor(Graphics graphics, int offsetX, int offsetY)
        {
            try
            {
                if (GetCursorPos(out POINT lpPoint))
                {
                    var adjustedX = lpPoint.X - offsetX;
                    var adjustedY = lpPoint.Y - offsetY;

                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2))
                    {
                        graphics.DrawEllipse(pen, adjustedX, adjustedY, 10, 10);
                        graphics.DrawLine(pen, adjustedX, adjustedY + 5, adjustedX + 10, adjustedY + 5);
                        graphics.DrawLine(pen, adjustedX + 5, adjustedY, adjustedX + 5, adjustedY + 10);
                    }
                }
            }
            catch
            {
                // Continue if cursor draw fails
            }
        }

        private string GenerateFilePath(ScreenCaptureSettings settings)
        {
            if (!Directory.Exists(settings.SaveDirectory))
            {
                Directory.CreateDirectory(settings.SaveDirectory);
            }

            var fileName = string.Format(settings.FileNameFormat, DateTime.Now);
            return Path.Combine(settings.SaveDirectory, fileName);
        }

        private ImageFormat GetImageFormat(string format)
        {
            return format.ToUpper() switch
            {
                "PNG" => ImageFormat.Png,
                "JPG" or "JPEG" => ImageFormat.Jpeg,
                "BMP" => ImageFormat.Bmp,
                "GIF" => ImageFormat.Gif,
                _ => ImageFormat.Png
            };
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null!;
        }

        public string OpenSaveDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                System.Diagnostics.Process.Start("explorer.exe", directory);
                return directory;
            }
            throw new DirectoryNotFoundException($"Klasör bulunamadı: {directory}");
        }

        public void CopyToClipboard(Bitmap bitmap)
        {
            try
            {
                System.Windows.Clipboard.SetImage(BitmapToImageSource(bitmap));
            }
            catch (Exception ex)
            {
                throw new Exception($"Panoya kopyalama hatası: {ex.Message}");
            }
        }

        public CaptureResult SaveBitmapToFile(Bitmap bitmap, ScreenCaptureSettings settings, string captureType)
        {
            var filePath = GenerateFilePath(settings);
            var format = GetImageFormat(settings.ImageFormat);

            if (settings.ImageFormat.Equals("JPG", StringComparison.OrdinalIgnoreCase) ||
                settings.ImageFormat.Equals("JPEG", StringComparison.OrdinalIgnoreCase))
            {
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, settings.JpegQuality);
                var jpegCodec = GetEncoder(ImageFormat.Jpeg);
                bitmap.Save(filePath, jpegCodec, encoderParams);
            }
            else
            {
                bitmap.Save(filePath, format);
            }

            var fileInfo = new FileInfo(filePath);

            return new CaptureResult
            {
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Width = bitmap.Width,
                Height = bitmap.Height,
                CapturedAt = DateTime.Now,
                CaptureType = captureType
            };
        }

        private System.Windows.Media.Imaging.BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
    }
}
