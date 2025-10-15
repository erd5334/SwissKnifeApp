using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ISSize = SixLabors.ImageSharp.Size;
using ISResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using Svg;
using System.Drawing;
using System.Drawing.Imaging;

namespace SwissKnifeApp.Views.Modules
{
    /// <summary>
    /// Interaction logic for ImageConverterPage.xaml
    /// </summary>
    public partial class ImageConverterPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<ImageItem> Images { get; set; } = new();
        private string _targetFolder = string.Empty;
        public string TargetFolder
        {
            get => _targetFolder;
            set { _targetFolder = value; OnPropertyChanged(nameof(TargetFolder)); }
        }

        public ImageConverterPage()
        {
            InitializeComponent();
            DataContext = this;
            if (FindName("sliderQuality") is Slider s)
            {
                s.ValueChanged += SliderQuality_ValueChanged;
            }
            if (FindName("dgImages") is DataGrid dg)
            {
                dg.ItemsSource = Images;
                dg.SelectionChanged += Dg_SelectionChanged;
            }
        }

        private void Dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (FindName("dgImages") as DataGrid)?.SelectedItem as ImageItem;
            if (item == null) return;
            if (FindName("imgPreview") is System.Windows.Controls.Image preview)
            {
                preview.Source = item.Thumbnail;
            }
        }

        private async void OnSettingsChanged(object sender, RoutedEventArgs e)
        {
            var item = (FindName("dgImages") as DataGrid)?.SelectedItem as ImageItem ?? Images.FirstOrDefault();
            if (item == null) return;
            // Basit canlı önizleme: işlenmiş görüntüyü küçük boyutta üret ve göster
            try
            {
                // Kullanıcı ayarlarına göre dönüştür, ancak önizleme için kalite düşük tutulabilir
                await ConvertImageAsync(item, ((FindName("cmbFormat") as ComboBox)?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "JPG", 75);
                if (item.ConvertedBytes != null && FindName("imgPreview") is System.Windows.Controls.Image preview)
                {
                    using var ms = new MemoryStream(item.ConvertedBytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    preview.Source = bmp;
                }
            }
            catch { }
        }

        private void BtnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp;*.svg;*.ico"
            };
            if (dlg.ShowDialog() == true)
            {
                Images.Clear();
                foreach (var file in dlg.FileNames)
                {
                    Images.Add(new ImageItem(file));
                }
            }
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true,
                Title = "Hedef klasörü seçin"
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TargetFolder = dlg.FileName;
                if (FindName("txtTargetFolder") is TextBox tb)
                    tb.Text = TargetFolder;
            }
        }

        private void SliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var value = (int)e.NewValue;
            if (FindName("txtQualityValue") is TextBlock t)
                t.Text = value.ToString();
        }

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (Images.Count == 0)
            {
                MessageBox.Show("Önce görsel seçin.");
                return;
            }
            string format = "JPG";
            if (FindName("cmbFormat") is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi)
                format = cbi.Content?.ToString() ?? "JPG";
            int quality = 90;
            if (FindName("sliderQuality") is Slider s2)
                quality = (int)s2.Value;
            foreach (var img in Images)
            {
                await ConvertImageAsync(img, format, quality);
            }
            MessageBox.Show("Dönüştürme tamamlandı.");
        }

        private async void BtnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TargetFolder))
            {
                MessageBox.Show("Hedef klasör seçin.");
                return;
            }
            foreach (var img in Images)
            {
                if (img.ConvertedBytes != null)
                {
                    string ext = img.TargetExtension;
                    string outPath = Path.Combine(TargetFolder, Path.GetFileNameWithoutExtension(img.FileName) + ext);
                    await File.WriteAllBytesAsync(outPath, img.ConvertedBytes);
                }
            }
            MessageBox.Show("Tüm dosyalar kaydedildi.");
        }

        private async Task ConvertImageAsync(ImageItem img, string format, int quality)
        {
            // SVG kaynak dosyası kontrolü
            bool isSvgSource = Path.GetExtension(img.FilePath).Equals(".svg", StringComparison.OrdinalIgnoreCase);
            
            // Capture UI values on UI thread
            int width = ParseInt((FindName("txtWidth") as TextBox)?.Text, 0);
            int height = ParseInt((FindName("txtHeight") as TextBox)?.Text, 0);
            int scalePercent = ParseInt((FindName("txtScalePercent") as TextBox)?.Text, 100);
            bool keepAspect = (FindName("chkKeepAspect") as CheckBox)?.IsChecked == true;
            bool isGray = (FindName("chkGrayscale") as CheckBox)?.IsChecked == true;
            bool isInvert = (FindName("chkInvert") as CheckBox)?.IsChecked == true;
            bool isBW = (FindName("chkBW") as CheckBox)?.IsChecked == true;
            int sat = ParseInt((FindName("sliderSaturation") as Slider)?.Value.ToString(), 0);
            int bri = ParseInt((FindName("sliderBrightness") as Slider)?.Value.ToString(), 0);

            await Task.Run(() =>
            {
                SixLabors.ImageSharp.Image<Rgba32> image;
                
                // SVG'den PNG'ye dönüştür, sonra ImageSharp ile yükle
                if (isSvgSource)
                {
                    byte[]? pngBytes = null;
                    try
                    {
                        var svgDoc = SvgDocument.Open(img.FilePath);
                        if (width > 0 && height > 0)
                        {
                            svgDoc.Width = width;
                            svgDoc.Height = height;
                        }
                        using var bitmap = svgDoc.Draw();
                        using var ms = new MemoryStream();
                        bitmap.Save(ms, ImageFormat.Png);
                        pngBytes = ms.ToArray();
                    }
                    catch
                    {
                        return;
                    }
                    
                    if (pngBytes == null) return;
                    image = SixLabors.ImageSharp.Image.Load<Rgba32>(pngBytes);
                }
                else
                {
                    image = SixLabors.ImageSharp.Image.Load<Rgba32>(img.FilePath);
                }

                using (image)
                {
                    if (scalePercent != 100 && scalePercent > 0)
                    {
                        var newW = Math.Max(1, image.Width * scalePercent / 100);
                        var newH = Math.Max(1, image.Height * scalePercent / 100);
                        image.Mutate(x => x.Resize(newW, newH));
                    }
                    else if (width > 0 || height > 0)
                    {
                        var targetW = width > 0 ? width : image.Width;
                        var targetH = height > 0 ? height : image.Height;
                        var options = new ResizeOptions
                        {
                            Size = new ISSize(targetW, targetH),
                            Mode = keepAspect ? ISResizeMode.Max : ISResizeMode.Stretch
                        };
                        image.Mutate(x => x.Resize(options));
                    }

                    image.Mutate(ctx =>
                    {
                        if (isGray) ctx.Grayscale();
                        if (isInvert) ctx.Invert();
                        if (isBW) ctx.BinaryThreshold(0.5f);
                        if (sat != 0) ctx.Saturate(1f + sat / 100f);
                        if (bri != 0) ctx.Brightness(1f + bri / 100f);
                    });

                    // Encoder seçimi ve uzantı uyumu
                    string outExt = GetExtension(format);
                    
                    if (format == "ICO")
                    {
                        // ICO formatı: PNG olarak kaydet ve manuel ICO oluştur
                        using var msIco = new MemoryStream();
                        image.SaveAsPng(msIco);
                        img.TargetExtension = ".ico";
                        img.ConvertedBytes = ConvertToIco(msIco.ToArray());
                    }
                    else if (format == "SVG")
                    {
                        // PNG to SVG: Basit SVG wrapper (gerçek vektörizasyon değil)
                        using var msSvg = new MemoryStream();
                        image.SaveAsPng(msSvg);
                        var base64 = Convert.ToBase64String(msSvg.ToArray());
                        var svgContent = $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""{image.Width}"" height=""{image.Height}"">
  <image width=""{image.Width}"" height=""{image.Height}"" xlink:href=""data:image/png;base64,{base64}""/>
</svg>";
                        img.TargetExtension = ".svg";
                        img.ConvertedBytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
                    }
                    else
                    {
                        // Normal format dönüşümü
                        IImageEncoder encoder = format switch
                        {
                            "JPG" => new JpegEncoder { Quality = Math.Clamp(quality, 0, 100) },
                            "PNG" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                            "BMP" => new BmpEncoder(),
                            "GIF" => new GifEncoder(),
                            "WEBP" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                            _ => new JpegEncoder { Quality = Math.Clamp(quality, 0, 100) }
                        };
                        if (format == "WEBP")
                        {
                            outExt = ".png";
                        }

                        using var ms = new MemoryStream();
                        image.Save(ms, encoder);
                        img.TargetExtension = outExt;
                        img.ConvertedBytes = ms.ToArray();
                    }
                }
            });
        }

        private static int ParseInt(string? text, int fallback)
            => int.TryParse(text, out var v) ? v : fallback;

        private static readonly string[] ImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".svg", ".ico" };

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var paths = files.SelectMany(path => Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                : new[] { path });

            var filtered = paths.Where(p => ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()));
            int added = 0;
            foreach (var f in filtered)
            {
                if (Images.Any(i => string.Equals(i.FilePath, f, StringComparison.OrdinalIgnoreCase)))
                    continue;
                Images.Add(new ImageItem(f));
                added++;
            }
            if (added > 0 && FindName("dgImages") is DataGrid dg)
            {
                dg.Items.Refresh();
            }
        }

        private string GetExtension(string format)
        {
            return format switch
            {
                "JPG" => ".jpg",
                "PNG" => ".png",
                "BMP" => ".bmp",
                "GIF" => ".gif",
                "WEBP" => ".webp",
                "ICO" => ".ico",
                "SVG" => ".svg",
                _ => ".jpg"
            };
        }

        private byte[] ConvertToIco(byte[] pngBytes)
        {
            try
            {
                // Basit ICO formatı: PNG datasını ICO header ile wrap et
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                
                // ICO header
                bw.Write((short)0);     // Reserved
                bw.Write((short)1);     // Type: 1 = ICO
                bw.Write((short)1);     // Image count
                
                // Image directory entry
                using var pngMs = new MemoryStream(pngBytes);
                using var img = System.Drawing.Image.FromStream(pngMs);
                byte width = (byte)(img.Width >= 256 ? 0 : img.Width);
                byte height = (byte)(img.Height >= 256 ? 0 : img.Height);
                
                bw.Write(width);        // Width
                bw.Write(height);       // Height
                bw.Write((byte)0);      // Color palette
                bw.Write((byte)0);      // Reserved
                bw.Write((short)1);     // Color planes
                bw.Write((short)32);    // Bits per pixel
                bw.Write(pngBytes.Length); // Image data size
                bw.Write(22);           // Image data offset (6 + 16)
                
                // PNG data
                bw.Write(pngBytes);
                
                return ms.ToArray();
            }
            catch
            {
                // Hata durumunda orijinal PNG bytes'ı dön
                return pngBytes;
            }
        }

        private async Task<byte[]?> ConvertSvgToPng(string svgPath, int width, int height)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var svgDoc = SvgDocument.Open(svgPath);
                    if (width > 0 && height > 0)
                    {
                        svgDoc.Width = width;
                        svgDoc.Height = height;
                    }
                    using var bitmap = svgDoc.Draw();
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
                catch
                {
                    return null;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ImageItem : INotifyPropertyChanged
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public string SizeText => new FileInfo(FilePath).Length / 1024 + " KB";
        public BitmapImage? Thumbnail { get; set; }
        public byte[]? ConvertedBytes { get; set; }
        public string TargetExtension { get; set; } = ".jpg";

        public ImageItem(string path)
        {
            FilePath = path;
            Thumbnail = GetThumbnail(path);
        }

        private BitmapImage GetThumbnail(string path)
        {
            try
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                
                // SVG için özel işlem
                if (ext == ".svg")
                {
                    var svgDoc = SvgDocument.Open(path);
                    svgDoc.Width = 40;
                    svgDoc.Height = 40;
                    using var bitmap = svgDoc.Draw();
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                
                // Diğer formatlar için normal işlem
                var normalBmp = new BitmapImage();
                normalBmp.BeginInit();
                normalBmp.UriSource = new Uri(path);
                normalBmp.DecodePixelWidth = 40;
                normalBmp.DecodePixelHeight = 40;
                normalBmp.CacheOption = BitmapCacheOption.OnLoad;
                normalBmp.EndInit();
                normalBmp.Freeze();
                return normalBmp;
            }
            catch
            {
                // Hata durumunda boş bir BitmapImage döndür
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
 
