using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PasswordEntryDialog : Window
    {
        private readonly PasswordDatabaseService _dbService;
        private readonly PasswordEntry? _editEntry;
        private readonly bool _isEditMode;

        public PasswordEntryDialog(PasswordDatabaseService dbService, PasswordEntry? editEntry = null)
        {
            InitializeComponent();
            _dbService = dbService;
            _editEntry = editEntry;
            _isEditMode = editEntry != null;

            LoadCategories();

            if (_isEditMode && _editEntry != null)
            {
                Title = "Parolayı Düzenle";
                TxtTitle.Text = _editEntry.Title;
                TxtUsername.Text = _editEntry.Username;
                TxtUrl.Text = _editEntry.Url;
                TxtNotes.Text = _editEntry.Notes;
                CmbCategory.SelectedValue = _editEntry.CategoryId;
                DpExpiryDate.SelectedDate = _editEntry.ExpiryDate;
                TxtStrength.Text = _editEntry.Strength;
                
                // Parolayı gösterme (güvenlik için boş bırakılabilir)
                var decryptedPassword = _dbService.DecryptPassword(_editEntry.EncryptedPassword);
                TxtPassword.Password = decryptedPassword;
            }
            else
            {
                Title = "Yeni Parola Ekle";
                CmbCategory.SelectedIndex = 0;
            }

            TxtPassword.PasswordChanged += (s, e) => UpdatePasswordStrength();
        }

        private void LoadCategories()
        {
            var categories = _dbService.GetAllCategories();
            CmbCategory.ItemsSource = categories;
            if (categories.Count > 0)
                CmbCategory.SelectedIndex = 0;
        }

        private void UpdatePasswordStrength()
        {
            var password = TxtPassword.Password;
            if (string.IsNullOrEmpty(password))
            {
                TxtStrength.Text = "";
                return;
            }

            int score = 0;
            if (password.Length >= 8) score += 20;
            if (password.Any(char.IsUpper)) score += 20;
            if (password.Any(char.IsLower)) score += 20;
            if (password.Any(char.IsDigit)) score += 20;
            if (password.Any(c => !char.IsLetterOrDigit(c))) score += 20;

            TxtStrength.Text = score switch
            {
                <= 40 => "Zayıf 🔴",
                <= 70 => "Orta 🟠",
                _ => "Güçlü 🟢"
            };
        }

        private void BtnGeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";
            const int length = 16;

            var result = new StringBuilder(length);
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[sizeof(uint)];

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(buffer);
                uint num = BitConverter.ToUInt32(buffer, 0);
                result.Append(charset[(int)(num % (uint)charset.Length)]);
            }

            TxtPassword.Password = result.ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitle.Text))
            {
                MessageBox.Show("Başlık boş olamaz!");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                MessageBox.Show("Parola boş olamaz!");
                return;
            }

            var entry = new PasswordEntry
            {
                Title = TxtTitle.Text,
                Username = TxtUsername.Text,
                Url = TxtUrl.Text,
                Notes = TxtNotes.Text,
                CategoryId = (int)(CmbCategory.SelectedValue ?? 1),
                ExpiryDate = DpExpiryDate.SelectedDate,
                Strength = TxtStrength.Text
            };

            try
            {
                if (_isEditMode && _editEntry != null)
                {
                    entry.Id = _editEntry.Id;
                    _dbService.UpdatePassword(entry, TxtPassword.Password);
                    MessageBox.Show("Parola güncellendi!");
                }
                else
                {
                    _dbService.AddPassword(entry, TxtPassword.Password);
                    MessageBox.Show("Parola eklendi!");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
