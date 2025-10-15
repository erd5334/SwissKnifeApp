using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class TextOperationsPage : UserControl
    {
        public TextOperationsPage()
        {
            InitializeComponent();
        }

        private void Uppercase_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = txtInput.Text.ToUpper();
        }

        private void Lowercase_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = txtInput.Text.ToLower();
        }

        private void TrimSpaces_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = string.Join(" ", txtInput.Text.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        private void CountChars_Click(object sender, RoutedEventArgs e)
        {
            int count = txtInput.Text.Length;
            MessageBox.Show($"Toplam karakter sayısı: {count}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Clear();
        }

        // Kelime Sayacı
        private void CountWords_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            int wordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            MessageBox.Show($"Toplam kelime sayısı: {wordCount}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Cümle/Paragraf Sayacı
        private void CountSentencesParagraphs_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            int sentenceCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            int paragraphCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            MessageBox.Show($"Cümle sayısı: {sentenceCount}\nParagraf sayısı: {paragraphCount}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Baş Harfleri Büyük Yap
        private void TitleCase_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            var result = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
            txtInput.Text = result;
        }

        // Metni Ters Çevir (karakter bazında)
        private void ReverseText_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            var reversed = new string(text.Reverse().ToArray());
            txtInput.Text = reversed;
        }

        // Metni Ters Çevir (kelime bazında)
        private void ReverseWords_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            txtInput.Text = string.Join(" ", words.Reverse());
        }

        // Metni Kopyala
        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtInput.Text);
            MessageBox.Show("Metin panoya kopyalandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Metni Base64 ile Şifrele
        private void EncodeBase64_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            var encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            txtInput.Text = encoded;
        }

        // Metni Base64 ile Çöz
        private void DecodeBase64_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text;
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(text));
                txtInput.Text = decoded;
            }
            catch
            {
                MessageBox.Show("Geçersiz Base64 metni.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Lorem Ipsum Üretici
        private void GenerateLoremIpsum_Click(object sender, RoutedEventArgs e)
        {
            int wordCount = 30; // Sabit, isterseniz parametreli yapabilirsiniz
            string[] loremWords = new[] {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "ut", "enim", "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi"
            };
            var rnd = new System.Random();
            var result = string.Join(" ", Enumerable.Range(0, wordCount).Select(i => loremWords[rnd.Next(loremWords.Length)]));
            txtInput.Text = result;
        }
    }
}
