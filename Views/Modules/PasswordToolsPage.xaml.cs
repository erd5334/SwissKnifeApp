using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PasswordToolsPage : Page
    {
        public PasswordToolsPage()
        {
            InitializeComponent();
        }

        // 1️⃣ Şifre Oluşturma
        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lower = "abcdefghijklmnopqrstuvwxyz";
            string numbers = "0123456789";
            string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            StringBuilder pool = new();
            if (ChkUpper.IsChecked == true) pool.Append(upper);
            if (ChkLower.IsChecked == true) pool.Append(lower);
            if (ChkNumbers.IsChecked == true) pool.Append(numbers);
            if (ChkSymbols.IsChecked == true) pool.Append(symbols);

            if (pool.Length == 0)
            {
                MessageBox.Show("En az bir karakter türü seçmelisiniz.");
                return;
            }

            int length = (int)SliderLength.Value;
            Random rnd = new();
            string result = new(Enumerable.Range(0, length)
                .Select(_ => pool[rnd.Next(pool.Length)]).ToArray());
            TxtGenerated.Text = result;
        }

        private void BtnCopyPassword_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtGenerated.Text);
            MessageBox.Show("Şifre panoya kopyalandı!");
        }

        // 2️⃣ Şifre Gücü Analizi
        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            string pwd = TxtPasswordCheck.Text;
            int score = 0;

            if (pwd.Length >= 8) score += 20;
            if (pwd.Any(char.IsUpper)) score += 20;
            if (pwd.Any(char.IsLower)) score += 20;
            if (pwd.Any(char.IsDigit)) score += 20;
            if (pwd.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) score += 20;

            ProgressStrength.Value = score;
            TxtStrengthLabel.Text = score switch
            {
                <= 40 => "Zayıf 🔴",
                <= 70 => "Orta 🟠",
                _ => "Güçlü 🟢"
            };
        }

        // 3️⃣ Hash Üretici
        private void BtnHash_Click(object sender, RoutedEventArgs e)
        {
            string input = TxtHashInput.Text;
            string algo = ((ComboBoxItem)CmbHashAlgo.SelectedItem).Content.ToString();
            string hash = ComputeHash(input, algo);
            TxtHashResult.Text = hash;
        }

        private static string ComputeHash(string input, string algo)
        {
            using HashAlgorithm hashAlg = algo switch
            {
                "MD5" => MD5.Create(),
                "SHA1" => SHA1.Create(),
                "SHA256" => SHA256.Create(),
                "SHA512" => SHA512.Create(),
                _ => SHA256.Create()
            };
            byte[] bytes = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        private void BtnCopyHash_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtHashResult.Text);
            MessageBox.Show("Hash panoya kopyalandı!");
        }

        // 4️⃣ AES Şifreleme / Çözme
        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string key = TxtAesKey.Password.PadRight(32, '0').Substring(0, 32);
                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] encrypted = encryptor.TransformFinalBlock(
                    Encoding.UTF8.GetBytes(TxtAesInput.Text), 0, TxtAesInput.Text.Length);

                TxtAesOutput.Text = Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void BtnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string key = TxtAesKey.Password.PadRight(32, '0').Substring(0, 32);
                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] cipherBytes = Convert.FromBase64String(TxtAesInput.Text);
                byte[] decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                TxtAesOutput.Text = Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }

        private void BtnCopyAes_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtAesOutput.Text);
            MessageBox.Show("Metin panoya kopyalandı!");
        }
    }
}
