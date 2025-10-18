using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class JsonXmlFormatterPage : UserControl
    {
        private bool isDarkMode = true;
        private readonly JsonXmlFormatterService _service = new();

        public Brush EditorBackground => isDarkMode ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) : Brushes.White;
        public Brush ForegroundColor => isDarkMode ? Brushes.White : Brushes.Black;
        public string ThemeToggleText => isDarkMode ? "🌙 Karanlık" : "☀️ Aydınlık";

        public JsonXmlFormatterPage()
        {
            InitializeComponent();
            SetTheme();
        }

        // 🌗 Tema Değiştir
        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            isDarkMode = false;
            SetTheme();
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isDarkMode = true;
            SetTheme();
        }

        private void SetTheme()
        {
            CodeEditor.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(37, 37, 38)) : Brushes.White;
            CodeEditor.Foreground = isDarkMode ? Brushes.White : Brushes.Black;
            CodeEditor.SyntaxHighlighting = CodeEditor.SyntaxHighlighting ?? HighlightingManager.Instance.GetDefinition("JavaScript");
        }

        // 🧠 Otomatik Biçim Algılama
        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var text = (CodeEditor.Text ?? string.Empty).Trim();
                if (_service.IsJson(text))
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                else if (_service.IsXml(text))
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            }
            catch { /* sessiz */ }
        }

        // ✨ Biçimlendir
        private void Format_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = (CodeEditor.Text ?? string.Empty).Trim();
                if (_service.IsJson(text))
                    CodeEditor.Text = _service.BeautifyJson(text);
                else if (_service.IsXml(text))
                    CodeEditor.Text = _service.BeautifyXml(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Biçimlendirme hatası: {ex.Message}");
            }
        }

        // 📦 Sıkıştır
        private void Minify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = (CodeEditor.Text ?? string.Empty).Trim();
                if (_service.IsJson(text))
                    CodeEditor.Text = _service.MinifyJson(text);
                else if (_service.IsXml(text))
                    CodeEditor.Text = _service.MinifyXml(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sıkıştırma hatası: {ex.Message}");
            }
        }

        // ✅ Doğrula
        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = (CodeEditor.Text ?? string.Empty).Trim();
                if (_service.IsJson(text))
                {
                    if (_service.TryValidateJson(text, out var err)) MessageBox.Show("✅ JSON geçerli!");
                    else MessageBox.Show($"❌ JSON geçersiz: {err}");
                }
                else if (_service.IsXml(text))
                {
                    if (_service.TryValidateXml(text, out var err)) MessageBox.Show("✅ XML geçerli!");
                    else MessageBox.Show($"❌ XML geçersiz: {err}");
                }
                else MessageBox.Show("⚠️ JSON veya XML formatı algılanamadı.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Hatalı veri: {ex.Message}");
            }
        }

        // ➡️ JSON → XML
        private void JsonToXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var json = (CodeEditor.Text ?? string.Empty).Trim();
                CodeEditor.Text = _service.JsonToXml(json, "Root");
                CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dönüştürme hatası: {ex.Message}");
            }
        }

        // ⬅️ XML → JSON
        private void XmlToJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var xml = (CodeEditor.Text ?? string.Empty).Trim();
                CodeEditor.Text = _service.XmlToJson(xml);
                CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dönüştürme hatası: {ex.Message}");
            }
        }

        // 🧹 Temizle
        private void Clear_Click(object sender, RoutedEventArgs e) => CodeEditor.Clear();

        // 🔍 Sorgulama
        private void Query_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = QueryBox.Text;
                string text = (CodeEditor.Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(query)) return;
                string? result = null;
                if (_service.IsJson(text))
                    result = _service.JsonQuery(text, query);
                else if (_service.IsXml(text))
                    result = _service.XmlQuery(text, query);
                MessageBox.Show(result ?? "Sonuç bulunamadı.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorgu hatası: {ex.Message}");
            }
        }
    }
}
