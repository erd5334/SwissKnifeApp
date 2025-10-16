using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
// using System.Text; // not needed

namespace SwissKnifeApp.Views.Modules
{
    public partial class ColorPickerPage : Page
    {
        private Color _selectedColor = Colors.Transparent;
        private readonly List<Color> _colorSamples = new()
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Orange, Colors.Purple, Colors.Cyan, Colors.Magenta,
            Colors.Black, Colors.White, Colors.Gray, Colors.Brown, Colors.Pink, Colors.Lime, Colors.Teal, Colors.Navy
        };
        private readonly List<string> _codeLanguages = new()
        {
            "XAML Background", "XAML Foreground", "React (sx)", "React (inline style)", "XAML", "CSS", "C#", "HTML", "Python", "VB.NET", "Excel VBA", "NET MAUI", "Flutter", "Java", "Kotlin", "Swift", "Objective-C", "Android XML", "SCSS", "LESS", "Tailwind CSS", "QML", "Unity C#", "MATLAB", "Figma", "SVG", "Dart", "PowerShell", "Rust", "Go", "C++ (Qt)", "Delphi", "PHP", "Laravel"
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
            HexCodeBox.Text = ColorToHex(color);
            RgbCodeBox.Text = ColorToRgb(color);
            PopulateCodeTabs(color);
        }

        private string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        private string ColorToRgb(Color color) => $"rgb({color.R}, {color.G}, {color.B})";

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
            string hex = ColorToHex(color);
            string rgb = ColorToRgb(color);
            foreach (var lang in _codeLanguages)
            {
                var tab = new TabItem { Header = lang };
                var codeBox = new TextBox
                {
                    Text = GenerateCodeBlock(lang, hex, rgb),
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

        private string GenerateCodeBlock(string lang, string hex, string rgb)
        {
            return lang switch
            {
                "XAML Background" => $"Background=\"{hex}\"",
                "XAML Foreground" => $"Foreground=\"{hex}\"",
                "React (sx)" => $"sx={{ backgroundColor: '{hex}' }}",
                "React (inline style)" => $"style={{ backgroundColor: '{hex}' }}",
                "XAML" => $"<SolidColorBrush Color=\"{hex}\" />",
                "CSS" => $"background-color: {hex};",
                "C#" => $"Color color = (Color)ColorConverter.ConvertFromString(\"{hex}\");",
                "HTML" => $"<div style=\"background:{hex};\"></div>",
                "Python" => $"color = '{hex}'",
                "VB.NET" => $"Dim color As Color = ColorTranslator.FromHtml(\"{hex}\")",
                "Excel VBA" => $"Range(\"A1\").Interior.Color = RGB({_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B})",
                "NET MAUI" => $"Color.FromArgb(\"{hex}\")",
                "Flutter" => $"Color(0xFF{hex.Substring(1)})",
                "Java" => $"Color color = Color.decode(\"{hex}\");",
                "Kotlin" => $"val color = Color.parseColor(\"{hex}\")",
                "Swift" => $"let color = UIColor(hex: \"{hex}\")",
                "Objective-C" => $"UIColor *color = [UIColor colorWithHexString: @\"{hex}\"];",
                "Android XML" => $"<color name=\"custom\">{hex}</color>",
                "SCSS" => $"$color: {hex};",
                "LESS" => $"@color: {hex};",
                "Tailwind CSS" => $"bg-[{hex}]",
                "QML" => $"color: \"{hex}\"",
                "Unity C#" => $"Color color = new Color32({_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B}, 255);",
                "MATLAB" => $"color = [{_selectedColor.R}/255, {_selectedColor.G}/255, {_selectedColor.B}/255];",
                "Figma" => $"HEX: {hex} | RGB: {_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B}",
                "SVG" => $"fill=\"{hex}\"",
                "Dart" => $"Color(0xFF{hex.Substring(1)})",
                "PowerShell" => $"$color = '{hex}'",
                "Rust" => $"let color = \"{hex}\";",
                "Go" => $"color := \"{hex}\"",
                "C++ (Qt)" => $"QColor color(\"{hex}\");",
                "Delphi" => $"Color := StringToColor(\"{hex}\");",
                "PHP" => $"$color = '{hex}';",
                "Laravel" => $"$color = '{hex}';",
                _ => hex
            };
        }

        // The following fields must be defined in XAML with x:Name to be available:
        // ColorSamplePanel, SelectedColorRect, HexCodeBox, RgbCodeBox, CodeTabControl
    }
}
