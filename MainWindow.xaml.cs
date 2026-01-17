using MahApps.Metro.Controls;
using SwissKnifeApp.Views.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SwissKnifeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool isMenuExpanded = true;
        private bool isDarkMode = false;
        private Button? activeButton = null;

        // All menu buttons for search functionality
        private List<Button> allMenuButtons = new();

        // Favorites
        private HashSet<string> favoriteModules = new();
        private const string FAVORITES_FILE = "favorites.json";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Initialize hotkeys after window handle is created
            InitializeHotkeys();
        }

        private void InitializeHotkeys()
        {
            try
            {
                var hotkeyService = new Services.HotkeyService();
                hotkeyService.RegisterHotkeys(this);
                
                // Wire up events to ScreenCaptureViewModel if it's loaded
                hotkeyService.FullScreenCaptureRequested += async (s, e) =>
                {
                    if (MainFrame.Content is ScreenCapturePage page && page.DataContext is ViewModels.ScreenCaptureViewModel vm)
                    {
                        var originalState = this.WindowState;
                        this.Hide();
                        await System.Threading.Tasks.Task.Delay(200); // Pencerenin kaybolması için kısa bir bekleme
                        
                        vm.CaptureFullScreenCommand.Execute(null);
                        
                        this.Show();
                        this.WindowState = originalState;
                        this.Activate();
                    }
                };

                hotkeyService.ActiveWindowCaptureRequested += async (s, e) =>
                {
                    if (MainFrame.Content is ScreenCapturePage page && page.DataContext is ViewModels.ScreenCaptureViewModel vm)
                    {
                        var originalState = this.WindowState;
                        this.Hide();
                        await System.Threading.Tasks.Task.Delay(200);
                        
                        vm.CaptureActiveWindowCommand.Execute(null);
                        
                        this.Show();
                        this.WindowState = originalState;
                        this.Activate();
                    }
                };

                hotkeyService.RegionSelectionRequested += async (s, e) =>
                {
                    if (MainFrame.Content is ScreenCapturePage page && page.DataContext is ViewModels.ScreenCaptureViewModel vm)
                    {
                        var originalState = this.WindowState;
                        this.Hide();
                        await System.Threading.Tasks.Task.Delay(200);
                        
                        vm.CaptureRegionSelectionCommand.Execute(null);
                        
                        this.Show();
                        this.WindowState = originalState;
                        this.Activate();
                    }
                };
            }
            catch
            {
                // Hotkey registration failed - continue without hotkeys
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Collect all menu buttons for search functionality
            CollectMenuButtons(MenuItemsContainer);
            
            // Load favorites
            LoadFavorites();
            
            // Add right-click handlers to all menu buttons
            AttachRightClickHandlers();
        }

        private void CollectMenuButtons(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && button.Tag is string)
                {
                    allMenuButtons.Add(button);
                }
                
                CollectMenuButtons(child);
            }
        }

        private void AttachRightClickHandlers()
        {
            foreach (var button in allMenuButtons)
            {
                button.MouseRightButtonUp += MenuButton_RightClick;
            }
        }

        #region Menu Navigation

        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isMenuExpanded)
            {
                // Collapse menu
                MenuColumn.Width = new GridLength(60);
                isMenuExpanded = false;
            }
            else
            {
                // Expand menu
                MenuColumn.Width = new GridLength(280);
                isMenuExpanded = true;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // Hide welcome screen
                WelcomeGrid.Visibility = Visibility.Collapsed;

                // Update active state
                SetActiveButton(button);

                // Navigate to page
                NavigateToModule(tag);
            }
        }

        private void NavigateToModule(string moduleTag)
        {
            // Remove "Active" suffix if present
            moduleTag = moduleTag.Replace("Active", "");
            
            switch (moduleTag)
            {
                case "TextOperations":
                    MainFrame.Navigate(new TextOperationsPage());
                    break;
                case "TextSummarizer":
                    MainFrame.Navigate(new TextSummarizerPage());
                    break;
                case "DocumentTools":
                    MainFrame.Navigate(new DocumentToolsPage());
                    break;
                case "UnitConverter":
                    MainFrame.Navigate(new UnitConverterPage());
                    break;
                case "PdfOperations":
                    MainFrame.Navigate(new PdfOperationsPage());
                    break;
                case "ImageTools":
                case "ImageConverter":
                    MainFrame.Navigate(new ImageToolsPage());
                    break;
                case "PhotoCollage":
                    MainFrame.Navigate(new PhotoCollagePage());
                    break;
                case "QrCode":
                    MainFrame.Navigate(new QrBarcodeToolsPage());
                    break;
                case "ColorTools":
                case "ColorPicker":
                    MainFrame.Navigate(new ColorToolsPage());
                    break;
                case "MoneyToText":
                    MainFrame.Navigate(new MoneyToTextPage());
                    break;
                case "TaxCalculator":
                    MainFrame.Navigate(new TaxCalculatorPage());
                    break;
                case "PasswordTools":
                    MainFrame.Navigate(new PasswordToolsPage());
                    break;
                case "FileManager":
                    MainFrame.Navigate(new FileManagerPage());
                    break;
                case "FileCopy":
                    MainFrame.Navigate(new FileCopyPage());
                    break;
                case "ClipboardHistory":
                    MainFrame.Navigate(new ClipboardHistoryPage());
                    break;
                case "DataAnalysis":
                    MainFrame.Navigate(new DataAnalysisPage());
                    break;
                case "JsonXmlFormatter":
                    MainFrame.Navigate(new JsonXmlFormatterPage());
                    break;
                case "SqlTools":
                    MainFrame.Navigate(new SqlToolsPage());
                    break;
                case "DeveloperTools":
                    MainFrame.Navigate(new DeveloperToolsPage());
                    break;
                case "AudioTools":
                    MainFrame.Navigate(new AudioToolsPage());
                    break;
                case "VideoTools":
                    MainFrame.Navigate(new VideoToolsPage());
                    break;
                case "YouTubeClipDownloader":
                    MainFrame.Navigate(new YouTubeClipDownloaderPage());
                    break;
                case "NetworkTools":
                    MainFrame.Navigate(new NetworkToolsPage());
                    break;
                case "RestClient":
                    MainFrame.Navigate(new RestClientPage());
                    break;
                case "DuplicateFileFinder":
                    MainFrame.Navigate(new DuplicateFileFinderPage());
                    break;
                case "RegexTester":
                    MainFrame.Navigate(new RegexTesterPage());
                    break;
                case "ScreenCapture":
                    MainFrame.Navigate(new ScreenCapturePage());
                    break;
                default:
                    MessageBox.Show($"'{moduleTag}' modülü henüz eklenmedi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void SetActiveButton(Button button)
        {
            // Remove active state from previous button
            if (activeButton != null)
            {
                string? currentTag = activeButton.Tag?.ToString();
                if (currentTag != null && currentTag.Contains("Active"))
                {
                    activeButton.Tag = currentTag.Replace("Active", "");
                }
            }

            // Set active state to new button
            activeButton = button;
            string? newTag = button.Tag?.ToString();
            if (newTag != null && !newTag.Contains("Active"))
            {
                button.Tag = newTag; // Will trigger template change
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Navigate back to home
            MainFrame.Content = null;
            WelcomeGrid.Visibility = Visibility.Visible;

            // Clear active button
            if (activeButton != null)
            {
                string? currentTag = activeButton.Tag?.ToString();
                if (currentTag != null && currentTag.Contains("Active"))
                {
                    activeButton.Tag = currentTag.Replace("Active", "");
                }
                activeButton = null;
            }
        }

        #endregion

        #region Search Functionality

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower().Trim();

            // Filter menu items based on search text
            foreach (var button in allMenuButtons)
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Show all buttons
                    button.Visibility = Visibility.Visible;
                }
                else
                {
                    // Search in button text content
                    var textBlock = FindVisualChild<TextBlock>(button);
                    if (textBlock != null)
                    {
                        string buttonText = textBlock.Text.ToLower();
                        button.Visibility = buttonText.Contains(searchText) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            // Auto-expand categories that have visible items
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                ExpandCategoriesWithVisibleItems();
            }
        }

        private void ExpandCategoriesWithVisibleItems()
        {
            // Find all expanders and expand them if they have visible buttons
            var expanders = FindVisualChildren<Expander>(MenuItemsContainer);
            foreach (var expander in expanders)
            {
                bool hasVisibleItems = HasVisibleButtons(expander);
                expander.IsExpanded = hasVisibleItems;
            }
        }

        private bool HasVisibleButtons(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && button.Visibility == Visibility.Visible)
                {
                    return true;
                }
                
                if (HasVisibleButtons(child))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Dark Mode

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ApplyDarkTheme();
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyLightTheme();
        }

        private void ApplyDarkTheme()
        {
            isDarkMode = true;

            // Update MainWindow resources
            this.Resources["SidebarBackground"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            this.Resources["SidebarHeaderBackground"] = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            this.Resources["SidebarHeaderForeground"] = new SolidColorBrush(Colors.White);
            this.Resources["MenuItemHoverBackground"] = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            this.Resources["MenuItemActiveBackground"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            this.Resources["MenuItemActiveForeground"] = new SolidColorBrush(Colors.White);
            this.Resources["MenuItemForeground"] = new SolidColorBrush(Colors.White);
            this.Resources["CategoryHeaderBackground"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            this.Resources["AccentColor"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            this.Resources["MainBackground"] = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            
            // Update Application-wide resources (for all modules)
            Application.Current.Resources["AppBackground"] = new SolidColorBrush(Color.FromRgb(25, 25, 25));
            Application.Current.Resources["AppForeground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Application.Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            Application.Current.Resources["InputBackground"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            Application.Current.Resources["InputForeground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            Application.Current.Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            Application.Current.Resources["ButtonForeground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            Application.Current.Resources["HeaderText"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(189, 195, 199));
        }

        private void ApplyLightTheme()
        {
            isDarkMode = false;

            // Restore MainWindow light colors
            this.Resources["SidebarBackground"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            this.Resources["SidebarHeaderBackground"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            this.Resources["SidebarHeaderForeground"] = new SolidColorBrush(Colors.White);
            this.Resources["MenuItemHoverBackground"] = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            this.Resources["MenuItemActiveBackground"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            this.Resources["MenuItemActiveForeground"] = new SolidColorBrush(Colors.White);
            this.Resources["MenuItemForeground"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            this.Resources["CategoryHeaderBackground"] = new SolidColorBrush(Color.FromRgb(236, 239, 241));
            this.Resources["AccentColor"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            this.Resources["MainBackground"] = new SolidColorBrush(Colors.White);
            
            // Restore Application-wide light colors (for all modules)
            Application.Current.Resources["AppBackground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["AppForeground"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            Application.Current.Resources["CardBackground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Application.Current.Resources["InputBackground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["InputForeground"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            Application.Current.Resources["InputBorderBrush"] = new SolidColorBrush(Color.FromRgb(189, 195, 199));
            Application.Current.Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            Application.Current.Resources["ButtonForeground"] = new SolidColorBrush(Colors.White);
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            Application.Current.Resources["HeaderText"] = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            Application.Current.Resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(127, 140, 141));
        }

        #endregion

        #region Favorites System

        private void LoadFavorites()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FAVORITES_FILE);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var favorites = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                    if (favorites != null)
                    {
                        favoriteModules = new HashSet<string>(favorites);
                        RefreshFavorites();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Favoriler yüklenirken hata: {ex.Message}");
            }
        }

        private void SaveFavorites()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FAVORITES_FILE);
                string json = System.Text.Json.JsonSerializer.Serialize(favoriteModules.ToList(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Favoriler kaydedilirken hata: {ex.Message}");
            }
        }

        private void MenuButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagString)
            {
                // Clean tag
                string tag = tagString.Replace("Active", "");
                
                var contextMenu = new ContextMenu();
                
                bool isFavorite = favoriteModules.Contains(tag);
                
                var menuItem = new MenuItem
                {
                    Header = isFavorite ? "⭐ Favorilerden Çıkar" : "☆ Favorilere Ekle",
                    Tag = tag
                };
                
                menuItem.Click += (s, args) =>
                {
                    if (isFavorite)
                    {
                        RemoveFromFavorites(tag);
                    }
                    else
                    {
                        AddToFavorites(tag, button);
                    }
                };
                
                contextMenu.Items.Add(menuItem);
                button.ContextMenu = contextMenu;
                contextMenu.IsOpen = true;
            }
        }

        private void AddToFavorites(string moduleTag, Button sourceButton)
        {
            if (!favoriteModules.Contains(moduleTag))
            {
                favoriteModules.Add(moduleTag);
                SaveFavorites();
                RefreshFavorites();
            }
        }

        private void RemoveFromFavorites(string moduleTag)
        {
            if (favoriteModules.Contains(moduleTag))
            {
                favoriteModules.Remove(moduleTag);
                SaveFavorites();
                RefreshFavorites();
            }
        }

        private void RefreshFavorites()
        {
            // Clear favorites panel
            FavoritesPanel.Children.Clear();
            
            if (favoriteModules.Count == 0)
            {
                // Show empty message
                var emptyText = new TextBlock
                {
                    Text = "Favori modül yok\nModüllere sağ tıklayarak ekleyin",
                    Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                    FontSize = 11,
                    Margin = new Thickness(25, 5, 25, 5),
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap
                };
                FavoritesPanel.Children.Add(emptyText);
                expFavorites.IsExpanded = false;
            }
            else
            {
                // Add favorite buttons
                foreach (var moduleTag in favoriteModules)
                {
                    var button = CreateFavoriteButton(moduleTag);
                    if (button != null)
                    {
                        FavoritesPanel.Children.Add(button);
                    }
                }
                expFavorites.IsExpanded = true;
            }
        }

        private Button? CreateFavoriteButton(string moduleTag)
        {
            // Find original button to copy properties
            var originalButton = allMenuButtons.FirstOrDefault(b => 
            {
                string? tag = b.Tag?.ToString();
                return tag != null && tag.Replace("Active", "") == moduleTag;
            });
            
            if (originalButton == null) return null;

            var button = new Button
            {
                Style = (Style)FindResource("ModernMenuButton"),
                Tag = moduleTag,
                ToolTip = originalButton.ToolTip
            };

            // Copy content from original button
            if (originalButton.Content is StackPanel originalPanel)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                
                // Copy icon and text
                foreach (var child in originalPanel.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        var clone = CloneElement(element);
                        if (clone != null)
                        {
                            panel.Children.Add(clone);
                        }
                    }
                }
                
                button.Content = panel;
            }

            button.Click += MenuButton_Click;
            button.MouseRightButtonUp += MenuButton_RightClick;

            return button;
        }

        private FrameworkElement? CloneElement(FrameworkElement source)
        {
            if (source is TextBlock textBlock)
            {
                return new TextBlock
                {
                    Text = textBlock.Text,
                    VerticalAlignment = textBlock.VerticalAlignment,
                    Margin = textBlock.Margin
                };
            }
            else if (source is MahApps.Metro.IconPacks.PackIconMaterial iconMaterial)
            {
                return new MahApps.Metro.IconPacks.PackIconMaterial
                {
                    Kind = iconMaterial.Kind,
                    Width = iconMaterial.Width,
                    Height = iconMaterial.Height,
                    Margin = iconMaterial.Margin
                };
            }
            else if (source is MahApps.Metro.IconPacks.PackIconModern iconModern)
            {
                return new MahApps.Metro.IconPacks.PackIconModern
                {
                    Kind = iconModern.Kind,
                    Width = iconModern.Width,
                    Height = iconModern.Height,
                    Margin = iconModern.Margin
                };
            }
            else if (source is MahApps.Metro.IconPacks.PackIconFontAwesome iconFontAwesome)
            {
                return new MahApps.Metro.IconPacks.PackIconFontAwesome
                {
                    Kind = iconFontAwesome.Kind,
                    Width = iconFontAwesome.Width,
                    Height = iconFontAwesome.Height,
                    Margin = iconFontAwesome.Margin
                };
            }
            
            return null;
        }

        #endregion

        #region Helper Methods

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    yield return typedChild;
                }
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        #endregion
    }
}