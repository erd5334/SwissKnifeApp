using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SwissKnifeApp.Views.Modules
{
    public partial class ColorToolsPage : Page
    {
        public ObservableCollection<PaletteItem> Palette { get; } = new();
        public ObservableCollection<SimulationItem> Simulations { get; } = new();

        private bool _isUpdating = false;

        public ColorToolsPage()
        {
            InitializeComponent();
            IcPalette.ItemsSource = Palette;
            IcSimulation.ItemsSource = Simulations;

            TxtBaseColor.Text = "#7C4DFF"; // Default
            UpdateGradientPreview(null, null);
            UpdateContrastCheck(null, null);
            UpdateSimulation(null, null);
        }

        #region Palette & Harmony

        private void TxtBaseColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePalette();
        }

        private void CmbHarmonyRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePalette();
        }

        private void UpdatePalette()
        {
            if (_isUpdating || TxtBaseColor == null) return;
            string hex = TxtBaseColor.Text.Trim();
            if (!IsValidHex(hex)) return;

            Color baseColor = (Color)ColorConverter.ConvertFromString(hex);
            RectBaseColorPreview.Fill = new SolidColorBrush(baseColor);

            string rule = (CmbHarmonyRule.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Complementary";
            
            Palette.Clear();
            var colors = GetHarmonyColors(baseColor, rule);
            
            foreach (var c in colors)
            {
                Palette.Add(new PaletteItem { 
                    Hex = ColorToHex(c.Item1), 
                    Brush = new SolidColorBrush(c.Item1),
                    Label = c.Item2
                });
            }
        }

        private List<(Color, string)> GetHarmonyColors(Color baseColor, string rule)
        {
            var results = new List<(Color, string)>();
            results.Add((baseColor, "Ana Renk"));

            var hsv = ColorToHsv(baseColor);

            switch (rule)
            {
                case "Complementary":
                    results.Add((HsvToColor((hsv.h + 180) % 360, hsv.s, hsv.v), "Tamamlayıcı"));
                    break;
                case "Triadic":
                    results.Add((HsvToColor((hsv.h + 120) % 360, hsv.s, hsv.v), "Üçlü 1"));
                    results.Add((HsvToColor((hsv.h + 240) % 360, hsv.s, hsv.v), "Üçlü 2"));
                    break;
                case "Analogous":
                    results.Add((HsvToColor((hsv.h + 30) % 360, hsv.s, hsv.v), "Benzer 1"));
                    results.Add((HsvToColor((hsv.h + 330) % 360, hsv.s, hsv.v), "Benzer 2"));
                    break;
                case "Split":
                    results.Add((HsvToColor((hsv.h + 150) % 360, hsv.s, hsv.v), "Bölünmüş 1"));
                    results.Add((HsvToColor((hsv.h + 210) % 360, hsv.s, hsv.v), "Bölünmüş 2"));
                    break;
                case "Tetradic":
                    results.Add((HsvToColor((hsv.h + 90) % 360, hsv.s, hsv.v), "Dörtlü 1"));
                    results.Add((HsvToColor((hsv.h + 180) % 360, hsv.s, hsv.v), "Dörtlü 2"));
                    results.Add((HsvToColor((hsv.h + 270) % 360, hsv.s, hsv.v), "Dörtlü 3"));
                    break;
                case "Monochromatic":
                    results.Add((HsvToColor(hsv.h, hsv.s, Math.Max(0, hsv.v - 0.3)), "Koyu"));
                    results.Add((HsvToColor(hsv.h, Math.Max(0, hsv.s - 0.3), hsv.v), "Açık"));
                    results.Add((HsvToColor(hsv.h, hsv.s, Math.Min(1, hsv.v + 0.3)), "Çok Açık"));
                    break;
            }

            return results;
        }

        private void BtnRandomPalette_Click(object sender, RoutedEventArgs e)
        {
            _isUpdating = true;
            Random rnd = new Random();
            Color c = Color.FromRgb((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
            TxtBaseColor.Text = ColorToHex(c);
            _isUpdating = false;
            UpdatePalette();
        }

        private void PaletteColor_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PaletteItem item)
            {
                Clipboard.SetText(item.Hex);
                // Hint logic would go here
            }
        }

        #endregion

        #region Gradients

        private void UpdateGradientPreview(object? sender, EventArgs? e)
        {
            if (RectGradientPreview == null) return;
            string c1 = TxtGrad1.Text;
            string c2 = TxtGrad2.Text;
            double angle = SldGradAngle.Value;

            if (IsValidHex(c1) && IsValidHex(c2))
            {
                Color color1 = (Color)ColorConverter.ConvertFromString(c1);
                Color color2 = (Color)ColorConverter.ConvertFromString(c2);

                var brush = new LinearGradientBrush(color1, color2, angle);
                RectGradientPreview.Fill = brush;
                TxtGradientCss.Text = $"background: linear-gradient({angle}deg, {c1}, {c2});";
            }
        }

        private void BtnCopyGradientCss_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtGradientCss.Text);
        }

        #endregion

        #region Contrast Checker

        private void UpdateContrastCheck(object? sender, TextChangedEventArgs? e)
        {
            if (TxtContrastRatio == null) return;
            string f = TxtContrastFore.Text;
            string b = TxtContrastBack.Text;

            if (IsValidHex(f) && IsValidHex(b))
            {
                Color fore = (Color)ColorConverter.ConvertFromString(f);
                Color back = (Color)ColorConverter.ConvertFromString(b);

                double ratio = GetContrastRatio(fore, back);
                TxtContrastRatio.Text = $"{ratio:F1}:1";

                TxtWcagNormal.Text = ratio >= 4.5 ? "GEÇTİ (AA)" : "KALDI";
                TxtWcagNormal.Foreground = ratio >= 4.5 ? Brushes.Green : Brushes.Red;
                
                TxtWcagLarge.Text = ratio >= 3.0 ? "GEÇTİ (AA)" : "KALDI";
                TxtWcagLarge.Foreground = ratio >= 3.0 ? Brushes.Green : Brushes.Red;

                BrdContrastPreview.Background = new SolidColorBrush(back);
                TxtContrastPreviewMain.Foreground = new SolidColorBrush(fore);
                TxtContrastPreviewSub.Foreground = new SolidColorBrush(fore);
            }
        }

        private double GetContrastRatio(Color c1, Color c2)
        {
            double l1 = GetRelativeLuminance(c1);
            double l2 = GetRelativeLuminance(c2);
            return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
        }

        private double GetRelativeLuminance(Color c)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        #endregion

        #region Simulation

        private void UpdateSimulation(object? sender, TextChangedEventArgs? e)
        {
            if (IcSimulation == null) return;
            string hex = TxtSimulatorColor.Text;
            if (IsValidHex(hex))
            {
                Color c = (Color)ColorConverter.ConvertFromString(hex);
                BrdSimSourceColor.Background = new SolidColorBrush(c);

                Simulations.Clear();
                Simulations.Add(new SimulationItem { Name = "Normal", Description = "Renk körlüğü yok", Brush = new SolidColorBrush(c), Hex = ColorToHex(c) });
                
                // Simplified Simulation Logic (using basic matrices for common types)
                Simulations.Add(CreateSim(c, "Protanopia", "Kırmızı duyarsızlığı", 0.567, 0.433, 0, 0.558, 0.442, 0, 0, 0.242, 0.758));
                Simulations.Add(CreateSim(c, "Deuteranopia", "Yeşil duyarsızlığı", 0.625, 0.375, 0, 0.7, 0.3, 0, 0, 0.3, 0.7));
                Simulations.Add(CreateSim(c, "Tritanopia", "Mavi duyarsızlığı", 0.95, 0.05, 0, 0, 0.433, 0.567, 0, 0.475, 0.525));
                Simulations.Add(CreateSim(c, "Achromatopsia", "Tam renk körlüğü", 0.299, 0.587, 0.114, 0.299, 0.587, 0.114, 0.299, 0.587, 0.114));
            }
        }

        private SimulationItem CreateSim(Color c, string name, string desc, params double[] m)
        {
            byte r = (byte)Math.Clamp(c.R * m[0] + c.G * m[1] + c.B * m[2], 0, 255);
            byte g = (byte)Math.Clamp(c.R * m[3] + c.G * m[4] + c.B * m[5], 0, 255);
            byte b = (byte)Math.Clamp(c.R * m[6] + c.G * m[7] + c.B * m[8], 0, 255);
            Color sim = Color.FromRgb(r, g, b);
            return new SimulationItem { Name = name, Description = desc, Brush = new SolidColorBrush(sim), Hex = ColorToHex(sim) };
        }

        #endregion

        #region Helpers

        private bool IsValidHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return false;
            if (!hex.StartsWith("#")) return false;
            return hex.Length == 7 || hex.Length == 9;
        }

        private string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private (double h, double s, double v) ColorToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else if (max == b) h = 60 * (((r - g) / delta) + 4);
            }

            if (h < 0) h += 360;

            double s = (max == 0) ? 0 : (delta / max);
            double v = max;

            return (h, s, v);
        }

        private Color HsvToColor(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double r1 = 0, g1 = 0, b1 = 0;
            if (h < 60) { r1 = c; g1 = x; }
            else if (h < 120) { r1 = x; g1 = c; }
            else if (h < 180) { g1 = c; b1 = x; }
            else if (h < 240) { g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; b1 = c; }
            else { r1 = c; b1 = x; }

            return Color.FromRgb((byte)((r1 + m) * 255), (byte)((g1 + m) * 255), (byte)((b1 + m) * 255));
        }

        #endregion
    }

    public class PaletteItem
    {
        public string Hex { get; set; } = "";
        public Brush Brush { get; set; } = Brushes.Transparent;
        public string Label { get; set; } = "";
    }

    public class SimulationItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Brush Brush { get; set; } = Brushes.Transparent;
        public string Hex { get; set; } = "";
    }
}
