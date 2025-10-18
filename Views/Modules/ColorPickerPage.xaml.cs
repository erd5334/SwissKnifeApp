using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SwissKnifeApp.Services;
// using System.Text; // not needed

namespace SwissKnifeApp.Views.Modules
{
    public partial class ColorPickerPage : Page
    {
        private Color _selectedColor = Colors.Transparent;
        private readonly ColorPickerService _service = new();
        private readonly List<Color> _colorSamples = new()
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Cyan, Colors.Magenta,
            Colors.Black, Colors.White, Colors.Gray, Colors.Brown, Colors.Pink, Colors.Lime, Colors.Teal, Colors.Navy
        };

        public ColorPickerPage()
        {
            InitializeComponent();
            PopulateColorSamples();
            UpdateSelectedColor(Colors.Red);
        }

        private void BtnEyedropper_Click(object sender, RoutedEventArgs e)
        {
            // Open transparent overlay across all screens and pick any pixel
            var overlay = new EyedropperOverlayWindow
            {
                Cursor = Cursors.Cross,
                Topmost = true,
                ShowInTaskbar = false
            };
            var result = overlay.ShowDialog();
            if (result == true && overlay.PickedColor.HasValue)
            {
                UpdateSelectedColor(overlay.PickedColor.Value);
            }
        }

        private void PopulateColorSamples()
        {
            if (ColorSamplePanel == null) return;
            ColorSamplePanel.Children.Clear();
            foreach (var color in _colorSamples)
            {
                var rect = new Rectangle
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.Black,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    Tag = color
                };
                rect.MouseLeftButtonUp += ColorSample_Click;
                ColorSamplePanel.Children.Add(rect);
            }
        }

        private void ColorSample_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect && rect.Tag is Color color)
            {
                UpdateSelectedColor(color);
            }
        }

        private void UpdateSelectedColor(Color color)
        {
            _selectedColor = color;
            SelectedColorRect.Fill = new SolidColorBrush(color);
            HexCodeBox.Text = _service.ToHex(color);
            RgbCodeBox.Text = _service.ToRgb(color);
            PopulateCodeTabs(color);
        }

        private void BtnCopyHex_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(HexCodeBox.Text);
        }

        private void BtnCopyRgb_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(RgbCodeBox.Text);
        }

        private void PopulateCodeTabs(Color color)
        {
            CodeTabControl.Items.Clear();
            foreach (var lang in _service.CodeLanguages)
            {
                var tab = new TabItem { Header = lang };
                var codeBox = new TextBox
                {
                    Text = _service.GenerateCode(lang, color),
                    IsReadOnly = true,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap
                };
                tab.Content = codeBox;
                CodeTabControl.Items.Add(tab);
            }
        }

        // The following fields must be defined in XAML with x:Name to be available:
        // ColorSamplePanel, SelectedColorRect, HexCodeBox, RgbCodeBox, CodeTabControl
    }
}
