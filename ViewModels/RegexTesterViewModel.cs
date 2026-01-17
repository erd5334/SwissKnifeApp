using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;

namespace SwissKnifeApp.ViewModels
{
    public partial class RegexTesterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _regexPattern = "";

        [ObservableProperty]
        private string _testText = "";

        [ObservableProperty]
        private string _replacementText = "";

        [ObservableProperty]
        private string _replaceResult = "";

        [ObservableProperty]
        private bool _isMatchCase = false;

        [ObservableProperty]
        private bool _isMultiline = false;

        [ObservableProperty]
        private bool _isSingleline = false;

        [ObservableProperty]
        private int _matchCount = 0;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private long _executionTime = 0;

        [ObservableProperty]
        private ObservableCollection<MatchInfo> _matches = new();

        [ObservableProperty]
        private ObservableCollection<CommonPattern> _commonPatterns = new();

        [ObservableProperty]
        private string _selectedCheatSheetItem = "";

        public ObservableCollection<string> CheatSheet { get; } = new()
        {
            "\\d - Rakam (0-9)",
            "\\D - Rakam olmayan",
            "\\w - Kelime karakteri (a-z, A-Z, 0-9, _)",
            "\\W - Kelime karakteri olmayan",
            "\\s - Boşluk karakteri",
            "\\S - Boşluk olmayan",
            ". - Herhangi bir karakter",
            "^ - Satır başı",
            "$ - Satır sonu",
            "* - 0 veya daha fazla",
            "+ - 1 veya daha fazla",
            "? - 0 veya 1",
            "{n} - Tam n kez",
            "{n,} - En az n kez",
            "{n,m} - n ile m arasında",
            "[abc] - a, b veya c",
            "[^abc] - a, b, c hariç",
            "[a-z] - a'dan z'ye",
            "(abc) - Grup (capture)",
            "(?:abc) - Grup (non-capture)",
            "a|b - a veya b",
            "\\b - Kelime sınırı",
            "\\A - String başı",
            "\\Z - String sonu"
        };

        public RegexTesterViewModel()
        {
            LoadCommonPatterns();
        }

        private void LoadCommonPatterns()
        {
            CommonPatterns.Add(new CommonPattern { Name = "Email", Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}" });
            CommonPatterns.Add(new CommonPattern { Name = "URL", Pattern = @"https?://[^\s]+" });
            CommonPatterns.Add(new CommonPattern { Name = "Telefon (TR)", Pattern = @"(0)?(\d{3})(\d{3})(\d{2})(\d{2})" });
            CommonPatterns.Add(new CommonPattern { Name = "TC Kimlik No", Pattern = @"([1-9]\d)(\d{7})(\d{2})" });
            CommonPatterns.Add(new CommonPattern { Name = "IP Adresi", Pattern = @"((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}" });
            CommonPatterns.Add(new CommonPattern { Name = "Tarih (DD/MM/YYYY)", Pattern = @"(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[012])/\d{4}" });
            CommonPatterns.Add(new CommonPattern { Name = "Kredi Kartı", Pattern = @"\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}" });
            CommonPatterns.Add(new CommonPattern { Name = "HTML Tag", Pattern = @"<[^>]+>" });
            CommonPatterns.Add(new CommonPattern { Name = "Hex Renk", Pattern = @"#?([a-fA-F0-9]{6}|[a-fA-F0-9]{3})" });
            CommonPatterns.Add(new CommonPattern { Name = "Username", Pattern = @"[a-zA-Z0-9_]{3,16}" });
            CommonPatterns.Add(new CommonPattern { Name = "Şifre (Güçlü)", Pattern = @"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}" });
        }

        [RelayCommand]
        private void TestRegex()
        {
            if (string.IsNullOrEmpty(RegexPattern))
            {
                ErrorMessage = "⚠️ Lütfen bir regex pattern girin";
                return;
            }

            try
            {
                ErrorMessage = "";
                Matches.Clear();

                var options = RegexOptions.None;
                if (!IsMatchCase) options |= RegexOptions.IgnoreCase;
                if (IsMultiline) options |= RegexOptions.Multiline;
                if (IsSingleline) options |= RegexOptions.Singleline;

                var stopwatch = Stopwatch.StartNew();
                var regex = new Regex(RegexPattern, options);
                var matchCollection = regex.Matches(TestText);
                stopwatch.Stop();

                ExecutionTime = stopwatch.ElapsedMilliseconds;
                MatchCount = matchCollection.Count;

                foreach (Match match in matchCollection)
                {
                    var matchInfo = new MatchInfo
                    {
                        Value = match.Value,
                        Index = match.Index,
                        Length = match.Length,
                        Groups = new ObservableCollection<GroupInfo>()
                    };

                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        var group = match.Groups[i];
                        matchInfo.Groups.Add(new GroupInfo
                        {
                            Index = i,
                            Name = regex.GroupNameFromNumber(i),
                            Value = group.Value,
                            Position = group.Index
                        });
                    }

                    Matches.Add(matchInfo);
                }

                if (MatchCount == 0)
                {
                    ErrorMessage = "ℹ️ Eşleşme bulunamadı";
                }
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = $"❌ Regex hatası: {ex.Message}";
                MatchCount = 0;
                ExecutionTime = 0;
            }
        }

        [RelayCommand]
        private void TestReplace()
        {
            if (string.IsNullOrEmpty(RegexPattern) || string.IsNullOrEmpty(TestText))
            {
                ErrorMessage = "⚠️ Pattern ve test metni gerekli";
                return;
            }

            try
            {
                ErrorMessage = "";

                var options = RegexOptions.None;
                if (!IsMatchCase) options |= RegexOptions.IgnoreCase;
                if (IsMultiline) options |= RegexOptions.Multiline;
                if (IsSingleline) options |= RegexOptions.Singleline;

                var regex = new Regex(RegexPattern, options);
                ReplaceResult = regex.Replace(TestText, ReplacementText);
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = $"❌ Replace hatası: {ex.Message}";
                ReplaceResult = "";
            }
        }

        [RelayCommand]
        private void LoadPattern(CommonPattern pattern)
        {
            if (pattern != null)
            {
                RegexPattern = pattern.Pattern;
            }
        }

        [RelayCommand]
        private void ClearAll()
        {
            RegexPattern = "";
            TestText = "";
            ReplacementText = "";
            ReplaceResult = "";
            Matches.Clear();
            MatchCount = 0;
            ExecutionTime = 0;
            ErrorMessage = "";
        }

        [RelayCommand]
        private void CopyReplaceResult()
        {
            if (!string.IsNullOrEmpty(ReplaceResult))
            {
                try
                {
                    Clipboard.SetText(ReplaceResult);
                    MessageBox.Show("✅ Replace sonucu panoya kopyalandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Kopyalama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("⚠️ Kopyalanacak sonuç yok!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CopyTestText()
        {
            if (!string.IsNullOrEmpty(TestText))
            {
                try
                {
                    Clipboard.SetText(TestText);
                    MessageBox.Show("✅ Test metni panoya kopyalandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Kopyalama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class MatchInfo
    {
        public string Value { get; set; } = string.Empty;
        public int Index { get; set; }
        public int Length { get; set; }
        public ObservableCollection<GroupInfo> Groups { get; set; } = new();
    }

    public class GroupInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class CommonPattern
    {
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
    }
}
