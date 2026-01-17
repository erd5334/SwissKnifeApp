using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OtpNet;
using MahApps.Metro.IconPacks;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PasswordToolsPage : Page
    {
        private readonly PasswordDatabaseService _dbService;
        private PasswordEntry? _selectedEntry;
        private DispatcherTimer _totpTimer;
        private DispatcherTimer _autoLockTimer;
        private DateTime _lastActivity;

        public PasswordToolsPage()
        {
            InitializeComponent();
            _dbService = new PasswordDatabaseService();
            
            _totpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _totpTimer.Tick += TotpTimer_Tick;

            _autoLockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _autoLockTimer.Tick += AutoLockTimer_Tick;

            _lastActivity = DateTime.Now;
            
            CheckVaultStatus();
        }

        private void CheckVaultStatus()
        {
            if (!_dbService.IsMasterPasswordSet())
            {
                TxtVaultStatus.Text = "Yeni Kasa Oluştur";
                TxtVaultMessage.Text = "Henüz bir master parola ayarlanmamış. Tüm verilerinizi güvenle saklamak için bir master parola belirleyin.";
                LoginPanel.Visibility = Visibility.Collapsed;
                SetPasswordPanel.Visibility = Visibility.Visible;
            }
            else
            {
                TxtVaultStatus.Text = "Kasa Kilitli";
                TxtVaultMessage.Text = "Verilerinize erişmek için master parolanızı girin.";
                LoginPanel.Visibility = Visibility.Visible;
                SetPasswordPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TotpTimer_Tick(object? sender, EventArgs e)
        {
            if (_selectedEntry != null && !string.IsNullOrEmpty(_selectedEntry.TotpSecret))
            {
                try
                {
                    var base32Bytes = Base32Encoding.ToBytes(_selectedEntry.TotpSecret);
                    var totp = new Totp(base32Bytes);
                    TxtTotpCode.Text = totp.ComputeTotp();
                    ProgressTotp.Value = totp.RemainingSeconds();
                }
                catch
                {
                    TxtTotpCode.Text = "HATA";
                }
            }
        }

        private void AutoLockTimer_Tick(object? sender, EventArgs e)
        {
            if (ChkAutoLock.IsChecked == true && _dbService.IsUnlocked)
            {
                if ((DateTime.Now - _lastActivity).TotalMinutes >= 5)
                {
                    LockVault();
                }
            }
        }

        private void UserActivityDetected()
        {
            _lastActivity = DateTime.Now;
        }

        private void LockVault()
        {
            _dbService.Lock();
            VaultContent.Visibility = Visibility.Collapsed;
            VaultOverlay.Visibility = Visibility.Visible;
            _totpTimer.Stop();
            _autoLockTimer.Stop();
            TxtMasterPassword.Clear();
            CheckVaultStatus();
        }

        // ============ Event Handlers ============

        private void BtnSetMasterPassword_Click(object sender, RoutedEventArgs e)
        {
            var p1 = TxtNewMasterPassword.Password;
            var p2 = TxtConfirmMasterPassword.Password;

            if (p1.Length < 8)
            {
                MessageBox.Show("Master parola en az 8 karakter olmalıdır!");
                return;
            }

            if (p1 != p2)
            {
                MessageBox.Show("Parolalar eşleşmiyor!");
                return;
            }

            _dbService.SetMasterPassword(p1);
            MessageBox.Show("Master parola başarıyla ayarlandı. Artık kasayı açabilirsiniz.");
            CheckVaultStatus();
        }

        private void BtnUnlockVault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _dbService.Unlock(TxtMasterPassword.Password);
                VaultOverlay.Visibility = Visibility.Collapsed;
                VaultContent.Visibility = Visibility.Visible;
                LoadData();
                _autoLockTimer.Start();
                UserActivityDetected();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtMasterPassword.Clear();
            }
        }

        private void TxtMasterPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnUnlockVault_Click(sender, e);
        }

        private void BtnLockVault_Click(object sender, RoutedEventArgs e)
        {
            LockVault();
        }

        private void LoadData()
        {
            var categories = _dbService.GetAllCategories();
            CmbCategoryFilter.ItemsSource = new List<PasswordCategory> { new PasswordCategory { Id = 0, Name = "Tümü" } }.Concat(categories);
            CmbCategoryFilter.SelectedIndex = 0;
            
            LstCategories.ItemsSource = categories;
            RefreshPasswordList();
        }

        private void RefreshPasswordList()
        {
            DgPasswords.ItemsSource = _dbService.GetAllPasswords();
        }

        // 1️⃣ Şifre Oluşturucu
        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lower = "abcdefghijklmnopqrstuvwxyz";
            string numbers = "0123456789";
            string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            StringBuilder pool = new();
            if (ChkUpper.IsChecked == true) pool.Append(upper);
            if (ChkLower.IsChecked == true) pool.Append(lower);
            if (ChkNumbers.IsChecked == true) pool.Append(numbers);
            if (ChkSymbols.IsChecked == true) pool.Append(symbols);

            if (pool.Length == 0) return;

            int length = (int)SliderLength.Value;
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = pool[bytes[i] % pool.Length];
            }
            TxtGenerated.Text = new string(result);
        }

        private void BtnCopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtGenerated.Text))
                Clipboard.SetText(TxtGenerated.Text);
        }

        // 2️⃣ Şifre Gücü & Breach Check
        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            string pwd = TxtPasswordCheck.Password;
            if (string.IsNullOrEmpty(pwd)) return;

            int score = 0;
            if (pwd.Length >= 8) score += 20;
            if (pwd.Length >= 12) score += 10;
            if (pwd.Any(char.IsUpper)) score += 15;
            if (pwd.Any(char.IsLower)) score += 15;
            if (pwd.Any(char.IsDigit)) score += 20;
            if (pwd.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) score += 20;

            score = Math.Min(100, score);
            ProgressStrength.Value = score;
            TxtStrengthLabel.Text = score switch
            {
                < 40 => "Zayıf - Lütfen daha güçlü bir şifre seçin.",
                < 70 => "Orta - İyi, ama daha sembol ekleyebilirsiniz.",
                < 90 => "Güçlü - Güvenli bir şifre.",
                _ => "Mükemmel - Kırılması çok zor!"
            };
        }

        private async void BtnCheckBreach_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            string pwd = TxtPasswordCheck.Password;
            if (string.IsNullOrEmpty(pwd)) return;

            TxtBreachResult.Text = "Kontrol ediliyor...";
            TxtBreachResult.Foreground = System.Windows.Media.Brushes.Black;

            try
            {
                using var sha1 = SHA1.Create();
                var hash = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(pwd))).Replace("-", "");
                var prefix = hash.Substring(0, 5);
                var suffix = hash.Substring(5);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SwissKnifeApp");
                    var response = await client.GetStringAsync($"https://api.pwnedpasswords.com/range/{prefix}");
                    
                    var lines = response.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var match = lines.FirstOrDefault(l => l.StartsWith(suffix, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        var count = match.Split(':')[1];
                        TxtBreachResult.Text = $"DİKKAT! Bu şifre daha önce {count} kez sızıntılarda görülmüş. Kesinlikle kullanmayın!";
                        TxtBreachResult.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {
                        TxtBreachResult.Text = "Güvenli! Bu şifre bilinen büyük veri sızıntılarında bulunamadı.";
                        TxtBreachResult.Foreground = System.Windows.Media.Brushes.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                TxtBreachResult.Text = $"Hata: {ex.Message}";
            }
        }

        // ============ Vault Operations ============

        private void DgPasswords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserActivityDetected();
            if (DgPasswords.SelectedItem is PasswordEntry entry)
            {
                _selectedEntry = entry;
                GridDetails.Visibility = Visibility.Visible;
                TxtDetailTitle.Text = entry.Title;
                TxtDetailUsername.Text = entry.Username;
                TxtDetailPassword.Text = "********";
                BtnShowPassword.Content = new PackIconMaterial { Kind = PackIconMaterialKind.Eye, Width = 14, Height = 14 };

                if (!string.IsNullOrEmpty(entry.TotpSecret))
                {
                    _totpTimer.Start();
                }
                else
                {
                    _totpTimer.Stop();
                    TxtTotpCode.Text = "--- ---";
                    ProgressTotp.Value = 0;
                }
            }
        }

        private void BtnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            if (_selectedEntry != null)
            {
                if (TxtDetailPassword.Text == "********")
                {
                    TxtDetailPassword.Text = _dbService.DecryptPassword(_selectedEntry.EncryptedPassword);
                    BtnShowPassword.Content = new PackIconMaterial { Kind = PackIconMaterialKind.EyeOff, Width = 14, Height = 14 };
                }
                else
                {
                    TxtDetailPassword.Text = "********";
                    BtnShowPassword.Content = new PackIconMaterial { Kind = PackIconMaterialKind.Eye, Width = 14, Height = 14 };
                }
            }
        }

        private void BtnCopyUsername_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntry != null) Clipboard.SetText(_selectedEntry.Username);
        }

        private void BtnCopyPasswordEntry_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntry != null)
                Clipboard.SetText(_dbService.DecryptPassword(_selectedEntry.EncryptedPassword));
        }

        private void BtnAddPassword_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            var dialog = new PasswordEntryDialog(_dbService);
            if (dialog.ShowDialog() == true) RefreshPasswordList();
        }

        private void BtnEditPassword_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            if (_selectedEntry != null)
            {
                var dialog = new PasswordEntryDialog(_dbService, _selectedEntry);
                if (dialog.ShowDialog() == true) RefreshPasswordList();
            }
        }

        private void BtnDeletePassword_Click(object sender, RoutedEventArgs e)
        {
            UserActivityDetected();
            if (_selectedEntry != null)
            {
                if (MessageBox.Show("Bu parolayı silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _dbService.DeletePassword(_selectedEntry.Id);
                    RefreshPasswordList();
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UserActivityDetected();
            var search = TxtSearch.Text;
            var cat = CmbCategoryFilter.SelectedItem as PasswordCategory;
            DgPasswords.ItemsSource = _dbService.SearchPasswords(search, cat?.Id > 0 ? cat.Id : null);
        }

        private void CmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TxtSearch_TextChanged(sender, null!);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshPasswordList();
        }

        private void DgPasswords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnEditPassword_Click(sender, e);
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            // Basit bir input dialog (SwissKnifeApp içinde bir yerlerden çalınabilir veya yeni oluşturulabilir)
            var input = Microsoft.VisualBasic.Interaction.InputBox("Yeni kategori adı:", "Kategori", "");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _dbService.AddCategory(input);
                LoadData();
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (LstCategories.SelectedItem is PasswordCategory cat && cat.Id > 1)
            {
                _dbService.DeleteCategory(cat.Id);
                LoadData();
            }
        }

        private void BtnChangeMasterPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Bu özellik yakında eklenecek!");
        }

        private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("TÜM veriler silinecek. Emin misiniz?", "UYARI", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _dbService.DeleteAllPasswords();
                RefreshPasswordList();
            }
        }

        // ============ Import / Export ============
        private void BtnExportPasswords_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "CSV Dosyası|*.csv", FileName = $"vault_export_{DateTime.Now:yyyyMMdd}.csv" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var passwords = _dbService.GetAllPasswords();
                    var sb = new StringBuilder();
                    sb.AppendLine("Title,Username,EncryptedPassword,Url,Notes,CategoryId,TotpSecret,IsSecureNote");

                    foreach (var p in passwords)
                    {
                        sb.AppendLine($"{EscapeCsv(p.Title)},{EscapeCsv(p.Username)},{EscapeCsv(p.EncryptedPassword)},{EscapeCsv(p.Url)},{EscapeCsv(p.Notes)},{p.CategoryId},{EscapeCsv(p.TotpSecret)},{p.IsSecureNote}");
                    }

                    System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Dışa aktarma başarılı.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata: " + ex.Message);
                }
            }
        }

        private void BtnImportPasswords_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "CSV Dosyası|*.csv" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(dlg.FileName);
                    if (lines.Length <= 1) return;

                    int count = 0;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = ParseCsvLine(lines[i]);
                        if (parts.Length >= 8)
                        {
                            var entry = new PasswordEntry
                            {
                                Title = parts[0],
                                Username = parts[1],
                                EncryptedPassword = parts[2], // Assume it's already encrypted with the SAME master key or needs re-encryption
                                Url = parts[3],
                                Notes = parts[4],
                                CategoryId = int.Parse(parts[5]),
                                TotpSecret = parts[6],
                                IsSecureNote = bool.Parse(parts[7]),
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now
                            };
                            
                            // Since we might be importing from a different system, we should ideally decrypt with old key and re-encrypt with new key
                            // But for simplicity in this vault, we assume the exported file's encrypted column is handled
                            // Actually, let's assume the CSV contains PLAIN passwords for import if it's from another app, 
                            // but if it's our export, it's encrypted.
                            // For now, we'll treat 'EncryptedPassword' as the raw encrypted string.
                            
                            // Re-adding via DB service (it will be treated correctly if we call AddPasswordEncrypted)
                            _dbService.AddPasswordEncrypted(entry);
                            count++;
                        }
                    }
                    RefreshPasswordList();
                    MessageBox.Show($"{count} kayıt içe aktarıldı.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata: " + ex.Message);
                }
            }
        }

        private string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\""))
                return $"\"{val.Replace("\"", "\"\"")}\"";
            return val;
        }

        private string[] ParseCsvLine(string line)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\"') inQuotes = !inQuotes;
                else if (line[i] == ',' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(line[i]);
            }
            parts.Add(current.ToString());
            return parts.ToArray();
        }
    }
}
