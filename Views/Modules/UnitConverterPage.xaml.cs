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

            switch (type)
            {
                case "Uzunluk":
                    cmbFrom.Items.Add("Metre");
                    cmbFrom.Items.Add("Kilometre");
                    cmbFrom.Items.Add("Santimetre");
                    cmbFrom.Items.Add("Milimetre");

                    cmbTo.Items.Add("Metre");
                    cmbTo.Items.Add("Kilometre");
                    cmbTo.Items.Add("Santimetre");
                    cmbTo.Items.Add("Milimetre");
                    break;

                case "Ağırlık":
                    cmbFrom.Items.Add("Gram");
                    cmbFrom.Items.Add("Kilogram");
                    cmbFrom.Items.Add("Ton");
                    cmbTo.Items.Add("Gram");
                    cmbTo.Items.Add("Kilogram");
                    cmbTo.Items.Add("Ton");
                    break;

                case "Sıcaklık":
                    cmbFrom.Items.Add("Celsius");
                    cmbFrom.Items.Add("Fahrenheit");
                    cmbFrom.Items.Add("Kelvin");
                    cmbTo.Items.Add("Celsius");
                    cmbTo.Items.Add("Fahrenheit");
                    cmbTo.Items.Add("Kelvin");
                    break;
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
            double result = ConvertUnit(type, from, to, value);

            txtResult.Text = $"{result:0.###}";
        }

        private double ConvertUnit(string type, string from, string to, double value)
        {
            if (type == "Uzunluk")
            {
                // Basit örnek — metre bazlı
                double toMeters = from switch
                {
                    "Kilometre" => value * 1000,
                    "Santimetre" => value / 100,
                    "Milimetre" => value / 1000,
                    _ => value
                };

                return to switch
                {
                    "Kilometre" => toMeters / 1000,
                    "Santimetre" => toMeters * 100,
                    "Milimetre" => toMeters * 1000,
                    _ => toMeters
                };
            }

            if (type == "Ağırlık")
            {
                double toGrams = from switch
                {
                    "Kilogram" => value * 1000,
                    "Ton" => value * 1_000_000,
                    _ => value
                };

                return to switch
                {
                    "Kilogram" => toGrams / 1000,
                    "Ton" => toGrams / 1_000_000,
                    _ => toGrams
                };
            }

            if (type == "Sıcaklık")
            {
                double celsius = from switch
                {
                    "Fahrenheit" => (value - 32) * 5 / 9,
                    "Kelvin" => value - 273.15,
                    _ => value
                };

                return to switch
                {
                    "Fahrenheit" => celsius * 9 / 5 + 32,
                    "Kelvin" => celsius + 273.15,
                    _ => celsius
                };
            }

            return value;
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
                var date = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;
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
                var ts = new DateTimeOffset(dt).ToUnixTimeSeconds();
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
                var span = d2 > d1 ? d2 - d1 : d1 - d2;
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
                int years = now.Year - birth.Year;
                int months = now.Month - birth.Month;
                int days = now.Day - birth.Day;
                if (days < 0)
                {
                    months--;
                    days += DateTime.DaysInMonth(now.Year, (now.Month == 1 ? 12 : now.Month - 1));
                }
                if (months < 0)
                {
                    years--;
                    months += 12;
                }

                // Toplam ay ve gün hesaplama
                var totalMonths = years * 12 + months;
                var totalDays = (int)(now - birth).TotalDays;

                txtTimeResult.Text = $"Yaş: {years} yıl, {months} ay, {days} gün\n" +
                                    $"Toplam: {totalMonths} ay\n" +
                                    $"Toplam: {totalDays} gün";
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

                int years = endDate.Year - startDate.Year;
                int months = endDate.Month - startDate.Month;
                int days = endDate.Day - startDate.Day;
                
                if (days < 0)
                {
                    months--;
                    days += DateTime.DaysInMonth(endDate.Year, (endDate.Month == 1 ? 12 : endDate.Month - 1));
                }
                if (months < 0)
                {
                    years--;
                    months += 12;
                }

                // Toplam ay ve gün hesaplama
                var totalMonths = years * 12 + months;
                var totalDays = (int)(endDate - startDate).TotalDays;
                
                txtTimeResult.Text = $"İki tarih arası: {years} yıl, {months} ay, {days} gün\n" +
                                    $"Toplam: {totalMonths} ay\n" +
                                    $"Toplam: {totalDays} gün";
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
                    var src = TimeZoneInfo.FindSystemTimeZoneById(txtTimeZoneSource.Text);
                    var tgt = TimeZoneInfo.FindSystemTimeZoneById(txtTimeZoneTarget.Text);
                    var srcTime = TimeZoneInfo.ConvertTimeToUtc(date, src);
                    var tgtTime = TimeZoneInfo.ConvertTimeFromUtc(srcTime, tgt);
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
