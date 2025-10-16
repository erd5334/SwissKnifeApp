using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SwissKnifeApp.Services;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PasswordToolsPage : Page
    {
        private readonly PasswordDatabaseService _dbService;
        private PasswordEntry? _selectedEntry;

        public PasswordToolsPage()
        {
            InitializeComponent();
            _dbService = new PasswordDatabaseService();
            LoadData();
        }

        private void LoadData()
        {
            // Kategorileri yükle
            var categories = _dbService.GetAllCategories();
            categories.Insert(0, new PasswordCategory { Id = 0, Name = "Tümü" });
            CmbCategoryFilter.ItemsSource = categories;
            CmbCategoryFilter.DisplayMemberPath = "Name";
            CmbCategoryFilter.SelectedIndex = 0;

            // Parolaları yükle
            RefreshPasswordList();
        }

        private void RefreshPasswordList()
        {
            var passwords = _dbService.GetAllPasswords();
            DgPasswords.ItemsSource = passwords;
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

        // ============ Parola Yöneticisi ============
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchText = TxtSearch.Text;
            var selectedCategory = CmbCategoryFilter.SelectedItem as PasswordCategory;
            int? categoryId = selectedCategory?.Id > 0 ? selectedCategory.Id : null;

            var results = _dbService.SearchPasswords(searchText, categoryId);
            DgPasswords.ItemsSource = results;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSearch_Click(sender, e);
        }

        private void CmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgPasswords != null)
                BtnSearch_Click(sender, e);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            CmbCategoryFilter.SelectedIndex = 0;
            RefreshPasswordList();
        }

        private void BtnAddPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PasswordEntryDialog(_dbService);
            if (dialog.ShowDialog() == true)
            {
                RefreshPasswordList();
            }
        }

        private void BtnEditPassword_Click(object sender, RoutedEventArgs e)
        {
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                var dialog = new PasswordEntryDialog(_dbService, entry);
                if (dialog.ShowDialog() == true)
                {
                    RefreshPasswordList();
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir parola seçin!");
            }
        }

        private void BtnDeletePassword_Click(object sender, RoutedEventArgs e)
        {
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                var result = MessageBox.Show($"{entry.Title} parolasını silmek istediğinize emin misiniz?",
                    "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _dbService.DeletePassword(entry.Id);
                    RefreshPasswordList();
                    MessageBox.Show("Parola silindi!");
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir parola seçin!");
            }
        }

        private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("TÜM parolaları silmek istediğinize emin misiniz? Bu işlem geri alınamaz!",
                "UYARI", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var confirmResult = MessageBox.Show("Bu işlem kalıcıdır. Devam etmek istiyor musunuz?",
                    "SON UYARI", MessageBoxButton.YesNo, MessageBoxImage.Stop);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    _dbService.DeleteAllPasswords();
                    RefreshPasswordList();
                    MessageBox.Show("Tüm parolalar silindi!");
                }
            }
        }

        private void BtnCopyPasswordEntry_Click(object sender, RoutedEventArgs e)
        {
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                var password = _dbService.DecryptPassword(entry.EncryptedPassword);
                if (!string.IsNullOrEmpty(password))
                {
                    Clipboard.SetText(password);
                    MessageBox.Show("Parola kopyalandı!");
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir parola seçin!");
            }
        }

        private void DgPasswords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                _selectedEntry = entry;
                TxtDetailTitle.Text = entry.Title;
                TxtDetailNotes.Text = entry.Notes;
                TxtDetailPassword.Text = "****";
            }
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntry != null)
            {
                var password = _dbService.DecryptPassword(_selectedEntry.EncryptedPassword);
                if (TxtDetailPassword.Text == "****")
                {
                    TxtDetailPassword.Text = password;
                    (sender as Button)!.Content = "🔒 Gizle";
                }
                else
                {
                    TxtDetailPassword.Text = "****";
                    (sender as Button)!.Content = "👁️ Göster";
                }
            }
        }
    }
}
