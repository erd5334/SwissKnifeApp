using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class TextOperationsPage : Page
    {
        private readonly SwissKnifeApp.Services.TextOperationsService _textService = new SwissKnifeApp.Services.TextOperationsService();
        public TextOperationsPage()
        {
            InitializeComponent();
        }

        private void Uppercase_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.ToUpper(txtInput.Text);
        }

        private void Lowercase_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.ToLower(txtInput.Text);
        }

        private void TrimSpaces_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.TrimSpaces(txtInput.Text);
        }

        private void CountChars_Click(object sender, RoutedEventArgs e)
        {
            int count = _textService.CountChars(txtInput.Text);
            MessageBox.Show($"Toplam karakter sayısı: {count}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Clear();
        }

        // Kelime Sayacı
        private void CountWords_Click(object sender, RoutedEventArgs e)
        {
            int wordCount = _textService.CountWords(txtInput.Text);
            MessageBox.Show($"Toplam kelime sayısı: {wordCount}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Cümle/Paragraf Sayacı
        private void CountSentencesParagraphs_Click(object sender, RoutedEventArgs e)
        {
            int sentenceCount = _textService.CountSentences(txtInput.Text);
            int paragraphCount = _textService.CountParagraphs(txtInput.Text);
            MessageBox.Show($"Cümle sayısı: {sentenceCount}\nParagraf sayısı: {paragraphCount}", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Baş Harfleri Büyük Yap
        private void TitleCase_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.ToTitleCase(txtInput.Text);
        }

        // Metni Ters Çevir (karakter bazında)
        private void ReverseText_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.ReverseText(txtInput.Text);
        }

        // Metni Ters Çevir (kelime bazında)
        private void ReverseWords_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.ReverseWords(txtInput.Text);
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
            txtInput.Text = _textService.EncodeBase64(txtInput.Text);
        }

        // Metni Base64 ile Çöz
        private void DecodeBase64_Click(object sender, RoutedEventArgs e)
        {
            if (_textService.TryDecodeBase64(txtInput.Text, out var decoded))
                txtInput.Text = decoded;
            else
                MessageBox.Show("Geçersiz Base64 metni.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Lorem Ipsum Üretici
        private void GenerateLoremIpsum_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = _textService.GenerateLoremIpsum(30);
        }
    }
}
