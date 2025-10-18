using SwissKnifeApp.Models;
using SwissKnifeApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SwissKnifeApp.Views.Modules
{
    public partial class ClipboardHistoryPage : Page
    {
        private readonly ClipboardHistoryService _clipboardService;
        private readonly ObservableCollection<ClipboardItem> _filteredItems;

        public ClipboardHistoryPage()
        {
            InitializeComponent();

            _clipboardService = new ClipboardHistoryService();
            _filteredItems = new ObservableCollection<ClipboardItem>();

            ClipboardList.ItemsSource = _filteredItems;
            
            // Kaydedilmiş geçmişi yükle
            _clipboardService.LoadFromFile();
            ApplySearchFilter("");

            StartClipboardWatcher();
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
                // Yeni öğe eklendi, filtreyi yenile
                ApplySearchFilter(SearchBox.Text);
            }
        }

        private void ClipboardList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClipboardList.SelectedItem is ClipboardItem item)
            {
                bool success = _clipboardService.CopyItemToClipboard(item);
                if (success)
                {
                    MessageBox.Show("Panoya kopyalandı.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Panoya kopyalanamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            _clipboardService.ClearAll();
            _filteredItems.Clear();
            MessageBox.Show("Geçmiş temizlendi.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool success = _clipboardService.SaveToFile();
            if (success)
            {
                MessageBox.Show("Geçmiş kaydedildi.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Geçmiş kaydedilemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            bool success = _clipboardService.LoadFromFile();
            if (success)
            {
                ApplySearchFilter("");
                MessageBox.Show($"{_clipboardService.GetItemCount()} öğe yüklendi.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Geçmiş yüklenemedi veya dosya bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            CheckClipboard();
            MessageBox.Show("Pano kontrol edildi.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter(SearchBox.Text);
        }

        private void ApplySearchFilter(string query)
        {
            _filteredItems.Clear();

            var filtered = _clipboardService.FilterItems(query);
            
            foreach (var item in filtered)
            {
                _filteredItems.Add(item);
            }
        }
    }
}
