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
                var confirmResult = MessageBox.Show("Bu işlem kalıcıdır. Tüm parolalar ve kategoriler silinecek. Devam etmek istiyor musunuz?",
                    "SON UYARI", MessageBoxButton.YesNo, MessageBoxImage.Stop);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    // Tüm parolaları sil
                    _dbService.DeleteAllPasswords();
                    // Tüm kategorileri sil (Genel hariç)
                    var categories = _dbService.GetAllCategories().Where(c => c.Id != 1).ToList();
                    foreach (var cat in categories)
                    {
                        _dbService.DeleteCategory(cat.Id);
                    }
                    RefreshPasswordList();
                    LoadData();
                    MessageBox.Show("Tüm parolalar ve kategoriler silindi!");
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

        private void DgPasswords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                _selectedEntry = entry;
                TxtDetailTitle.Text = entry.Title;
                TxtDetailPassword.Text = "****";
                // Yeni detaylar
                if (FindName("TxtDetailUsername") is TextBlock tbUser) tbUser.Text = entry.Username;                
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

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Yeni kategori adı girin:", "Kategori Ekle", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _dbService.AddCategory(input);
                LoadData();
                MessageBox.Show("Kategori eklendi!");
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CmbCategoryFilter.SelectedItem is PasswordCategory cat && cat.Id > 0)
            {
                var result = MessageBox.Show($"{cat.Name} kategorisini silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _dbService.DeleteCategory(cat.Id);
                    LoadData();
                    MessageBox.Show("Kategori silindi!");
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek bir kategori seçin!");
            }
        }

        // ============ İçe/Dışa Aktarma ============
        private void BtnExportPasswords_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Dosyası|*.csv",
                FileName = $"parolalar_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var passwords = _dbService.GetAllPasswords();
                    var categories = _dbService.GetAllCategories();

                    using (var writer = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // Kategori bilgilerini yaz
                        writer.WriteLine("### KATEGORILER ###");
                        writer.WriteLine("KategoriId,KategoriAdi,Renk");
                        foreach (var cat in categories)
                        {
                            writer.WriteLine($"{cat.Id},{EscapeCsv(cat.Name)},{EscapeCsv(cat.Color)}");
                        }

                        writer.WriteLine();
                        writer.WriteLine("### PAROLALAR ###");
                        writer.WriteLine("Id,Baslik,KullaniciAdi,SifreliParola,Url,Notlar,KategoriId,KategoriAdi,SonTarih,Guc,OlusturmaTarihi,DegistirmeTarihi");

                        foreach (var pwd in passwords)
                        {
                            var line = $"{pwd.Id}," +
                                      $"{EscapeCsv(pwd.Title)}," +
                                      $"{EscapeCsv(pwd.Username)}," +
                                      $"{EscapeCsv(pwd.EncryptedPassword)}," +
                                      $"{EscapeCsv(pwd.Url)}," +
                                      $"{EscapeCsv(pwd.Notes)}," +
                                      $"{pwd.CategoryId}," +
                                      $"{EscapeCsv(pwd.CategoryName)}," +
                                      $"{pwd.ExpiryDate?.ToString("yyyy-MM-dd")}," +
                                      $"{EscapeCsv(pwd.Strength)}," +
                                      $"{pwd.CreatedDate:yyyy-MM-dd HH:mm:ss}," +
                                      $"{pwd.ModifiedDate:yyyy-MM-dd HH:mm:ss}";
                            writer.WriteLine(line);
                        }
                    }

                    MessageBox.Show($"Parolalar başarıyla dışa aktarıldı!\nDosya: {dlg.FileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnImportPasswords_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Dosyası|*.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "İçe aktarma işlemi mevcut kategorileri ve parolaları etkileyebilir.\n\n" +
                    "• Aynı ID'ye sahip kategoriler güncellenecek\n" +
                    "• Yeni kategoriler eklenecek\n" +
                    "• Aynı ID'ye sahip parolalar ATLANACAK (yineleme önlenir)\n" +
                    "• Yeni parolalar eklenecek\n\n" +
                    "Devam etmek istiyor musunuz?",
                    "İçe Aktarma Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ImportPasswordsFromCsv(dlg.FileName);
                        RefreshPasswordList();
                        LoadData();
                        MessageBox.Show("Parolalar başarıyla içe aktarıldı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"İçe aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ImportPasswordsFromCsv(string filePath)
        {
            var lines = System.IO.File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            bool inCategories = false;
            bool inPasswords = false;

            var existingPasswordIds = _dbService.GetAllPasswords().Select(p => p.Id).ToHashSet();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.Contains("### KATEGORILER ###"))
                {
                    inCategories = true;
                    inPasswords = false;
                    continue;
                }
                else if (line.Contains("### PAROLALAR ###"))
                {
                    inCategories = false;
                    inPasswords = true;
                    continue;
                }
                else if (line.StartsWith("KategoriId,") || line.StartsWith("Id,"))
                {
                    continue; // Başlık satırlarını atla
                }

                if (inCategories)
                {
                    var parts = ParseCsvLine(line);
                    if (parts.Length >= 2)
                    {
                        var catId = int.Parse(parts[0]);
                        var catName = parts[1];
                        var catColor = parts.Length > 2 ? parts[2] : "#2196F3";

                        // Kategoriyi ekle veya güncelle
                        var existingCats = _dbService.GetAllCategories();
                        if (!existingCats.Any(c => c.Id == catId))
                        {
                            _dbService.AddCategory(catName, catColor);
                        }
                    }
                }
                else if (inPasswords)
                {
                    var parts = ParseCsvLine(line);
                    if (parts.Length >= 12)
                    {
                        var id = int.Parse(parts[0]);
                        
                        // Aynı ID'ye sahip parola varsa atla (yineleme önlenir)
                        if (existingPasswordIds.Contains(id))
                        {
                            continue;
                        }

                        var entry = new PasswordEntry
                        {
                            Title = parts[1],
                            Username = parts[2],
                            EncryptedPassword = parts[3], // Zaten şifrelenmiş
                            Url = parts[4],
                            Notes = parts[5],
                            CategoryId = int.Parse(parts[6]),
                            ExpiryDate = string.IsNullOrWhiteSpace(parts[8]) ? null : DateTime.Parse(parts[8]),
                            Strength = parts[9],
                            CreatedDate = DateTime.Parse(parts[10]),
                            ModifiedDate = DateTime.Parse(parts[11])
                        };

                            // Kategori mevcut mu kontrol et, yoksa 'Genel' (ID=1) ata
                            var existingCats = _dbService.GetAllCategories();
                            if (!existingCats.Any(c => c.Id == entry.CategoryId))
                            {
                                entry.CategoryId = 1; // Genel
                            }
                        // Parolayı doğrudan şifrelenmiş haliyle ekle
                        _dbService.AddPasswordEncrypted(entry);
                    }
                }
            }
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result.ToArray();
        }
    }
}
