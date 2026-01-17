using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SwissKnifeApp.Views
{
    public partial class RegionSelectionWindow : Window
    {
        private System.Windows.Point _startPoint;
        private System.Windows.Point _endPoint;
        private bool _isSelecting = false;
        private Bitmap? _screenshot;

        public event EventHandler<RegionSelectedEventArgs>? RegionSelected;

        public RegionSelectionWindow()
        {
            InitializeComponent();
            
            // Cover all screens (Multi-monitor support)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            
            // Capture background screenshot
            CaptureBackground();
            
            // Show instructions
            Cursor = System.Windows.Input.Cursors.Cross;
        }

        private void CaptureBackground()
        {
            try
            {
                // Use VirtualScreen to capture across all monitors
                int left = (int)SystemParameters.VirtualScreenLeft;
                int top = (int)SystemParameters.VirtualScreenTop;
                int width = (int)SystemParameters.VirtualScreenWidth;
                int height = (int)SystemParameters.VirtualScreenHeight;
                
                _screenshot = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(_screenshot))
                {
                    graphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                }

                // Set as background
                Background = new ImageBrush(BitmapToImageSource(_screenshot))
                {
                    Opacity = 0.3
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Screenshot hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _isSelecting = true;
            _startPoint = e.GetPosition(this);
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (_isSelecting)
            {
                _endPoint = e.GetPosition(this);
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            
            if (_isSelecting)
            {
                _isSelecting = false;
                _endPoint = e.GetPosition(this);
                
                CaptureSelectedRegion();
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_isSelecting)
            {
                var rect = GetSelectionRect();
                
                // Draw selection rectangle
                var brush = new SolidColorBrush(Colors.DodgerBlue) { Opacity = 0.3 };
                var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DodgerBlue, 2);
                
                drawingContext.DrawRectangle(brush, pen, rect);
                
                // Draw dimensions
                var text = new FormattedText(
                    $"{(int)rect.Width} × {(int)rect.Height}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    System.Windows.Media.Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                drawingContext.DrawText(text, new System.Windows.Point(rect.X, rect.Y - 20));
            }
        }

        private Rect GetSelectionRect()
        {
            var x = Math.Min(_startPoint.X, _endPoint.X);
            var y = Math.Min(_startPoint.Y, _endPoint.Y);
            var width = Math.Abs(_endPoint.X - _startPoint.X);
            var height = Math.Abs(_endPoint.Y - _startPoint.Y);
            
            return new Rect(x, y, width, height);
        }

        private void CaptureSelectedRegion()
        {
            try
            {
                var rect = GetSelectionRect();
                
                if (rect.Width < 5 || rect.Height < 5)
                {
                    Close();
                    return;
                }

                if (_screenshot == null)
                {
                    Close();
                    return;
                }

                // Crop screenshot to selected region
                var croppedBitmap = new Bitmap((int)rect.Width, (int)rect.Height);
                using (var graphics = Graphics.FromImage(croppedBitmap))
                {
                    graphics.DrawImage(
                        _screenshot,
                        new Rectangle(0, 0, (int)rect.Width, (int)rect.Height),
                        new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height),
                        GraphicsUnit.Pixel);
                }

                // Raise event
                RegionSelected?.Invoke(this, new RegionSelectedEventArgs
                {
                    Bitmap = croppedBitmap,
                    X = (int)rect.X,
                    Y = (int)rect.Y,
                    Width = (int)rect.Width,
                    Height = (int)rect.Height
                });

                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Region capture hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _screenshot?.Dispose();
        }
    }

    public class RegionSelectedEventArgs : EventArgs
    {
        public Bitmap? Bitmap { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
