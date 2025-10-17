using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFonts = System.Windows.SystemFonts;
using ImageSharpBrushes = SixLabors.ImageSharp.Drawing.Processing.Brushes;
using ImageSharpFonts = SixLabors.Fonts.SystemFonts;
using ImageSharpFontStyle = SixLabors.Fonts.FontStyle;
using Image = System.Windows.Controls.Image;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PhotoCollagePage : Page
    {

        private List<string> _loadedPhotos = new List<string>();
        private int _maxPhotos = 4; // Default for 2x2 template
        private int _draggedPhotoIndex = -1;

        private class PhotoTransform
        {
            public double Zoom { get; set; } = 1.0;
            public double OffsetX { get; set; } = 0.0;
            public double OffsetY { get; set; } = 0.0;
        }

        private Dictionary<int, PhotoTransform> _photoTransforms = new Dictionary<int, PhotoTransform>();
        private bool _isPanning = false;
        private System.Windows.Point _panStartPoint;
        private int _panningPhotoIndex = -1;

       
        public PhotoCollagePage()
        {
            InitializeComponent();
            UpdatePreview();
        }

        // Event handler fonksiyonları
        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var border = sender as Border;
                if (border?.Tag is int sourceIndex)
                {
                    _draggedPhotoIndex = sourceIndex;
                    DragDrop.DoDragDrop(border, sourceIndex, DragDropEffects.Move);
                }
            }
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            // Kolaj içi fotoğraf yer değiştirme
            if (e.Data.GetDataPresent(typeof(int)))
            {
                var targetBorder = sender as Border;
                if (targetBorder?.Tag is int targetIndex && _draggedPhotoIndex >= 0 && _draggedPhotoIndex != targetIndex)
                {
                    // Fotoğrafların yerlerini değiştir
                    var temp = _loadedPhotos[_draggedPhotoIndex];
                    _loadedPhotos[_draggedPhotoIndex] = _loadedPhotos[targetIndex];
                    _loadedPhotos[targetIndex] = temp;

                    _draggedPhotoIndex = -1;
                    UpdatePreview();
                }
                e.Handled = true;
            }
            // Dışarıdan dosya ekleme
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        if (_loadedPhotos.Count < _maxPhotos)
                        {
                            _loadedPhotos.Add(file);
                        }
                    }
                }
                UpdatePhotoCount();
                UpdatePreview();
            }
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(int)) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var image = sender as Image;
            if (image?.Tag is int photoIndex)
            {
                if (!_photoTransforms.ContainsKey(photoIndex))
                    _photoTransforms[photoIndex] = new PhotoTransform();
                var t = _photoTransforms[photoIndex];
                double oldZoom = t.Zoom;
                double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
                t.Zoom = Math.Max(0.5, Math.Min(3.0, t.Zoom + zoomDelta));
                // Zoom merkezini korumak için offset'i ayarla
                var pos = e.GetPosition(image);
                double relX = (pos.X - image.ActualWidth / 2.0) / oldZoom;
                double relY = (pos.Y - image.ActualHeight / 2.0) / oldZoom;
                t.OffsetX -= relX * (t.Zoom - oldZoom);
                t.OffsetY -= relY * (t.Zoom - oldZoom);
                ClampPan(image, t);
                ApplyImageTransform(image, t);
                e.Handled = true;
            }
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                var image = sender as Image;
                if (image?.Tag is int photoIndex)
                {
                    _isPanning = true;
                    _panStartPoint = e.GetPosition(image);
                    _panningPhotoIndex = photoIndex;
                    image.CaptureMouse();
                }
            }
            else if (e.ClickCount == 2 && e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                var img = sender as Image;
                if (img?.Source is BitmapImage bmp)
                {
                    string path = bmp.UriSource.LocalPath;
                    _loadedPhotos.Remove(path);
                    UpdatePhotoCount();
                    UpdatePreview();
                }
            }
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isPanning && e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var image = sender as Image;
                if (image?.Tag is int photoIndex && _panningPhotoIndex == photoIndex)
                {
                    if (!_photoTransforms.ContainsKey(photoIndex))
                        _photoTransforms[photoIndex] = new PhotoTransform();
                    var t = _photoTransforms[photoIndex];
                    var pos = e.GetPosition(image);
                    double dx = pos.X - _panStartPoint.X;
                    double dy = pos.Y - _panStartPoint.Y;
                    t.OffsetX += dx / t.Zoom;
                    t.OffsetY += dy / t.Zoom;
                    _panStartPoint = pos;
                    ClampPan(image, t);
                    ApplyImageTransform(image, t);
                }
            }
        }

        private void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                var image = sender as Image;
                image?.ReleaseMouseCapture();
                _isPanning = false;
                _panningPhotoIndex = -1;
            }
        }

        private void ApplyImageTransform(Image image, PhotoTransform t)
        {
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(t.Zoom, t.Zoom));
            group.Children.Add(new TranslateTransform(t.OffsetX, t.OffsetY));
            image.RenderTransform = group;
            image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void ClampPan(Image image, PhotoTransform t)
        {
            double w = image.ActualWidth;
            double h = image.ActualHeight;
            if (t.Zoom >= 1.0)
            {
                double maxOffsetX = (t.Zoom - 1) * w / (2 * t.Zoom);
                double maxOffsetY = (t.Zoom - 1) * h / (2 * t.Zoom);
                t.OffsetX = Math.Max(-maxOffsetX, Math.Min(maxOffsetX, t.OffsetX));
                t.OffsetY = Math.Max(-maxOffsetY, Math.Min(maxOffsetY, t.OffsetY));
            }
            else
            {
                // Küçültme: fotoğrafı ortala, kenarlarda boşluk olmasın
                t.OffsetX = 0;
                t.OffsetY = 0;
            }
        }

        private void CmbTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTemplate.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag?.ToString() ?? "4";
                _maxPhotos = tag switch
                {
                    "2H" => 2,
                    "2V" => 2,
                    "3" => 3,
                    "4" => 4,
                    "4_1_3" => 4,
                    "5" => 5,
                    "6" => 6,
                    "8" => 8,
                    "9" => 9,
                    "12" => 12,
                    "16" => 16,
                    _ => 4
                };

                // Fazla fotoğrafları kaldır
                if (_loadedPhotos.Count > _maxPhotos)
                {
                    _loadedPhotos = _loadedPhotos.Take(_maxPhotos).ToList();
                }

                UpdatePhotoCount();
                UpdatePreview();
            }
        }

        private void BtnAddPhotos_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Görsel Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tüm Dosyalar|*.*",
                Multiselect = true,
                Title = "Fotoğraf Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    if (_loadedPhotos.Count < _maxPhotos)
                    {
                        _loadedPhotos.Add(file);
                    }
                    else
                    {
                        MessageBox.Show($"Maksimum {_maxPhotos} fotoğraf ekleyebilirsiniz.", 
                            "Limit Aşıldı", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                        break;
                    }
                }

                UpdatePhotoCount();
                UpdatePreview();
            }
        }

        private void BtnClearPhotos_Click(object sender, RoutedEventArgs e)
        {
            _loadedPhotos.Clear();
            UpdatePhotoCount();
            UpdatePreview();
        }

        private void UpdatePhotoCount()
        {
            if (TxtPhotoCount != null)
            {
                TxtPhotoCount.Text = $"Yüklenen: {_loadedPhotos.Count} / {_maxPhotos} fotoğraf";
            }
                if (TxtPhotoCount != null)
                {
                    TxtPhotoCount.Text = $"Yüklenen: {_loadedPhotos.Count} / {_maxPhotos} fotoğraf";
                }
        }

        private void CmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void SliderBorderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtBorderWidth != null)
            {
                TxtBorderWidth.Text = $"{(int)e.NewValue} px";
                UpdatePreview();
            }
                if (TxtBorderWidth != null)
                {
                    TxtBorderWidth.Text = $"{(int)e.NewValue} px";
                    UpdatePreview();
                }
        }

        private void CmbBorderColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void SliderCornerRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtCornerRadius != null)
            {
                TxtCornerRadius.Text = $"{(int)e.NewValue} px";
                UpdatePreview();
            }
                if (TxtCornerRadius != null)
                {
                    TxtCornerRadius.Text = $"{(int)e.NewValue} px";
                    UpdatePreview();
                }
        }

        private void TxtOverlayText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void NumFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            UpdatePreview();
        }

        private void CmbTextColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void PreviewCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        if (_loadedPhotos.Count < _maxPhotos)
                        {
                            _loadedPhotos.Add(file);
                        }
                    }
                }

                UpdatePhotoCount();
                UpdatePreview();
            }
        }

        private void PreviewCanvas_DragOver(object sender, DragEventArgs e)
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

        private void UpdatePreview()
        {
            if (PreviewCanvas == null || EmptyState == null) return;

            PreviewCanvas.Children.Clear();

            if (_loadedPhotos.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;

            // Arka plan rengi
                var bgColor = ColorPickerBackground.SelectedColor ?? System.Windows.Media.Colors.White;
                var bgBrush = new SolidColorBrush(bgColor);
            PreviewCanvas.Background = bgBrush;

            // Kenarlık ayarları
            int borderWidth = (int)SliderBorderWidth.Value;
            var borderColor = ColorPickerBorder.SelectedColor?.ToString() ?? "#FFFFFF";
            int cornerRadius = (int)SliderCornerRadius.Value;

            // Şablon
            string template = "4";
            if (CmbTemplate?.SelectedItem is ComboBoxItem templateItem)
            {
                template = templateItem.Tag?.ToString() ?? "4";
            }

            double canvasWidth = 600;
            double canvasHeight = 600;

            CreateCollageLayout(template, canvasWidth, canvasHeight, borderWidth, borderColor, cornerRadius);

            // Metin ekle
            if (!string.IsNullOrWhiteSpace(TxtOverlayText?.Text))
            {
                AddTextOverlay();
            }
        }

        private void CreateCollageLayout(string template, double width, double height, int borderWidth, string borderColor, int cornerRadius)
        {
            var borderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor));

            switch (template)
            {
                case "2H": // 2 Yatay
                    CreateGrid(1, 2, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "2V": // 2 Dikey
                    CreateGrid(2, 1, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "3": // 3 Fotoğraf (2+1)
                    CreateCustom3Layout(width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "4": // 2x2
                    CreateGrid(2, 2, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "4_1_3": // 4 Fotoğraf (1+3)
                    CreateCustom4_1_3Layout(width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "5": // 5 Fotoğraf (2+3)
                    CreateCustom5Layout(width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "6": // 3x2
                    CreateGrid(2, 3, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "8": // 8 Fotoğraf (2+2+4)
                    CreateCustom8Layout(width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "9": // 3x3
                    CreateGrid(3, 3, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "12": // 4x3
                    CreateGrid(3, 4, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
                case "16": // 4x4
                    CreateGrid(4, 4, width, height, borderWidth, borderBrush, cornerRadius);
                    break;
            }
        }

        private void CreateGrid(int rows, int cols, double width, double height, int borderWidth, SolidColorBrush borderBrush, int cornerRadius)
        {
            double cellWidth = (width - (cols + 1) * borderWidth) / cols;
            double cellHeight = (height - (rows + 1) * borderWidth) / rows;

            int photoIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (photoIndex >= _loadedPhotos.Count) break;

                    double x = borderWidth + col * (cellWidth + borderWidth);
                    double y = borderWidth + row * (cellHeight + borderWidth);

                    var border = new Border
                    {
                        Width = cellWidth,
                        Height = cellHeight,
                        BorderBrush = borderBrush,
                        BorderThickness = new Thickness(borderWidth),
                        CornerRadius = new CornerRadius(cornerRadius),
                        Tag = photoIndex,
                        ClipToBounds = true, // Fotoğraf taşmasın
                        Background = WpfBrushes.Transparent
                    };
                    border.AllowDrop = true;
                    border.MouseMove += Border_MouseMove;
                    border.Drop += Border_Drop;
                    border.DragOver += Border_DragOver;

                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(_loadedPhotos[photoIndex]);
                        bitmap.DecodePixelWidth = (int)cellWidth;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        var image = new System.Windows.Controls.Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.Uniform,
                            Tag = photoIndex,
                            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
                        };
                        image.MouseDown += Image_MouseDown;
                        image.MouseWheel += Image_MouseWheel;
                        image.MouseMove += Image_MouseMove;
                        image.MouseUp += Image_MouseUp;

                        // Transform'ı uygula (zoom ve pan)
                        if (_photoTransforms.ContainsKey(photoIndex))
                        {
                            ApplyImageTransform(image, _photoTransforms[photoIndex]);
                        }

                        // Image'ı Grid içine sar (ClipToBounds için)
                        var imageContainer = new Grid
                        {
                            ClipToBounds = true,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                            VerticalAlignment = System.Windows.VerticalAlignment.Stretch
                        };
                        imageContainer.Children.Add(image);
                        border.Child = imageContainer;
                    }
                    catch
                    {
                        border.Background = WpfBrushes.LightGray;
                    }

                    Canvas.SetLeft(border, x);
                    Canvas.SetTop(border, y);
                    PreviewCanvas.Children.Add(border);
                    photoIndex++;
                }
            }
        }

        private void CreateCustom3Layout(double width, double height, int borderWidth, SolidColorBrush borderBrush, int cornerRadius)
        {
            // Üstte 2, altta 1 büyük fotoğraf
            double topHeight = (height - 3 * borderWidth) / 2;
            double topWidth = (width - 3 * borderWidth) / 2;
            double bottomHeight = topHeight;
            double bottomWidth = width - 2 * borderWidth;

            // Üst sol
            if (_loadedPhotos.Count > 0)
                AddPhotoToBorder(borderWidth, borderWidth, topWidth, topHeight, 0, borderBrush, cornerRadius, borderWidth);

            // Üst sağ
            if (_loadedPhotos.Count > 1)
                AddPhotoToBorder(2 * borderWidth + topWidth, borderWidth, topWidth, topHeight, 1, borderBrush, cornerRadius, borderWidth);

            // Alt ortada
            if (_loadedPhotos.Count > 2)
                AddPhotoToBorder(borderWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 2, borderBrush, cornerRadius, borderWidth);
        }

        private void CreateCustom4_1_3Layout(double width, double height, int borderWidth, SolidColorBrush borderBrush, int cornerRadius)
        {
            // Üstte 1 büyük, altta 3 küçük
            double topHeight = (height - 3 * borderWidth) * 2 / 3;
            double topWidth = width - 2 * borderWidth;
            double bottomHeight = (height - 3 * borderWidth) / 3;
            double bottomWidth = (width - 4 * borderWidth) / 3;

            // Üstte büyük
            if (_loadedPhotos.Count > 0)
                AddPhotoToBorder(borderWidth, borderWidth, topWidth, topHeight, 0, borderBrush, cornerRadius, borderWidth);

            // Altta 3 küçük
            if (_loadedPhotos.Count > 1)
                AddPhotoToBorder(borderWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 1, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 2)
                AddPhotoToBorder(2 * borderWidth + bottomWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 2, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 3)
                AddPhotoToBorder(3 * borderWidth + 2 * bottomWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 3, borderBrush, cornerRadius, borderWidth);
        }

        private void CreateCustom5Layout(double width, double height, int borderWidth, SolidColorBrush borderBrush, int cornerRadius)
        {
            // Üstte 2, altta 3
            double topHeight = (height - 3 * borderWidth) / 2;
            double topWidth = (width - 3 * borderWidth) / 2;
            double bottomHeight = topHeight;
            double bottomWidth = (width - 4 * borderWidth) / 3;

            // Üst 2
            if (_loadedPhotos.Count > 0)
                AddPhotoToBorder(borderWidth, borderWidth, topWidth, topHeight, 0, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 1)
                AddPhotoToBorder(2 * borderWidth + topWidth, borderWidth, topWidth, topHeight, 1, borderBrush, cornerRadius, borderWidth);

            // Alt 3
            if (_loadedPhotos.Count > 2)
                AddPhotoToBorder(borderWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 2, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 3)
                AddPhotoToBorder(2 * borderWidth + bottomWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 3, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 4)
                AddPhotoToBorder(3 * borderWidth + 2 * bottomWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 4, borderBrush, cornerRadius, borderWidth);
        }

        private void CreateCustom8Layout(double width, double height, int borderWidth, SolidColorBrush borderBrush, int cornerRadius)
        {
            // Üstte 2, ortada 2, altta 4
            double rowHeight = (height - 4 * borderWidth) / 3;
            double topWidth = (width - 3 * borderWidth) / 2;
            double bottomWidth = (width - 5 * borderWidth) / 4;

            // Üst 2
            if (_loadedPhotos.Count > 0)
                AddPhotoToBorder(borderWidth, borderWidth, topWidth, rowHeight, 0, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 1)
                AddPhotoToBorder(2 * borderWidth + topWidth, borderWidth, topWidth, rowHeight, 1, borderBrush, cornerRadius, borderWidth);

            // Orta 2
            if (_loadedPhotos.Count > 2)
                AddPhotoToBorder(borderWidth, 2 * borderWidth + rowHeight, topWidth, rowHeight, 2, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 3)
                AddPhotoToBorder(2 * borderWidth + topWidth, 2 * borderWidth + rowHeight, topWidth, rowHeight, 3, borderBrush, cornerRadius, borderWidth);

            // Alt 4
            double yBottom = 3 * borderWidth + 2 * rowHeight;
            if (_loadedPhotos.Count > 4)
                AddPhotoToBorder(borderWidth, yBottom, bottomWidth, rowHeight, 4, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 5)
                AddPhotoToBorder(2 * borderWidth + bottomWidth, yBottom, bottomWidth, rowHeight, 5, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 6)
                AddPhotoToBorder(3 * borderWidth + 2 * bottomWidth, yBottom, bottomWidth, rowHeight, 6, borderBrush, cornerRadius, borderWidth);
            if (_loadedPhotos.Count > 7)
                AddPhotoToBorder(4 * borderWidth + 3 * bottomWidth, yBottom, bottomWidth, rowHeight, 7, borderBrush, cornerRadius, borderWidth);
        }

        private void AddPhotoToBorder(double x, double y, double width, double height, int photoIndex, SolidColorBrush borderBrush, int cornerRadius, int borderWidth)
        {
            var border = new Border
            {
                Width = width,
                Height = height,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(borderWidth),
                CornerRadius = new CornerRadius(cornerRadius),
                Tag = photoIndex,
                ClipToBounds = true, // Fotoğraf taşmasın
                Background = WpfBrushes.Transparent
            };
            border.AllowDrop = true;
            border.MouseMove += Border_MouseMove;
            border.Drop += Border_Drop;
            border.DragOver += Border_DragOver;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_loadedPhotos[photoIndex]);
                bitmap.DecodePixelWidth = (int)width;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var image = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    Tag = photoIndex,
                    RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
                };
                image.MouseDown += Image_MouseDown;
                image.MouseWheel += Image_MouseWheel;
                image.MouseMove += Image_MouseMove;
                image.MouseUp += Image_MouseUp;

                // Transform'ı uygula (zoom ve pan)
                if (_photoTransforms.ContainsKey(photoIndex))
                {
                    ApplyImageTransform(image, _photoTransforms[photoIndex]);
                }

                // Image'ı Grid içine sar (ClipToBounds için)
                var imageContainer = new Grid
                {
                    ClipToBounds = true,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch
                };
                imageContainer.Children.Add(image);
                border.Child = imageContainer;
            }
            catch
            {
                border.Background = WpfBrushes.LightGray;
            }

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        PreviewCanvas.Children.Add(border);
    }        private void AddTextOverlay()
        {
            string text = TxtOverlayText.Text;
            double fontSize = NumFontSize?.Value ?? 24;
            var textColor = ColorPickerText.SelectedColor?.ToString() ?? "#FFFFFF";
            var brushObj = new BrushConverter().ConvertFrom(textColor);
            var textBrush = brushObj != null ? (SolidColorBrush)brushObj : WpfBrushes.White;

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = textBrush,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 5,
                    ShadowDepth = 2,
                    Opacity = 0.7
                }
            };

            // Metin konumu
            string position = "TopLeft";
            if (CmbTextPosition?.SelectedItem is ComboBoxItem posItem)
            {
                position = posItem.Tag?.ToString() ?? "TopLeft";
            }

            double canvasWidth = PreviewCanvas.ActualWidth > 0 ? PreviewCanvas.ActualWidth : 600;
            double canvasHeight = PreviewCanvas.ActualHeight > 0 ? PreviewCanvas.ActualHeight : 600;
            double margin = 20;

            // TextBlock boyutunu hesaplamak için ölçüm yapıyoruz
            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = textBlock.DesiredSize.Width;
            double textHeight = textBlock.DesiredSize.Height;

            double x = margin, y = margin;
            switch (position)
            {
                case "TopLeft":
                    x = margin;
                    y = margin;
                    break;
                case "TopCenter":
                    x = (canvasWidth - textWidth) / 2;
                    y = margin;
                    break;
                case "TopRight":
                    x = canvasWidth - textWidth - margin;
                    y = margin;
                    break;
                case "BottomLeft":
                    x = margin;
                    y = canvasHeight - textHeight - margin;
                    break;
                case "BottomCenter":
                    x = (canvasWidth - textWidth) / 2;
                    y = canvasHeight - textHeight - margin;
                    break;
                case "BottomRight":
                    x = canvasWidth - textWidth - margin;
                    y = canvasHeight - textHeight - margin;
                    break;
                case "Center":
                    x = (canvasWidth - textWidth) / 2;
                    y = (canvasHeight - textHeight) / 2;
                    break;
            }

            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            PreviewCanvas.Children.Add(textBlock);
        }

        private void BtnSaveCollage_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedPhotos.Count == 0)
            {
                MessageBox.Show("Lütfen önce fotoğraf ekleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Dosyası|*.png|JPG Dosyası|*.jpg",
                FileName = $"Collage_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (CmbOutputFormat?.SelectedItem is ComboBoxItem formatItem)
            {
                if (formatItem.Tag?.ToString() == "jpg")
                {
                    saveFileDialog.FilterIndex = 2;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    int outputSize = 1200;
                    if (CmbOutputSize?.SelectedItem is ComboBoxItem sizeItem)
                    {
                        outputSize = int.Parse(sizeItem.Tag?.ToString() ?? "1200");
                    }

                    CreateAndSaveCollage(saveFileDialog.FileName, outputSize);
                    MessageBox.Show("Kolaj başarıyla kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateAndSaveCollage(string fileName, int size)
        {
            // Arka plan rengi
            var bgColorHex = ColorPickerBackground.SelectedColor?.ToString() ?? "#FFFFFF";
            using var image = new Image<Rgba32>(size, size);
            image.Mutate(ctx => ctx.Fill(Rgba32.ParseHex(bgColorHex)));

            int borderWidth = (int)(SliderBorderWidth.Value * size / 600.0);

            // Şablon
            string template = "4";
            if (CmbTemplate?.SelectedItem is ComboBoxItem templateItem)
            {
                template = templateItem.Tag?.ToString() ?? "4";
            }

            // Fotoğrafları yerleştir
            DrawPhotosOnImage(image, template, size, borderWidth);

            // Metin ekle
            if (!string.IsNullOrWhiteSpace(TxtOverlayText?.Text))
            {
                DrawTextOnImage(image, size);
            }

            // Kaydet
            if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                image.SaveAsJpeg(fileName);
            }
            else
            {
                image.SaveAsPng(fileName);
            }
        }

        private void DrawPhotosOnImage(Image<Rgba32> canvas, string template, int size, int borderWidth)
        {
            switch (template)
            {
                case "2H":
                    DrawGridPhotos(canvas, 1, 2, size, borderWidth);
                    break;
                case "2V":
                    DrawGridPhotos(canvas, 2, 1, size, borderWidth);
                    break;
                case "3":
                    DrawCustom3Photos(canvas, size, borderWidth);
                    break;
                case "4":
                    DrawGridPhotos(canvas, 2, 2, size, borderWidth);
                    break;
                case "6":
                    DrawGridPhotos(canvas, 2, 3, size, borderWidth);
                    break;
                case "9":
                    DrawGridPhotos(canvas, 3, 3, size, borderWidth);
                    break;
            }
        }

        private void DrawGridPhotos(Image<Rgba32> canvas, int rows, int cols, int size, int borderWidth)
        {
            int cellWidth = (size - (cols + 1) * borderWidth) / cols;
            int cellHeight = (size - (rows + 1) * borderWidth) / rows;

            int photoIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (photoIndex >= _loadedPhotos.Count) return;

                    int x = borderWidth + col * (cellWidth + borderWidth);
                    int y = borderWidth + row * (cellHeight + borderWidth);

                    try
                    {
                        using var photo = SixLabors.ImageSharp.Image.Load<Rgba32>(_loadedPhotos[photoIndex]);
                        photo.Mutate(ctx => ctx.Resize(cellWidth, cellHeight, KnownResamplers.Lanczos3));

                        canvas.Mutate(ctx => ctx.DrawImage(photo, new SixLabors.ImageSharp.Point(x, y), 1f));
                    }
                    catch { }

                    photoIndex++;
                }
            }
        }

        private void DrawCustom3Photos(Image<Rgba32> canvas, int size, int borderWidth)
        {
            int topHeight = (size - 3 * borderWidth) / 2;
            int topWidth = (size - 3 * borderWidth) / 2;
            int bottomHeight = topHeight;
            int bottomWidth = size - 2 * borderWidth;

            // Üst sol
            if (_loadedPhotos.Count > 0)
                DrawSinglePhoto(canvas, borderWidth, borderWidth, topWidth, topHeight, 0);

            // Üst sağ
            if (_loadedPhotos.Count > 1)
                DrawSinglePhoto(canvas, 2 * borderWidth + topWidth, borderWidth, topWidth, topHeight, 1);

            // Alt ortada
            if (_loadedPhotos.Count > 2)
                DrawSinglePhoto(canvas, borderWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, 2);
        }

        private void DrawSinglePhoto(Image<Rgba32> canvas, int x, int y, int width, int height, int photoIndex)
        {
            try
            {
                using var photo = SixLabors.ImageSharp.Image.Load<Rgba32>(_loadedPhotos[photoIndex]);
                photo.Mutate(ctx => ctx.Resize(width, height, KnownResamplers.Lanczos3));
                canvas.Mutate(ctx => ctx.DrawImage(photo, new SixLabors.ImageSharp.Point(x, y), 1f));
            }
            catch { }
    }

    private void DrawTextOnImage(Image<Rgba32> image, int size)
    {
        string text = TxtOverlayText.Text;
        float fontSize = (float)(NumFontSize?.Value ?? 24) * size / 600f;

    var textColor = Rgba32.ParseHex(ColorPickerText.SelectedColor?.ToString() ?? "#FFFFFF");

        // Metin konumu
        string position = "TopLeft";
        if (CmbTextPosition?.SelectedItem is ComboBoxItem posItem)
        {
            position = posItem.Tag?.ToString() ?? "TopLeft";
        }
        float margin = 20 * size / 600f;
        float x = margin, y = margin;
        switch (position)
        {
            case "TopLeft":
                x = margin; y = margin; break;
            case "TopCenter":
                x = size / 2f; y = margin; break;
            case "TopRight":
                x = size - margin; y = margin; break;
            case "BottomLeft":
                x = margin; y = size - margin; break;
            case "BottomCenter":
                x = size / 2f; y = size - margin; break;
            case "BottomRight":
                x = size - margin; y = size - margin; break;
            case "Center":
                x = size / 2f; y = size / 2f; break;
        }

        try
        {
            var font = ImageSharpFonts.CreateFont("Arial", fontSize, ImageSharpFontStyle.Bold);
            image.Mutate(ctx =>
            {
                ctx.DrawText(text, font, textColor, new PointF(x, y));
            });
        }
        catch { }
    }

    private void ColorPickerBackground_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
    {
        UpdatePreview();
    }

    private void ColorPickerBorder_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
    {
        UpdatePreview();
    }

    private void ColorPickerText_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
    {
        UpdatePreview();
    }

    private void CmbTextPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePreview();
    }
}
}
