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
    public class ClipboardHistoryService
    {
        private readonly ObservableCollection<ClipboardItem> _allItems;
        private readonly string _saveFilePath;
        private string _lastText = "";
        private string _lastImageHash = "";

        public ClipboardHistoryService(string saveFileName = "clipboard_history_pro.json")
        {
            _allItems = new ObservableCollection<ClipboardItem>();
            _saveFilePath = saveFileName;
        }

        public ObservableCollection<ClipboardItem> GetAllItems() => _allItems;

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
                // Silently ignore
            }

            return false;
        }

        private bool HandleTextClipboard()
        {
            string text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            if (text != _lastText)
            {
                _lastText = text;
                string preview = text.Length > 100 ? text[..100].Replace("\r\n", " ") + "..." : text.Replace("\r\n", " ");
                AddClipboardItem("Text", text, preview, text);
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
                    AddClipboardItem("Image", img, "[Görsel]", "");
                    return true;
                }
            }
            return false;
        }

        private bool HandleFileDropListClipboard()
        {
            var files = Clipboard.GetFileDropList();
            if (files.Count == 0) return false;

            string joined = string.Join(", ", files.Cast<string>());
            if (joined != _lastText)
            {
                _lastText = joined;
                AddClipboardItem("File", files, joined, joined);
                return true;
            }
            return false;
        }

        private string CalculateImageHash(BitmapSource image)
        {
            try
            {
                return $"{image.PixelWidth}x{image.PixelHeight}_{image.Format}";
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }

        private void AddClipboardItem(string type, object data, string preview, string fullText)
        {
            var item = new ClipboardItem
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.Now,
                Type = type,
                Data = data,
                Preview = preview,
                FullText = fullText,
                IsPinned = false
            };
            
            // Eğer aynı içerik varsa (pinned değilse) eskisini silebiliriz veya üste taşıyabiliriz
            var existing = _allItems.FirstOrDefault(x => x.Type == type && x.FullText == fullText && !x.IsPinned);
            if (existing != null) _allItems.Remove(existing);

            _allItems.Insert(0, item);
            
            // Limit history size to 100 items (excluding pinned)
            if (_allItems.Count(x => !x.IsPinned) > 100)
            {
                var last = _allItems.LastOrDefault(x => !x.IsPinned);
                if (last != null) _allItems.Remove(last);
            }
        }

        public void TogglePin(ClipboardItem item)
        {
            if (item == null) return;
            item.IsPinned = !item.IsPinned;
            
            // Pinlendiğinde en üste al, unpinlendiğinde zaman sırasına göre kalsın
            _allItems.Remove(item);
            if (item.IsPinned)
                _allItems.Insert(0, item);
            else
            {
                // Zaman sırasına göre uygun yere koy
                int index = 0;
                while (index < _allItems.Count && (_allItems[index].IsPinned || _allItems[index].Time > item.Time))
                {
                    index++;
                }
                _allItems.Insert(index, item);
            }
            SaveToFile();
        }

        public bool CopyItemToClipboard(ClipboardItem item)
        {
            if (item == null) return false;
            try
            {
                switch (item.Type)
                {
                    case "Text":
                        _lastText = item.FullText; // Kendi eklediğimizi tekrar yakalamamak için
                        Clipboard.SetText(item.FullText);
                        return true;
                    case "Image":
                        _lastImageHash = CalculateImageHash((BitmapSource)item.Data);
                        Clipboard.SetImage((BitmapSource)item.Data);
                        return true;
                    case "File":
                        Clipboard.SetFileDropList((StringCollection)item.Data);
                        return true;
                }
            }
            catch { return false; }
            return false;
        }

        public IEnumerable<ClipboardItem> FilterItems(string query, bool onlyPinned = false, bool onlyTemplates = false)
        {
            var items = _allItems.AsEnumerable();
            
            if (onlyPinned) items = items.Where(x => x.IsPinned);
            if (onlyTemplates) items = items.Where(x => x.IsTemplate);

            if (string.IsNullOrWhiteSpace(query))
                return items;

            return items.Where(item => 
                (item.Preview?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                (item.FullText?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));
        }

        public void ClearHistory()
        {
            var toRemove = _allItems.Where(x => !x.IsPinned && !x.IsTemplate).ToList();
            foreach (var item in toRemove) _allItems.Remove(item);
            _lastText = "";
            _lastImageHash = "";
        }

        public bool SaveToFile()
        {
            try
            {
                // Sadece Pinned, Template ve son 20 Text itemi kaydet
                var saveList = _allItems
                    .Where(x => x.IsPinned || x.IsTemplate || x.Type == "Text")
                    .Take(100)
                    .Select(x => new ClipboardItem
                    {
                        Id = x.Id,
                        Time = x.Time,
                        Type = x.Type,
                        Preview = x.Preview,
                        FullText = x.FullText,
                        IsPinned = x.IsPinned,
                        IsTemplate = x.IsTemplate,
                        Data = x.Type == "Text" ? x.FullText : null // Sadece metin verisini sakla
                    }).ToList();

                var json = JsonSerializer.Serialize(saveList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_saveFilePath, json);
                return true;
            }
            catch { return false; }
        }

        public bool LoadFromFile()
        {
            try
            {
                if (!File.Exists(_saveFilePath)) return false;
                var json = File.ReadAllText(_saveFilePath);
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json);
                if (items == null) return false;

                _allItems.Clear();
                foreach (var item in items)
                {
                    if (item.Type == "Text") item.Data = item.FullText;
                    _allItems.Add(item);
                }
                return true;
            }
            catch { return false; }
        }

        public void AddTemplate(string name, string content)
        {
            var item = new ClipboardItem
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.Now,
                Type = "Text",
                Preview = name,
                FullText = content,
                Data = content,
                IsTemplate = true
            };
            _allItems.Insert(0, item);
            SaveToFile();
        }
    }
}
