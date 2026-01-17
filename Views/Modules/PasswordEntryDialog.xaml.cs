using System;
using System.Linq;
using System.Windows;
using System.Text;
using System.Security.Cryptography;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class PasswordEntryDialog : Window
    {
        private readonly PasswordDatabaseService _dbService;
        private readonly PasswordEntry? _editEntry;

        public PasswordEntryDialog(PasswordDatabaseService dbService, PasswordEntry? entry = null)
        {
            InitializeComponent();
            _dbService = dbService;
            _editEntry = entry;

            LoadCategories();

            if (_editEntry != null)
            {
                TxtDialogTitle.Text = "Kaydı Düzenle";
                TxtEntryTitle.Text = _editEntry.Title;
                TxtUsername.Text = _editEntry.Username;
                TxtPassword.Password = _dbService.DecryptPassword(_editEntry.EncryptedPassword);
                TxtUrl.Text = _editEntry.Url;
                TxtNotes.Text = _editEntry.Notes;
                TxtTotp.Text = _editEntry.TotpSecret;
                ChkIsNote.IsChecked = _editEntry.IsSecureNote;
                DpExpiry.SelectedDate = _editEntry.ExpiryDate;
                CmbCategory.SelectedValue = _editEntry.CategoryId;
            }
        }

        private void LoadCategories()
        {
            var categories = _dbService.GetAllCategories();
            CmbCategory.ItemsSource = categories;
            CmbCategory.SelectedValuePath = "Id";
            if (_editEntry == null) CmbCategory.SelectedIndex = 0;
        }

        private void BtnShowHide_Click(object sender, RoutedEventArgs e)
        {
            if (TxtPassword.Visibility == Visibility.Visible)
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPassword.Visibility = Visibility.Collapsed;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                IconEye.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.EyeOff;
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                IconEye.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Eye;
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var result = new char[16];
            for (int i = 0; i < 16; i++) result[i] = chars[bytes[i] % chars.Length];
            var pwd = new string(result);
            
            TxtPassword.Password = pwd;
            TxtPasswordVisible.Text = pwd;
        }

        private void ChkIsNote_Changed(object sender, RoutedEventArgs e)
        {
            if (PasswordSection != null)
            {
                PasswordSection.Visibility = ChkIsNote.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtEntryTitle.Text))
            {
                MessageBox.Show("Lütfen bir başlık girin.");
                return;
            }

            var password = TxtPassword.Visibility == Visibility.Visible ? TxtPassword.Password : TxtPasswordVisible.Text;

            if (_editEntry == null)
            {
                var newEntry = new PasswordEntry
                {
                    Title = TxtEntryTitle.Text,
                    Username = TxtUsername.Text,
                    EncryptedPassword = password,
                    Url = TxtUrl.Text,
                    Notes = TxtNotes.Text,
                    TotpSecret = TxtTotp.Text,
                    IsSecureNote = ChkIsNote.IsChecked ?? false,
                    CategoryId = (int)(CmbCategory.SelectedValue ?? 1),
                    ExpiryDate = DpExpiry.SelectedDate,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                _dbService.AddPassword(newEntry, password);
            }
            else
            {
                _editEntry.Title = TxtEntryTitle.Text;
                _editEntry.Username = TxtUsername.Text;
                _editEntry.Url = TxtUrl.Text;
                _editEntry.Notes = TxtNotes.Text;
                _editEntry.TotpSecret = TxtTotp.Text;
                _editEntry.IsSecureNote = ChkIsNote.IsChecked ?? false;
                _editEntry.CategoryId = (int)(CmbCategory.SelectedValue ?? 1);
                _editEntry.ExpiryDate = DpExpiry.SelectedDate;
                _editEntry.ModifiedDate = DateTime.Now;

                _dbService.UpdatePassword(_editEntry, password);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
