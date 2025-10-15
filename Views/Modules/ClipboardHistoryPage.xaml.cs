using SwissKnifeApp.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SwissKnifeApp.Views.Modules
{
    public partial class ClipboardHistoryPage : Page
    {
        private ObservableCollection<ClipboardItem> _allItems = new();
        private ObservableCollection<ClipboardItem> _filteredItems = new();
        private string _saveFile = "clipboard_history.json";
        private string _lastText = "";

        public ClipboardHistoryPage()
        {
            InitializeComponent();
            ClipboardList.ItemsSource = _filteredItems;
            StartClipboardWatcher();
        }

        private void StartClipboardWatcher()
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => CheckClipboard();
            timer.Start();
        }

        private string _lastImageHash = "";

        private void CheckClipboard()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (text != _lastText)
                    {
                        _lastText = text;
                        AddClipboardItem("Text", text, text.Length > 100 ? text[..100] + "..." : text);
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    var img = Clipboard.GetImage();
                    if (img != null)
                    {
                        string hash = GetImageHash(img);
                        if (hash != _lastImageHash)
                        {
                            _lastImageHash = hash;
                            AddClipboardItem("Image", img, "[Görsel]");
                        }
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    string joined = string.Join(", ", files.Cast<string>());
                    if (joined != _lastText)
                    {
                        _lastText = joined;
                        AddClipboardItem("File", files, joined);
                    }
                }
            }
            catch { }
        }

        private string GetImageHash(BitmapSource image)
        {
            try
            {
                int stride = image.PixelWidth * (image.Format.BitsPerPixel / 8);
                byte[] pixels = new byte[stride * image.PixelHeight];
                image.CopyPixels(pixels, stride, 0);

                // sadece ilk birkaç pikselin ortalamasını alarak basit bir hash
                int sampleSize = Math.Min(pixels.Length, 5000);
                int sum = 0;
                for (int i = 0; i < sampleSize; i++)
                    sum += pixels[i];

                return $"{image.PixelWidth}x{image.PixelHeight}_{sum % 100000}";
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }


        private void AddClipboardItem(string type, object data, string preview)
        {
            var item = new ClipboardItem
            {
                Time = DateTime.Now,
                Type = type,
                Data = data,
                Preview = preview
            };
            _allItems.Insert(0, item);
            ApplySearchFilter(SearchBox.Text);
        }

        private void ClipboardList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClipboardList.SelectedItem is ClipboardItem item)
            {
                switch (item.Type)
                {
                    case "Text":
                        Clipboard.SetText(item.Data.ToString());
                        break;
                    case "Image":
                        Clipboard.SetImage((BitmapSource)item.Data);
                        break;
                    case "File":
                        // FileDropList tipi için ek işlem yapılabilir
                        break;
                }
                MessageBox.Show("Panoya kopyalandı.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClipboardList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClipboardList.SelectedItem is ClipboardItem item && item.Type == "Image")
            {
                PopupImage.Source = (BitmapSource)item.Data;
                ImagePopup.IsOpen = true;
            }
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            ImagePopup.IsOpen = false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _allItems.Clear();
            _filteredItems.Clear();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var json = JsonSerializer.Serialize(_allItems);
            File.WriteAllText(_saveFile, json);
            MessageBox.Show("Geçmiş kaydedildi.");
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_saveFile)) return;

            var items = JsonSerializer.Deserialize<ObservableCollection<ClipboardItem>>(File.ReadAllText(_saveFile));
            _allItems.Clear();
            foreach (var item in items)
                _allItems.Add(item);

            ApplySearchFilter(SearchBox.Text);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => CheckClipboard();

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter(SearchBox.Text);
        }

        private void ApplySearchFilter(string query)
        {
            _filteredItems.Clear();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allItems
                : new ObservableCollection<ClipboardItem>(_allItems
                    .Where(i => i.Preview.Contains(query, StringComparison.OrdinalIgnoreCase)));

            foreach (var item in filtered)
                _filteredItems.Add(item);
        }
    }

    public class ClipboardItem
    {
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string Preview { get; set; }
        public object Data { get; set; }
    }
}
