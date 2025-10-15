using ICSharpCode.AvalonEdit.Highlighting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace SwissKnifeApp.Views.Modules
{
    public partial class JsonXmlFormatterPage : UserControl
    {
        private bool isDarkMode = true;

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
                var text = CodeEditor.Text.Trim();
                if (text.StartsWith("{") || text.StartsWith("["))
                {
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                }
                else if (text.StartsWith("<"))
                {
                    CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                }
            }
            catch { /* sessiz */ }
        }

        // ✨ Biçimlendir
        private void Format_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = CodeEditor.Text.Trim();
                if (text.StartsWith("{") || text.StartsWith("["))
                {
                    var parsedJson = JToken.Parse(text);
                    CodeEditor.Text = parsedJson.ToString(Formatting.Indented);
                }
                else if (text.StartsWith("<"))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(text);
                    CodeEditor.Text = BeautifyXml(doc);
                }
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
                var text = CodeEditor.Text.Trim();
                if (text.StartsWith("{") || text.StartsWith("["))
                {
                    var parsedJson = JToken.Parse(text);
                    CodeEditor.Text = parsedJson.ToString(Formatting.None);
                }
                else if (text.StartsWith("<"))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(text);
                    CodeEditor.Text = doc.OuterXml;
                }
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
                var text = CodeEditor.Text.Trim();
                if (text.StartsWith("{") || text.StartsWith("["))
                {
                    JToken.Parse(text);
                    MessageBox.Show("✅ JSON geçerli!");
                }
                else if (text.StartsWith("<"))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(text);
                    MessageBox.Show("✅ XML geçerli!");
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
                var json = CodeEditor.Text.Trim();
                XmlDocument doc = JsonConvert.DeserializeXmlNode(json, "Root");
                CodeEditor.Text = BeautifyXml(doc);
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
                var xml = CodeEditor.Text.Trim();
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                string json = JsonConvert.SerializeXmlNode(doc, Formatting.Indented, true);
                CodeEditor.Text = json;
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
                string text = CodeEditor.Text.Trim();
                if (string.IsNullOrEmpty(query)) return;

                if (text.StartsWith("{") || text.StartsWith("["))
                {
                    var token = JToken.Parse(text).SelectToken(query);
                    MessageBox.Show(token?.ToString() ?? "Sonuç bulunamadı.");
                }
                else if (text.StartsWith("<"))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(text);
                    var node = doc.SelectSingleNode(query);
                    MessageBox.Show(node?.OuterXml ?? "Sonuç bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorgu hatası: {ex.Message}");
            }
        }

        private string BeautifyXml(XmlDocument doc)
        {
            using var sw = new StringWriter();
            using var xw = new XmlTextWriter(sw) { Formatting = System.Xml.Formatting.Indented };
            doc.WriteTo(xw);
            return sw.ToString();
        }
    }
}
