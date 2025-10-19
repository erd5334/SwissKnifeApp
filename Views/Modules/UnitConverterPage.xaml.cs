using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class UnitConverterPage : UserControl
    {
        private const string SettingsFile = "UnitConverterSettings.json";
        private readonly SwissKnifeApp.Services.UnitConverterService _converter = new SwissKnifeApp.Services.UnitConverterService();

        public UnitConverterPage()
        {
            InitializeComponent();
            // LoadUnits çağrısını buradan kaldır
            Loaded += UnitConverterPage_Loaded;
        }

        private void UnitConverterPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Sayfa tamamen yüklendikten sonra default değerleri ayarla
            LoadUnits("Uzunluk");
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (settings != null)
                    {
                        txtTimestamp.Text = settings.Timestamp ?? "";
                        txtDate1.Text = settings.Date1 ?? "";
                        txtDate2.Text = settings.Date2 ?? "";
                        txtBirthDate.Text = settings.BirthDate ?? "";
                        txtAgeStartDate.Text = settings.AgeStartDate ?? "";
                        txtAgeEndDate.Text = settings.AgeEndDate ?? "";
                        txtTimeZoneSource.Text = settings.TimeZoneSource ?? "";
                        txtTimeZoneTarget.Text = settings.TimeZoneTarget ?? "";
                        txtTimeZoneDate.Text = settings.TimeZoneDate ?? "";
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new UserSettings
                {
                    Timestamp = txtTimestamp?.Text,
                    Date1 = txtDate1?.Text,
                    Date2 = txtDate2?.Text,
                    BirthDate = txtBirthDate?.Text,
                    AgeStartDate = txtAgeStartDate?.Text,
                    AgeEndDate = txtAgeEndDate?.Text,
                    TimeZoneSource = txtTimeZoneSource?.Text,
                    TimeZoneTarget = txtTimeZoneTarget?.Text,
                    TimeZoneDate = txtTimeZoneDate?.Text
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        private class UserSettings
        {
            public string? Timestamp { get; set; }
            public string? Date1 { get; set; }
            public string? Date2 { get; set; }
            public string? BirthDate { get; set; }
            public string? AgeStartDate { get; set; }
            public string? AgeEndDate { get; set; }
            public string? TimeZoneSource { get; set; }
            public string? TimeZoneTarget { get; set; }
            public string? TimeZoneDate { get; set; }
        }

        private void LoadUnits(string type)
        {
            // Null kontrolü ekle
            if (cmbFrom == null || cmbTo == null) 
                return;

            cmbFrom.Items.Clear();
            cmbTo.Items.Clear();

            foreach (var unit in _converter.GetUnits(type))
            {
                cmbFrom.Items.Add(unit);
                cmbTo.Items.Add(unit);
            }

            cmbFrom.SelectedIndex = 0;
            cmbTo.SelectedIndex = 1;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtValue.Text, out double value))
            {
                MessageBox.Show("Lütfen geçerli bir sayı girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string type = ((ComboBoxItem)cmbUnitType.SelectedItem).Content.ToString();
            string from = cmbFrom.SelectedItem.ToString();
            string to = cmbTo.SelectedItem.ToString();
            double result = _converter.Convert(type, from, to, value);

            txtResult.Text = $"{result:0.###}";
        }

        

        private void cmbUnitType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUnitType.SelectedItem is ComboBoxItem item)
                LoadUnits(item.Content.ToString());
        }
        // Timestamp → Tarih
        private void TimestampToDate_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(txtTimestamp.Text, out long ts))
            {
                var date = _converter.TimestampToDate(ts);
                txtTimeResult.Text = $"Tarih: {date:dd.MM.yyyy HH:mm:ss}";
            }
            else
            {
                txtTimeResult.Text = "Geçersiz timestamp!";
            }
            SaveSettings();
        }

        // Tarih → Timestamp
        private void DateToTimestamp_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtTimestamp.Text, out DateTime dt))
            {
                var ts = _converter.DateToTimestamp(dt);
                txtTimeResult.Text = $"Timestamp: {ts}";
            }
            else
            {
                txtTimeResult.Text = "Geçersiz tarih!";
            }
            SaveSettings();
        }

        // Zaman Aralığı Hesapla
        private void CalculateTimeSpan_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtDate1.Text, out DateTime d1) && DateTime.TryParse(txtDate2.Text, out DateTime d2))
            {
                var span = _converter.AbsoluteDifference(d1, d2);
                txtTimeResult.Text = $"Fark: {span.Days} gün, {span.Hours} saat, {span.Minutes} dakika, {span.Seconds} saniye";
            }
            else
            {
                txtTimeResult.Text = "Geçersiz tarih formatı!";
            }
            SaveSettings();
        }

        // Yaş Hesapla
        private void CalculateAge_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtBirthDate.Text, out DateTime birth))
            {
                var now = DateTime.Now;
                var age = _converter.CalculateAge(birth, now);
                txtTimeResult.Text = $"Yaş: {age.Years} yıl, {age.Months} ay, {age.Days} gün\n" +
                                    $"Toplam: {age.TotalMonths} ay\n" +
                                    $"Toplam: {age.TotalDays} gün";
            }
            else
            {
                txtTimeResult.Text = "Geçersiz doğum tarihi!";
            }
            SaveSettings();
        }

        // İki Tarih Arası Yaş Hesapla
        private void CalculateAgeBetweenDates_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtAgeStartDate.Text, out DateTime startDate) && 
                DateTime.TryParse(txtAgeEndDate.Text, out DateTime endDate))
            {
                if (endDate < startDate)
                {
                    txtTimeResult.Text = "Bitiş tarihi başlangıç tarihinden önce olamaz!";
                    return;
                }

                var age = _converter.CalculateAgeBetween(startDate, endDate);
                txtTimeResult.Text = $"İki tarih arası: {age.Years} yıl, {age.Months} ay, {age.Days} gün\n" +
                                    $"Toplam: {age.TotalMonths} ay\n" +
                                    $"Toplam: {age.TotalDays} gün";
            }
            else
            {
                txtTimeResult.Text = "Geçersiz tarih formatı!";
            }
            SaveSettings();
        }

        // Zaman Dilimi Çevir
        private void ConvertTimeZone_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtTimeZoneDate.Text, out DateTime date) &&
                !string.IsNullOrWhiteSpace(txtTimeZoneSource.Text) &&
                !string.IsNullOrWhiteSpace(txtTimeZoneTarget.Text))
            {
                try
                {
                    var tgtTime = _converter.ConvertTimeZone(date, txtTimeZoneSource.Text, txtTimeZoneTarget.Text);
                    txtTimeResult.Text = $"{txtTimeZoneSource.Text} → {txtTimeZoneTarget.Text}: {tgtTime:dd.MM.yyyy HH:mm:ss}";
                }
                catch
                {
                    txtTimeResult.Text = "Zaman dilimi adı geçersiz! (örn. 'Turkey Standard Time', 'UTC')";
                }
            }
            else
            {
                txtTimeResult.Text = "Tarih ve zaman dilimi giriniz!";
            }
            SaveSettings();
        }
    }
}
