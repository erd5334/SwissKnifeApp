using ImageMagick;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SwissKnifeApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SwissKnifeApp.Views.Modules
{
    public partial class ImageToolsPage : Page
    {
        public ObservableCollection<AdvImageItem> ImageList { get; } = new();
        private string _targetFolder = "";
        private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imagetools_settings.json");

        public ImageToolsPage()
        {
            InitializeComponent();
            DgImages.ItemsSource = ImageList;
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (settings != null && settings.ContainsKey("RemoveBgKey"))
                    {
                        PbRemoveBgKey.Password = settings["RemoveBgKey"];
                    }
                }
            }
            catch { /* Ayarlar yüklenemezse sessizce devam et */ }
        }

        private void SaveSettings(string apiKey)
        {
            try
            {
                var settings = new Dictionary<string, string> { { "RemoveBgKey", apiKey } };
                var json = System.Text.Json.JsonSerializer.Serialize(settings);
                File.WriteAllText(_settingsPath, json);
            }
            catch { /* Ayarlar kaydedilemezse sessizce devam et */ }
        }

        private void BtnAddImages_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Görsel Dosyaları|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif;*.tiff"
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var path in dlg.FileNames)
                {
                    if (!ImageList.Any(x => x.Path == path))
                        ImageList.Add(new AdvImageItem(path));
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ImageList.Clear();
            ImgPreview.Source = null;
            IcMetadata.ItemsSource = null;
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _targetFolder = dlg.FileName;
                TxtOutputPath.Text = _targetFolder;
            }
        }

        private void DgImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgImages.SelectedItem is AdvImageItem item)
            {
                ImgPreview.Source = item.Thumbnail;
                LoadMetadata(item);
            }
        }

        private void LoadMetadata(AdvImageItem item)
        {
            try
            {
                using var image = new MagickImage(item.Path);
                var meta = new List<KeyValuePair<string, string>>();
                meta.Add(new KeyValuePair<string, string>("Boyut", $"{image.Width} x {image.Height} px"));
                meta.Add(new KeyValuePair<string, string>("Format", image.Format.ToString()));
                
                var profile = image.GetExifProfile();
                if (profile != null)
                {
                    foreach (var value in profile.Values.Take(15))
                    {
                        meta.Add(new KeyValuePair<string, string>(value.Tag.ToString(), value.GetValue()?.ToString() ?? ""));
                    }
                }
                IcMetadata.ItemsSource = meta;
            }
            catch { IcMetadata.ItemsSource = null; }
        }

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ImageList.Count == 0) return;
            if (string.IsNullOrEmpty(_targetFolder))
            {
                MessageBox.Show("Lütfen önce hedef klasörü seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PrProcessing.Visibility = Visibility.Visible;
            
            // Capture Settings
            string format = (CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "JPG";
            int quality = (int)SldQuality.Value;
            bool optimize = ChkOptimize.IsChecked == true;
            bool strip = ChkStripMetadata.IsChecked == true;
            
            int targetWidth = 0, targetHeight = 0;
            int.TryParse(TxtWidth.Text, out targetWidth);
            int.TryParse(TxtHeight.Text, out targetHeight);
            bool keepAspect = ChkKeepAspect.IsChecked == true;
            bool doUpscale = ChkUpscale.IsChecked == true;

            string wmType = (CmbWatermarkType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Metin";
            string wmText = TxtWatermarkText.Text;
            float wmOpacity = (float)SldWatermarkOpacity.Value;
            double wmFontSize = SldWatermarkFontSize.Value;
            string wmColorHex = (CmbWatermarkColor.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#FFFFFF";
            string wmPos = (CmbWatermarkPos.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "BottomRight";

            await Task.Run(async () =>
            {
                foreach (var item in ImageList)
                {
                    try
                    {
                        using var image = new MagickImage(item.Path);

                        // 1. Resize / Upscale
                        if (doUpscale)
                        {
                            image.FilterType = FilterType.Lanczos;
                            image.Resize(new Percentage(200));
                            image.Sharpen();
                        }
                        else if (targetWidth > 0 || targetHeight > 0)
                        {
                            var geo = new MagickGeometry((uint)targetWidth, (uint)targetHeight);
                            if (keepAspect) geo.IgnoreAspectRatio = false;
                            image.Resize(geo);
                        }

                        // 2. Metadata Strip
                        if (strip) image.Strip();

                        // 3. Watermark (Simple implementation with Magick.NET)
                        if (!string.IsNullOrEmpty(wmText) && wmType == "Metin")
                        {
                            image.Settings.FontPointsize = wmFontSize;
                            image.Settings.FillColor = new MagickColor(wmColorHex);
                            image.Settings.FillColor = new MagickColor(
                                image.Settings.FillColor.R, 
                                image.Settings.FillColor.G, 
                                image.Settings.FillColor.B, 
                                (ushort)(wmOpacity * 65535));
                            
                            // Yazının her renkte görünmesi için ince bir kenarlık ekliyoruz (zıt renk)
                            image.Settings.StrokeColor = wmColorHex == "#FFFFFF" ? new MagickColor("#000000") : new MagickColor("#FFFFFF");
                            image.Settings.StrokeColor = new MagickColor(
                                image.Settings.StrokeColor.R,
                                image.Settings.StrokeColor.G,
                                image.Settings.StrokeColor.B,
                                (ushort)(wmOpacity * 65535));
                            image.Settings.StrokeWidth = 1;
                            
                            image.Settings.TextGravity = wmPos switch {
                                "BottomRight" => Gravity.Southeast,
                                "Center" => Gravity.Center,
                                "BottomLeft" => Gravity.Southwest,
                                "TopRight" => Gravity.Northeast,
                                _ => Gravity.Southeast
                            };

                            // Windows sistemlerinde standart fontu zorluyoruz
                            image.Settings.Font = "Arial";
                            
                            image.Annotate(wmText, image.Settings.TextGravity);
                        }

                        // 4. Save & Optimize
                        string fileName = Path.GetFileNameWithoutExtension(item.Name) + GetExt(format);
                        string fullPath = Path.Combine(_targetFolder, fileName);

                        image.Format = format switch
                        {
                            "JPG" => MagickFormat.Jpg,
                            "PNG" => MagickFormat.Png,
                            "WEBP" => MagickFormat.WebP,
                            "BMP" => MagickFormat.Bmp,
                            "GIF" => MagickFormat.Gif,
                            "ICO" => MagickFormat.Ico,
                            _ => MagickFormat.Jpg
                        };
                        image.Quality = (uint)quality;

                        if (optimize)
                        {
                            var optimizer = new ImageOptimizer();
                            // Note: Optimize works on files
                            image.Write(fullPath);
                            optimizer.LosslessCompress(fullPath);
                        }
                        else
                        {
                            image.Write(fullPath);
                        }

                        Application.Current.Dispatcher.Invoke(() => item.Status = "Tamamlandı");
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() => item.Status = "Hata: " + ex.Message);
                    }
                }
            });

            PrProcessing.Visibility = Visibility.Collapsed;
            MessageBox.Show("İşlem tamamlandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetExt(string f) => f switch { "JPG" => ".jpg", "PNG" => ".png", "WEBP" => ".webp", "BMP" => ".bmp", "GIF" => ".gif", "ICO" => ".ico", _ => ".jpg" };

        private async void BtnRemoveBg_Click(object sender, RoutedEventArgs e)
        {
            if (DgImages.SelectedItem is not AdvImageItem item) return;
            string apiKey = PbRemoveBgKey.Password;
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Lütfen Remove.bg API anahtarınızı girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PrProcessing.Visibility = Visibility.Visible;
            SaveSettings(apiKey); // Başarılı olsun olmasın girilen anahtarı kaydet
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

                using var form = new MultipartFormDataContent();
                form.Add(new ByteArrayContent(File.ReadAllBytes(item.Path)), "image_file", "image.jpg");
                form.Add(new StringContent("auto"), "size");

                var response = await client.PostAsync("https://api.remove.bg/v1.0/removebg", form);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();
                    
                    // Eğer kullanıcı bir hedef klasör seçmişse oraya, seçmemişse orijinal dosyanın yanına kaydet
                    string saveDirectory = !string.IsNullOrEmpty(_targetFolder) ? _targetFolder : Path.GetDirectoryName(item.Path);
                    string outPath = Path.Combine(saveDirectory, Path.GetFileNameWithoutExtension(item.Name) + "_no_bg.png");
                    
                    File.WriteAllBytes(outPath, data);
                    MessageBox.Show($"Arka plan başarıyla kaldırıldı ve kaydedildi:\n{outPath}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("API Hatası: " + response.ReasonPhrase);
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
            PrProcessing.Visibility = Visibility.Collapsed;
        }
    }

    public class AdvImageItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Name => System.IO.Path.GetFileName(Path);
        public string OriginalSizeText => new FileInfo(Path).Length / 1024 + " KB";
        
        private string _status = "Bekliyor";
        public string Status 
        { 
            get => _status; 
            set { _status = value; OnPropertyChanged(); } 
        }

        public BitmapImage Thumbnail { get; }

        public AdvImageItem(string path)
        {
            Path = path;
            Thumbnail = CreateThumbnail(path);
        }

        private BitmapImage CreateThumbnail(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = 100;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
