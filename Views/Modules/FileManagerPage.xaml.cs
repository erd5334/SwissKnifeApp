using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Security.Cryptography;
using SwissKnifeApp.Services;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Views.Modules
{
    public partial class FileManagerPage : Page
    {
    private readonly FileManagerService _service = new();
    private readonly CopyService _copyService = new();
    private System.Threading.CancellationTokenSource? _copyCts;
        private string? _file1Content;
        private string? _file2Content;
        private ObservableCollection<FileRenameItem> _files = new();
        private Point _dragStartPoint;
        private bool _isDragging = false;

        public FileManagerPage()
        {
            InitializeComponent();
            DgFiles.ItemsSource = _files;
            TxtCustomTemplate.Text = "{name}_{date}";
            // ...existing code...
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
                var ordered = files
                    .OrderBy(f => NaturalSortKey(Path.GetFileName(f))); // Doğal (insan) sıralama
                foreach (var file in ordered)
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

        private static string NaturalSortKey(string input)
        {
            return Regex.Replace(input, "\\d+", m => m.Value.PadLeft(10, '0'));
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _files) item.IsSelected = true;
            DgFiles.Items.Refresh();
        }

        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _files) item.IsSelected = false;
            DgFiles.Items.Refresh();
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (DgFiles.SelectedItem is FileRenameItem item)
            {
                var index = _files.IndexOf(item);
                if (index > 0)
                {
                    _files.Move(index, index - 1);
                }
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (DgFiles.SelectedItem is FileRenameItem item)
            {
                var index = _files.IndexOf(item);
                if (index < _files.Count - 1 && index >= 0)
                {
                    _files.Move(index, index + 1);
                }
            }
        }

        // Drag & Drop reorder handlers
        private void DgFiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private void DgFiles_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                var position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var dataGrid = (DataGrid)sender;
                    var row = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
                    if (row != null)
                    {
                        _isDragging = true;
                        DragDrop.DoDragDrop(row, row.Item, DragDropEffects.Move);
                    }
                }
            }
        }

        private void DgFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DgFiles_Drop(object sender, DragEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            var sourceItem = e.Data.GetData(typeof(FileRenameItem)) as FileRenameItem;
            if (sourceItem == null) return;

            var targetRow = FindAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
            if (targetRow == null) return;
            var targetItem = targetRow.Item as FileRenameItem;
            if (targetItem == null || ReferenceEquals(sourceItem, targetItem)) return;

            var sourceIndex = _files.IndexOf(sourceItem);
            var targetIndex = _files.IndexOf(targetItem);
            if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
            {
                _files.Move(sourceIndex, targetIndex);
                DgFiles.SelectedItem = sourceItem;
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
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

        // ============================================
        // 4️⃣ DOSYA AYIRICI / BİRLEŞTİRİCİ
        // ============================================

        private void BtnSelectSplitFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true) TxtSplitFilePath.Text = dlg.FileName;
        }

        private void BtnSplitFile_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtSplitFilePath.Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                long partSize = 100 * 1024 * 1024; // Varsayılan 100MB
                byte[] buffer = new byte[partSize];
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                int index = 1;
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string partPath = $"{path}.part{index:D3}";
                    using var partFs = new FileStream(partPath, FileMode.Create, FileAccess.Write);
                    partFs.Write(buffer, 0, read);
                    index++;
                }
                TxtSplitStatus.Text = $"✅ Dosya {index - 1} parçaya ayrıldı.";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnJoinFiles_Click(object sender, RoutedEventArgs e)
        {
            string firstPart = TxtSplitFilePath.Text;
            if (string.IsNullOrEmpty(firstPart) || !firstPart.Contains(".part001")) 
            {
                MessageBox.Show("Lütfen '.part001' uzantılı ilk parçayı seçin.");
                return;
            }

            try
            {
                string originalPath = firstPart.Replace(".part001", "");
                using var outFs = new FileStream(originalPath, FileMode.Create, FileAccess.Write);
                int index = 1;
                while (true)
                {
                    string partPath = $"{originalPath}.part{index:D3}";
                    if (!File.Exists(partPath)) break;
                    using var partFs = new FileStream(partPath, FileMode.Open, FileAccess.Read);
                    partFs.CopyTo(outFs);
                    index++;
                }
                TxtSplitStatus.Text = $"✅ {index - 1} parça birleştirildi: {Path.GetFileName(originalPath)}";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ============================================
        // 5️⃣ HASH DOĞRULAYICI
        // ============================================

        private void BtnSelectHashFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true) TxtHashFilePath.Text = dlg.FileName;
        }

        private void BtnCalculateHash_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtHashFilePath.Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                string algo = (CmbHashAlgo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "SHA256";
                using var stream = File.OpenRead(path);
                
                byte[] hashBytes;
                if (algo == "MD5") hashBytes = MD5.Create().ComputeHash(stream);
                else if (algo == "SHA512") hashBytes = SHA512.Create().ComputeHash(stream);
                else hashBytes = SHA256.Create().ComputeHash(stream);

                TxtCalculatedHash.Text = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                VerifyHash();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void TxtVerifyHash_TextChanged(object sender, TextChangedEventArgs e) => VerifyHash();

        private void VerifyHash()
        {
            if (string.IsNullOrEmpty(TxtCalculatedHash.Text) || string.IsNullOrEmpty(TxtVerifyHash.Text))
            {
                TxtHashResult.Text = "Bekleniyor...";
                TxtHashResult.Foreground = Brushes.Gray;
                return;
            }

            if (TxtCalculatedHash.Text.Equals(TxtVerifyHash.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                TxtHashResult.Text = "✅ HASH DOĞRULANDI - Dosya Güvenli";
                TxtHashResult.Foreground = Brushes.Green;
            }
            else
            {
                TxtHashResult.Text = "❌ HASH UYUŞMUYOR - Dosya Değişmiş Olabilir!";
                TxtHashResult.Foreground = Brushes.Red;
            }
        }

        // ============================================
        // 6️⃣ TEMİZLİK ARAÇLARI
        // ============================================

        private void BtnSelectCleanupFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) TxtCleanupPath.Text = dialog.SelectedPath;
        }

        private void BtnFindEmptyFolders_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtCleanupPath.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            LstCleanupResults.Items.Clear();
            var emptyFolders = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                .Where(d => !Directory.EnumerateFileSystemEntries(d).Any()).ToList();

            foreach (var r in emptyFolders) LstCleanupResults.Items.Add(r);
            BtnDeleteSelectedCleanup.Visibility = emptyFolders.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnFindLargeFiles_Click(object sender, RoutedEventArgs e)
        {
            string path = TxtCleanupPath.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            LstCleanupResults.Items.Clear();
            BtnDeleteSelectedCleanup.Visibility = Visibility.Collapsed;
            
            var largeFiles = new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories)
                .Where(f => f.Length > 100 * 1024 * 1024)
                .OrderByDescending(f => f.Length)
                .Take(50);

            foreach (var f in largeFiles) 
                LstCleanupResults.Items.Add($"{f.Length / 1024 / 1024} MB - {f.FullName}");
        }

        private void BtnDeleteSelectedCleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Seçili tüm boş klasörleri silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                foreach (string item in LstCleanupResults.Items)
                {
                    try { if (Directory.Exists(item)) Directory.Delete(item); } catch { }
                }
                BtnFindEmptyFolders_Click(null!, null!);
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
