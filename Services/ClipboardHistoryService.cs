using SwissKnifeApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SwissKnifeApp.Services
{
    /// <summary>
    /// Pano geçmişi yönetimi için servis
    /// </summary>
    public class ClipboardHistoryService
    {
        private readonly ObservableCollection<ClipboardItem> _allItems;
        private readonly string _saveFilePath;
        private string _lastText = "";
        private string _lastImageHash = "";

        public ClipboardHistoryService(string saveFileName = "clipboard_history.json")
        {
            _allItems = new ObservableCollection<ClipboardItem>();
            _saveFilePath = saveFileName;
        }

        /// <summary>
        /// Tüm clipboard öğelerini döndürür
        /// </summary>
        public ObservableCollection<ClipboardItem> GetAllItems() => _allItems;

        /// <summary>
        /// Clipboard'u kontrol eder ve yeni öğeler varsa ekler
        /// </summary>
        /// <returns>Yeni öğe eklendiyse true</returns>
        public bool CheckAndAddClipboardContent()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    return HandleTextClipboard();
                }
                else if (Clipboard.ContainsImage())
                {
                    return HandleImageClipboard();
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    return HandleFileDropListClipboard();
                }
            }
            catch
            {
                // Clipboard erişim hataları sessizce yoksayılır
            }

            return false;
        }

        private bool HandleTextClipboard()
        {
            string text = Clipboard.GetText();
            if (text != _lastText)
            {
                _lastText = text;
                string preview = text.Length > 100 ? text[..100] + "..." : text;
                AddClipboardItem("Text", text, preview);
                return true;
            }
            return false;
        }

        private bool HandleImageClipboard()
        {
            var img = Clipboard.GetImage();
            if (img != null)
            {
                string hash = CalculateImageHash(img);
                if (hash != _lastImageHash)
                {
                    _lastImageHash = hash;
                    AddClipboardItem("Image", img, "[Görsel]");
                    return true;
                }
            }
            return false;
        }

        private bool HandleFileDropListClipboard()
        {
            var files = Clipboard.GetFileDropList();
            string joined = string.Join(", ", files.Cast<string>());
            if (joined != _lastText)
            {
                _lastText = joined;
                AddClipboardItem("File", files, joined);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Görsel için basit hash hesaplar (benzersizlik kontrolü için)
        /// </summary>
        private string CalculateImageHash(BitmapSource image)
        {
            try
            {
                int stride = image.PixelWidth * (image.Format.BitsPerPixel / 8);
                byte[] pixels = new byte[stride * image.PixelHeight];
                image.CopyPixels(pixels, stride, 0);

                // İlk birkaç pikselin ortalamasını alarak basit bir hash
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

        /// <summary>
        /// Clipboard öğesi ekler
        /// </summary>
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
        }

        /// <summary>
        /// Seçilen öğeyi panoya kopyalar
        /// </summary>
        public bool CopyItemToClipboard(ClipboardItem item)
        {
            if (item == null) return false;

            try
            {
                switch (item.Type)
                {
                    case "Text":
                        Clipboard.SetText(item.Data?.ToString() ?? "");
                        return true;

                    case "Image":
                        if (item.Data is BitmapSource bitmapSource)
                        {
                            Clipboard.SetImage(bitmapSource);
                            return true;
                        }
                        break;

                    case "File":
                        if (item.Data is StringCollection files)
                        {
                            Clipboard.SetFileDropList(files);
                            return true;
                        }
                        break;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Arama sorgusuna göre filtreler
        /// </summary>
        public IEnumerable<ClipboardItem> FilterItems(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _allItems;

            return _allItems.Where(item => 
                item.Preview?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Tüm öğeleri temizler
        /// </summary>
        public void ClearAll()
        {
            _allItems.Clear();
            _lastText = "";
            _lastImageHash = "";
        }

        /// <summary>
        /// Geçmişi JSON dosyasına kaydeder
        /// </summary>
        public bool SaveToFile()
        {
            try
            {
                // Sadece Text tipindeki öğeleri kaydet (Image ve File serileştirilemez)
                var textItems = _allItems
                    .Where(item => item.Type == "Text")
                    .Select(item => new ClipboardItem
                    {
                        Time = item.Time,
                        Type = item.Type,
                        Preview = item.Preview,
                        Data = item.Data
                    })
                    .ToList();

                var json = JsonSerializer.Serialize(textItems, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_saveFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// JSON dosyasından geçmişi yükler
        /// </summary>
        public bool LoadFromFile()
        {
            try
            {
                if (!File.Exists(_saveFilePath))
                    return false;

                var json = File.ReadAllText(_saveFilePath);
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json);

                if (items == null)
                    return false;

                _allItems.Clear();
                foreach (var item in items)
                {
                    _allItems.Add(item);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Toplam öğe sayısını döndürür
        /// </summary>
        public int GetItemCount() => _allItems.Count;
    }
}
