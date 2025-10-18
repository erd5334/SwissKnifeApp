using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using SwissKnifeApp.Services;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Views.Modules
{
    public partial class FileManagerPage : Page
    {
        private readonly FileManagerService _service = new();
        private string? _file1Content;
        private string? _file2Content;
        private ObservableCollection<FileRenameItem> _files = new();

        public FileManagerPage()
        {
            InitializeComponent();
            DgFiles.ItemsSource = _files;
            TxtCustomTemplate.Text = "{name}_{date}";
        }
        // =============================
        // DOSYA ŞİFRELEME/ÇÖZME EVENT HANDLER STUB'LARI
        // =============================
        // AES ile dosya şifreleme
        private void BtnEncryptFile_Click(object sender, RoutedEventArgs e)
        {
            var filePath = TxtEncryptFilePath.Text;
            var password = PwdEncryptPassword.Password;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                TxtEncryptStatus.Text = "Geçerli bir dosya seçin.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                TxtEncryptStatus.Text = "Parola girin.";
                return;
            }
            try
            {
                var tempPath = filePath + ".tmp";
                _service.EncryptFile(filePath, tempPath, password);
                File.Delete(filePath);
                File.Move(tempPath, filePath);
                TxtEncryptStatus.Text = $"✅ Şifreleme başarılı: {Path.GetFileName(filePath)}";
                MessageBox.Show("Dosya başarıyla şifrelendi!\n\nUYARI: Orijinal dosya artık şifrelenmiş durumda.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtEncryptStatus.Text = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Şifreleme sırasında hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // AES ile dosya çözme
        private void BtnDecryptFile_Click(object sender, RoutedEventArgs e)
        {
            var filePath = TxtEncryptFilePath.Text;
            var password = PwdEncryptPassword.Password;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                TxtEncryptStatus.Text = "Geçerli bir dosya seçin.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                TxtEncryptStatus.Text = "Parola girin.";
                return;
            }
            try
            {
                var tempPath = filePath + ".tmp";
                _service.DecryptFile(filePath, tempPath, password);
                File.Delete(filePath);
                File.Move(tempPath, filePath);
                TxtEncryptStatus.Text = $"✅ Çözme başarılı: {Path.GetFileName(filePath)}";
                MessageBox.Show("Dosya başarıyla çözüldü!\n\nDosya artık orijinal haline döndü.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtEncryptStatus.Text = $"❌ Hata: {ex.Message}";
                var tempPath = filePath + ".tmp";
                if (File.Exists(tempPath)) File.Delete(tempPath);
                MessageBox.Show($"Çözme sırasında hata oluştu:\n{ex.Message}\n\nYanlış parola veya bozuk dosya olabilir.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Dosya seçimi
        private void BtnSelectEncryptFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Tüm Dosyalar|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtEncryptFilePath.Text = dlg.FileName;
                TxtEncryptStatus.Text = "Dosya seçildi.";
            }
        }

        // Sürükle-bırak ile dosya seçimi
        private void EncryptFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    TxtEncryptFilePath.Text = files[0];
                    TxtEncryptStatus.Text = "Dosya seçildi (sürükle-bırak).";
                }
            }
        }

        private void EncryptFile_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        // Parola göster/gizle
        private void ChkShowEncryptPassword_Checked(object sender, RoutedEventArgs e)
        {
            // PasswordBox'ı TextBox'a dönüştür
            var parent = (StackPanel)PwdEncryptPassword.Parent;
            var password = PwdEncryptPassword.Password;
            var txt = new TextBox { Text = password, Width = PwdEncryptPassword.Width, Margin = PwdEncryptPassword.Margin, Name = "TxtEncryptPassword" };
            parent.Children.Remove(PwdEncryptPassword);
            parent.Children.Insert(0, txt);
        }

        private void ChkShowEncryptPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            // TextBox'ı PasswordBox'a geri döndür
            var parent = (StackPanel)ChkShowEncryptPassword.Parent;
            var txt = parent.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == "TxtEncryptPassword");
            if (txt != null)
            {
                var pwd = new PasswordBox { Width = txt.Width, Margin = txt.Margin, Name = "PwdEncryptPassword" };
                pwd.Password = txt.Text;
                parent.Children.Remove(txt);
                parent.Children.Insert(0, pwd);
                PwdEncryptPassword = pwd;
            }
        }

        // AES Şifreleme Fonksiyonu
        private void EncryptFileAES(string inputPath, string outputPath, string password)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            var salt = System.Text.Encoding.UTF8.GetBytes("SwissKnifeSalt2025");
            var key = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);
            using var fsInput = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var fsOutput = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var cs = new System.Security.Cryptography.CryptoStream(fsOutput, aes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
            fsInput.CopyTo(cs);
        }

        // AES Çözme Fonksiyonu
        private void DecryptFileAES(string inputPath, string outputPath, string password)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            var salt = System.Text.Encoding.UTF8.GetBytes("SwissKnifeSalt2025");
            var key = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);
            using var fsInput = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var fsOutput = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var cs = new System.Security.Cryptography.CryptoStream(fsInput, aes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Read);
            cs.CopyTo(fsOutput);
        }

        // ============================================
        // 1️⃣ DOSYA KARŞILAŞTIRICI (DIFF) BÖLÜMÜ
        // ============================================

        private void BtnSelectFile1_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Tüm Dosyalar|*.*|Metin Dosyaları|*.txt|Kod Dosyaları|*.cs;*.js;*.py;*.html;*.css"
            };

            if (dlg.ShowDialog() == true)
            {
                TxtFile1Path.Text = dlg.FileName;
                _file1Content = File.ReadAllText(dlg.FileName);
                TxtLeftContent.Text = _file1Content;
            }
        }

        private void BtnSelectFile2_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Tüm Dosyalar|*.*|Metin Dosyaları|*.txt|Kod Dosyaları|*.cs;*.js;*.py;*.html;*.css"
            };

            if (dlg.ShowDialog() == true)
            {
                TxtFile2Path.Text = dlg.FileName;
                _file2Content = File.ReadAllText(dlg.FileName);
                TxtRightContent.Text = _file2Content;
            }
        }

        private void BtnEnterText1_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("Sol Metin Girişi");
            if (dialog.ShowDialog() == true)
            {
                _file1Content = dialog.InputText;
                TxtFile1Path.Text = "[Manuel Metin Girişi]";
                TxtLeftContent.Text = _file1Content;
            }
        }

        private void BtnEnterText2_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("Sağ Metin Girişi");
            if (dialog.ShowDialog() == true)
            {
                _file2Content = dialog.InputText;
                TxtFile2Path.Text = "[Manuel Metin Girişi]";
                TxtRightContent.Text = _file2Content;
            }
        }

        private void BtnCompare_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_file1Content) || string.IsNullOrEmpty(_file2Content))
            {
                MessageBox.Show("Lütfen her iki dosya/metin içeriğini de seçin veya girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text1 = _file1Content;
            var text2 = _file2Content;

            // Ayarlara göre ön işleme
            if (ChkIgnoreWhitespace.IsChecked == true)
            {
                text1 = Regex.Replace(text1, @"\s+", " ").Trim();
                text2 = Regex.Replace(text2, @"\s+", " ").Trim();
            }

            if (ChkIgnoreCase.IsChecked == true)
            {
                text1 = text1.ToLowerInvariant();
                text2 = text2.ToLowerInvariant();
            }

            // Karşılaştırma modu
            var mode = CmbDiffMode.SelectedIndex;
            string stats = "";

            if (mode == 0) // Satır bazında
            {
                var (leftHighlighted, rightHighlighted, addedCount, removedCount, changedCount) =
                    _service.CompareLineByLine(text1, text2, ChkIgnoreWhitespace.IsChecked == true, ChkIgnoreCase.IsChecked == true);
                TxtLeftContent.Text = leftHighlighted;
                TxtRightContent.Text = rightHighlighted;
                stats = $"✅ Eklenen: {addedCount} satır | ❌ Silinen: {removedCount} satır | ✏️ Değiştirilen: {changedCount} satır";
            }
            else if (mode == 1) // Sözcük bazında
            {
                var (leftHighlighted, rightHighlighted, diffCount) =
                    _service.CompareWordByWord(text1, text2, ChkIgnoreWhitespace.IsChecked == true, ChkIgnoreCase.IsChecked == true);
                TxtLeftContent.Text = leftHighlighted;
                TxtRightContent.Text = rightHighlighted;
                stats = $"📝 Farklı sözcük sayısı: {diffCount}";
            }
            else if (mode == 2) // Karakter bazında
            {
                var (similarity, diffCount) = _service.CompareCharByChar(text1, text2);
                TxtLeftContent.Text = text1;
                TxtRightContent.Text = text2;
                stats = $"🔍 Benzerlik: %{similarity:F2} | Farklı karakter: {diffCount}";
            }

            TxtDiffStats.Text = stats;
        }

        // Compare methods moved to FileManagerService

        // ============================================
        // 2️⃣ TOPLU YENİDEN ADLANDIRICI BÖLÜMÜ
        // ============================================

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFolderPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnLoadFiles_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtFolderPath.Text) || !Directory.Exists(TxtFolderPath.Text))
            {
                MessageBox.Show("Lütfen geçerli bir klasör seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _files.Clear();

            var searchOption = ChkIncludeSubfolders.IsChecked == true 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;

            var filter = string.IsNullOrWhiteSpace(TxtFileFilter.Text) ? "*.*" : TxtFileFilter.Text;

            try
            {
                var files = Directory.GetFiles(TxtFolderPath.Text, filter, searchOption);
                foreach (var file in files)
                {
                    _files.Add(new FileRenameItem
                    {
                        IsSelected = true,
                        OriginalName = Path.GetFileName(file),
                        NewName = Path.GetFileName(file),
                        Extension = Path.GetExtension(file),
                        FullPath = file
                    });
                }

                MessageBox.Show($"{_files.Count} dosya yüklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosyalar yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbRenameMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlSequential == null) return;

            PnlSequential.Visibility = Visibility.Collapsed;
            PnlDateTime.Visibility = Visibility.Collapsed;
            PnlReplace.Visibility = Visibility.Collapsed;
            PnlCustom.Visibility = Visibility.Collapsed;

            switch (CmbRenameMode.SelectedIndex)
            {
                case 0: PnlSequential.Visibility = Visibility.Visible; break;
                case 1: PnlDateTime.Visibility = Visibility.Visible; break;
                case 2: PnlReplace.Visibility = Visibility.Visible; break;
                case 3: PnlCustom.Visibility = Visibility.Visible; break;
            }
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_files.Count == 0)
            {
                MessageBox.Show("Lütfen önce dosyaları yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApplyRenameRules(preview: true);
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (_files.Count == 0)
            {
                MessageBox.Show("Lütfen önce dosyaları yükleyin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedFiles = _files.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Lütfen en az bir dosya seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"{selectedFiles.Count} dosya yeniden adlandırılacak. Devam etmek istiyor musunuz?",
                "Onay",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyRenameRules(preview: false);
            }
        }

        private void ApplyRenameRules(bool preview)
        {
            var options = new RenameOptions
            {
                Mode = (RenameMode)CmbRenameMode.SelectedIndex,
                BaseName = TxtBaseName.Text,
                StartNumber = (int)(NumStartNumber.Value ?? 1),
                Digits = (int)(NumDigits.Value ?? 3),
                DateFormat = CmbDateFormat.SelectedIndex switch
                {
                    0 => "yyyy-MM-dd",
                    1 => "yyyyMMdd",
                    2 => "dd-MM-yyyy",
                    3 => "yyyy-MM-dd_HH-mm-ss",
                    _ => "yyyy-MM-dd"
                },
                DatePrefix = ChkDatePrefix.IsChecked == true,
                DateNow = DateTime.Now,
                SearchText = TxtSearchText.Text,
                ReplaceText = TxtReplaceText.Text,
                UseRegex = ChkUseRegex.IsChecked == true,
                CaseSensitive = ChkCaseSensitive.IsChecked == true,
                Template = TxtCustomTemplate.Text
            };

            var selectedFiles = _files.Where(f => f.IsSelected).ToList();
            var (successCount, errorCount) = _service.ApplyRenameRules(selectedFiles, options, preview);

            if (preview)
            {
                MessageBox.Show("Önizleme oluşturuldu! 'Yeni Dosya Adı' sütununu kontrol edin.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"İşlem tamamlandı!\n✅ Başarılı: {successCount}\n❌ Hatalı: {errorCount}", "Sonuç", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnLoadFiles_Click(null!, null!);
            }
        }
    }

    // ============================================
    // YARDIMCI SINIFLAR
    // ============================================

    public class FileRenameItem
    {
        public bool IsSelected { get; set; }
        public string OriginalName { get; set; } = "";
        public string NewName { get; set; } = "";
        public string Extension { get; set; } = "";
        public string FullPath { get; set; } = "";
    }

    // Metin Giriş Dialog'u
    public class TextInputDialog : Window
    {
        public string InputText { get; private set; } = "";
        private TextBox _textBox;

        public TextInputDialog(string title)
        {
            Title = title;
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(_textBox, 0);
            grid.Children.Add(_textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 1);

            var okButton = new Button
            {
                Content = "Tamam",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            okButton.Click += (s, e) => { InputText = _textBox.Text; DialogResult = true; };

            var cancelButton = new Button
            {
                Content = "İptal",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5)
            };
            cancelButton.Click += (s, e) => { DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
