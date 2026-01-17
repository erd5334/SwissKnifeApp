using SwissKnifeApp.Models;
using SwissKnifeApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace SwissKnifeApp.Views.Modules
{
    public partial class ClipboardHistoryPage : Page
    {
        private readonly ClipboardHistoryService _clipboardService;
        private readonly ObservableCollection<ClipboardItem> _filteredHistory = new();
        private readonly ObservableCollection<ClipboardItem> _filteredPinned = new();
        private readonly ObservableCollection<ClipboardItem> _filteredTemplates = new();

        private readonly ObservableCollection<SequenceItem> _sequenceItems = new();
        private int _currentSequenceIndex = 0;

        public ClipboardHistoryPage()
        {
            InitializeComponent();

            _clipboardService = new ClipboardHistoryService();
            _clipboardService.LoadFromFile();

            ClipboardList.ItemsSource = _filteredHistory;
            PinnedList.ItemsSource = _filteredPinned;
            TemplatesList.ItemsSource = _filteredTemplates;
            SequenceList.ItemsSource = _sequenceItems;

            // Add ContextMenu to ClipboardList for Sequence
            var cm = new ContextMenu();
            var miSequence = new MenuItem { Header = "Otomatik Sıraya Ekle" };
            miSequence.Click += (s, e) => AddSelectedToSequence();
            cm.Items.Add(miSequence);
            ClipboardList.ContextMenu = cm;
            PinnedList.ContextMenu = cm;

            // Share same template for simplicity
            PinnedList.ItemTemplate = ClipboardList.ItemTemplate;
            TemplatesList.ItemTemplate = ClipboardList.ItemTemplate;

            ApplyFilters();
            StartClipboardWatcher();
        }

        private void AddSelectedToSequence()
        {
            if (ClipboardList.SelectedItem is ClipboardItem item)
            {
                _sequenceItems.Add(new SequenceItem { 
                    Index = _sequenceItems.Count + 1, 
                    Content = item.Preview, 
                    FullText = item.FullText 
                });
                UpdateSequenceStatus();
            }
        }

        private void BtnNextInSequence_Click(object sender, RoutedEventArgs e)
        {
            if (_sequenceItems.Count == 0) return;

            if (_currentSequenceIndex >= _sequenceItems.Count)
                _currentSequenceIndex = 0;

            var item = _sequenceItems[_currentSequenceIndex];
            System.Windows.Clipboard.SetText(item.FullText);
            
            _currentSequenceIndex++;
            UpdateSequenceStatus();
            
            // Highlight current in UI
            SequenceList.SelectedIndex = _currentSequenceIndex - 1;
        }

        private void UpdateSequenceStatus()
        {
            if (_sequenceItems.Count == 0)
            {
                TxtSequenceStatus.Text = "Sıra boş";
                return;
            }
            TxtSequenceStatus.Text = $"{_currentSequenceIndex + 1}. öğe hazır (Toplam {_sequenceItems.Count})";
        }

        private void BtnRemoveFromSequence_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is SequenceItem item)
            {
                _sequenceItems.Remove(item);
                // Re-index
                for (int i = 0; i < _sequenceItems.Count; i++) _sequenceItems[i].Index = i + 1;
                _currentSequenceIndex = 0;
                UpdateSequenceStatus();
            }
        }

        public class SequenceItem : System.ComponentModel.INotifyPropertyChanged
        {
            private int _index;
            public int Index 
            { 
                get => _index; 
                set { _index = value; OnPropertyChanged(); } 
            }
            public string Content { get; set; } = "";
            public string FullText { get; set; } = "";

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        private void StartClipboardWatcher()
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => CheckClipboard();
            timer.Start();
        }

        private void CheckClipboard()
        {
            if (_clipboardService.CheckAndAddClipboardContent())
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            string query = SearchBox.Text;

            UpdateList(_filteredHistory, _clipboardService.FilterItems(query));
            UpdateList(_filteredPinned, _clipboardService.FilterItems(query, onlyPinned: true));
            UpdateList(_filteredTemplates, _clipboardService.FilterItems(query, onlyTemplates: true));
        }

        private void UpdateList(ObservableCollection<ClipboardItem> target, System.Collections.Generic.IEnumerable<ClipboardItem> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClipboardList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is ClipboardItem item)
            {
                PerformCopy(item);
            }
        }

        private void PerformCopy(ClipboardItem item)
        {
            if (_clipboardService.CopyItemToClipboard(item))
            {
                // Show a brief message or toast if available
            }
        }

        private void ClipboardList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is ClipboardItem item && item.Type == "Image")
            {
                PopupImage.Source = (BitmapSource)item.Data;
                TxtOcrResult.Text = string.IsNullOrEmpty(item.FullText) ? "" : "OCR Sonucu: " + item.FullText;
                ImagePopup.IsOpen = true;
                BtnOcr.IsEnabled = true;
                BtnOcr.DataContext = item; // OCR yapılacak öğeyi taşıması için
            }
            else
            {
                BtnOcr.IsEnabled = false;
            }
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ClipboardItem item)
            {
                _clipboardService.TogglePin(item);
                ApplyFilters();
            }
        }

        private void BtnCopyNow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ClipboardItem item)
            {
                PerformCopy(item);
            }
        }

        private void BtnAddTemplate_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtTemplateName.Text.Trim();
            string content = TxtTemplateContent.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Lütfen ad ve içerik girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _clipboardService.AddTemplate(name, content);
            TxtTemplateName.Clear();
            TxtTemplateContent.Clear();
            ApplyFilters();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Pinlenmemiş tüm geçmişi temizlemek istiyor musunuz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _clipboardService.ClearHistory();
                ApplyFilters();
            }
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e) => ImagePopup.IsOpen = false;

        private async void BtnOcr_Click(object sender, RoutedEventArgs e)
        {
            if (BtnOcr.DataContext is not ClipboardItem item || item.Data is not BitmapSource bitmapSource) return;

            try
            {
                BtnOcr.IsEnabled = false;
                TxtOcrResult.Text = "Metin ayıklanıyor...";

                // BitmapSource'u SoftwareBitmap'e dönüştür
                SoftwareBitmap softwareBitmap;
                using (MemoryStream ms = new MemoryStream())
                {
                    System.Windows.Media.Imaging.BitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                    encoder.Save(ms);
                    
                    using (IRandomAccessStream winStream = ms.AsRandomAccessStream())
                    {
                        Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(winStream);
                        softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    }
                }

                // OCR Çalıştır
                OcrEngine engine = OcrEngine.TryCreateFromUserProfileLanguages();
                if (engine != null)
                {
                    var result = await engine.RecognizeAsync(softwareBitmap);
                    if (!string.IsNullOrEmpty(result.Text))
                    {
                        item.FullText = result.Text;
                        TxtOcrResult.Text = "OCR Başarılı: " + result.Text;
                        
                        // Metni otomatik olarak panoya ekle (opsiyonel)
                        System.Windows.Clipboard.SetText(result.Text);
                        
                        // Serviste de güncelle ki kaydedilsin
                        _clipboardService.SaveToFile();
                    }
                    else
                    {
                        TxtOcrResult.Text = "Metin bulunamadı.";
                    }
                }
                else
                {
                    MessageBox.Show("Sistemde OCR dili yüklü değil.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("OCR İşlemi sırasında hata: " + ex.Message);
                TxtOcrResult.Text = "";
            }
            finally
            {
                BtnOcr.IsEnabled = true;
            }
        }
    }
}
