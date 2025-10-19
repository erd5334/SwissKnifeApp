using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SwissKnifeApp.Models;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class TaxSettingsWindow : Window
    {
        private TaxRatesData _taxRates;
        private readonly string _jsonFilePath;
        private ObservableCollection<VatWithholdingRate> _kdvTevkifatList;
        private ObservableCollection<CarTaxBracket> _mtvCarList;
        private ObservableCollection<FuelTaxRateViewModel> _fuelList;

        public TaxSettingsWindow()
        {
            InitializeComponent();
            _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tax-rates.json");
            _kdvTevkifatList = new ObservableCollection<VatWithholdingRate>();
            _mtvCarList = new ObservableCollection<CarTaxBracket>();
            _fuelList = new ObservableCollection<FuelTaxRateViewModel>();
            
            LoadCurrentRates();
        }

        private void LoadCurrentRates()
        {
            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    MessageBox.Show($"Vergi oranları dosyası bulunamadı: {_jsonFilePath}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _taxRates = new TaxRatesData();
                    InitializeDefaultRates();
                    return;
                }

                var json = File.ReadAllText(_jsonFilePath);
                _taxRates = JsonSerializer.Deserialize<TaxRatesData>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new TaxRatesData();

                // If sections are empty, initialize with defaults
                if (_taxRates.KdvTevkifat?.Oranlar == null || _taxRates.KdvTevkifat.Oranlar.Count == 0)
                {
                    InitializeDefaultKdvTevkifat();
                }

                if (_taxRates.MotorluTasitlarVergisi == null || !_taxRates.MotorluTasitlarVergisi.ContainsKey("2025"))
                {
                    InitializeDefaultMtv();
                }

                if (_taxRates.OzelTuketimVergisi?.Akaryakit == null || _taxRates.OzelTuketimVergisi.Akaryakit.Count == 0)
                {
                    InitializeDefaultFuel();
                }

                if (_taxRates.EmlakVergisi == null || !_taxRates.EmlakVergisi.ContainsKey("2025"))
                {
                    InitializeDefaultProperty();
                }

                if (_taxRates.VergiGecikme == null || !_taxRates.VergiGecikme.ContainsKey("2025"))
                {
                    InitializeDefaultDelayInterest();
                }

                LoadDataToUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Vergi oranları yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                _taxRates = new TaxRatesData();
                InitializeDefaultRates();
                LoadDataToUI();
            }
        }

        private void InitializeDefaultRates()
        {
            InitializeDefaultKdvTevkifat();
            InitializeDefaultMtv();
            InitializeDefaultFuel();
            InitializeDefaultProperty();
            InitializeDefaultDelayInterest();
        }

        private void InitializeDefaultKdvTevkifat()
        {
            _taxRates.KdvTevkifat = new VatWithholdingData
            {
                Oranlar = new List<VatWithholdingRate>
                {
                    new VatWithholdingRate { Tanim = "Makine ve Teçhizat Alımı", KdvOran = 20, TevkifatOran = 50 },
                    new VatWithholdingRate { Tanim = "Bakım ve Onarım Hizmetleri", KdvOran = 20, TevkifatOran = 50 },
                    new VatWithholdingRate { Tanim = "Yapı İşleri ve Onarım", KdvOran = 20, TevkifatOran = 50 },
                    new VatWithholdingRate { Tanim = "Danışmanlık ve Mühendislik", KdvOran = 20, TevkifatOran = 90 },
                    new VatWithholdingRate { Tanim = "Temizlik ve Güvenlik Hizmetleri", KdvOran = 20, TevkifatOran = 90 },
                    new VatWithholdingRate { Tanim = "İşgücü Temini", KdvOran = 20, TevkifatOran = 90 },
                    new VatWithholdingRate { Tanim = "Hurda ve Atık Teslimi", KdvOran = 20, TevkifatOran = 90 },
                    new VatWithholdingRate { Tanim = "Metal İşleme", KdvOran = 20, TevkifatOran = 90 },
                    new VatWithholdingRate { Tanim = "Spor Kulübü Hizmetleri", KdvOran = 20, TevkifatOran = 50 }
                }
            };
        }

        private void InitializeDefaultMtv()
        {
            if (!_taxRates.MotorluTasitlarVergisi.ContainsKey("2025"))
            {
                _taxRates.MotorluTasitlarVergisi["2025"] = new MotorVehicleTaxData
                {
                    Otomobil = new List<CarTaxBracket>
                    {
                        new CarTaxBracket { Alt = 0, Ust = 1300, Yil1 = 3574, Yil2 = 2516, Yil3 = 1879, Yil4 = 1413, Yil5Plus = 1059 },
                        new CarTaxBracket { Alt = 1301, Ust = 1600, Yil1 = 6440, Yil2 = 4533, Yil3 = 3383, Yil4 = 2542, Yil5Plus = 1907 },
                        new CarTaxBracket { Alt = 1601, Ust = 1800, Yil1 = 9304, Yil2 = 6546, Yil3 = 4887, Yil4 = 3670, Yil5Plus = 2754 },
                        new CarTaxBracket { Alt = 1801, Ust = 2000, Yil1 = 11454, Yil2 = 8058, Yil3 = 6014, Yil4 = 4517, Yil5Plus = 3389 },
                        new CarTaxBracket { Alt = 2001, Ust = 2500, Yil1 = 17181, Yil2 = 12088, Yil3 = 9020, Yil4 = 6774, Yil5Plus = 5082 },
                        new CarTaxBracket { Alt = 2501, Ust = 3000, Yil1 = 28635, Yil2 = 20148, Yil3 = 15034, Yil4 = 11294, Yil5Plus = 8472 },
                        new CarTaxBracket { Alt = 3001, Ust = 3500, Yil1 = 44235, Yil2 = 31124, Yil3 = 23226, Yil4 = 17445, Yil5Plus = 13089 },
                        new CarTaxBracket { Alt = 3501, Ust = 4000, Yil1 = 68863, Yil2 = 48444, Yil3 = 36152, Yil4 = 27149, Yil5Plus = 20372 },
                        new CarTaxBracket { Alt = 4001, Ust = 999999, Yil1 = 91817, Yil2 = 64589, Yil3 = 48202, Yil4 = 36199, Yil5Plus = 27162 }
                    }
                };
            }
        }

        private void InitializeDefaultFuel()
        {
            _taxRates.OzelTuketimVergisi = new SpecialConsumptionTaxData
            {
                Akaryakit = new List<FuelTaxRate>
                {
                    new FuelTaxRate { Tanim = "Benzin", Oran = 5.17m },
                    new FuelTaxRate { Tanim = "Motorin", Oran = 3.44m },
                    new FuelTaxRate { Tanim = "LPG", Oran = 2.07m }
                }
            };
        }

        private void InitializeDefaultProperty()
        {
            if (!_taxRates.EmlakVergisi.ContainsKey("2025"))
            {
                _taxRates.EmlakVergisi["2025"] = new PropertyTaxData
                {
                    BinaOran = 0.2m,
                    AraziOran = 0.1m
                };
            }
        }

        private void InitializeDefaultDelayInterest()
        {
            if (!_taxRates.VergiGecikme.ContainsKey("2025"))
            {
                _taxRates.VergiGecikme["2025"] = new TaxDelayInterestData { AylikOran = 3.5m };
            }
            if (!_taxRates.VergiGecikme.ContainsKey("2024"))
            {
                _taxRates.VergiGecikme["2024"] = new TaxDelayInterestData { AylikOran = 3.5m };
            }
        }

        private void LoadDataToUI()
        {
            // KDV Tevkifat
            _kdvTevkifatList.Clear();
            foreach (var rate in _taxRates.KdvTevkifat?.Oranlar ?? new List<VatWithholdingRate>())
            {
                _kdvTevkifatList.Add(rate);
            }
            DgKdvTevkifat.ItemsSource = _kdvTevkifatList;

            // MTV Otomobil
            _mtvCarList.Clear();
            if (_taxRates.MotorluTasitlarVergisi.ContainsKey("2025"))
            {
                foreach (var bracket in _taxRates.MotorluTasitlarVergisi["2025"].Otomobil)
                {
                    _mtvCarList.Add(bracket);
                }
            }
            DgMtvOtomobil.ItemsSource = _mtvCarList;

            // Fuel
            _fuelList.Clear();
            foreach (var fuel in _taxRates.OzelTuketimVergisi?.Akaryakit ?? new List<FuelTaxRate>())
            {
                _fuelList.Add(new FuelTaxRateViewModel { Tanim = fuel.Tanim, Oran = fuel.Oran });
            }
            DgAkaryakit.ItemsSource = _fuelList;

            // Property Tax
            if (_taxRates.EmlakVergisi.ContainsKey("2025"))
            {
                NumBinaOran.Value = (double)_taxRates.EmlakVergisi["2025"].BinaOran;
                NumAraziOran.Value = (double)_taxRates.EmlakVergisi["2025"].AraziOran;
            }

            // Delay Interest
            if (_taxRates.VergiGecikme.ContainsKey("2025"))
            {
                NumDelay2025.Value = (double)_taxRates.VergiGecikme["2025"].AylikOran;
            }
            if (_taxRates.VergiGecikme.ContainsKey("2024"))
            {
                NumDelay2024.Value = (double)_taxRates.VergiGecikme["2024"].AylikOran;
            }
        }

        private void BtnAddKdvTevkifat_Click(object sender, RoutedEventArgs e)
        {
            var newRate = new VatWithholdingRate
            {
                Tanim = "Yeni Kategori",
                KdvOran = 20,
                TevkifatOran = 50
            };
            _kdvTevkifatList.Add(newRate);
            DgKdvTevkifat.SelectedItem = newRate;
            DgKdvTevkifat.ScrollIntoView(newRate);
        }

        private void BtnEditKdvTevkifat_Click(object sender, RoutedEventArgs e)
        {
            if (DgKdvTevkifat.SelectedItem is VatWithholdingRate selected)
            {
                // DataGrid'de düzenleme modunu etkinleştir
                var row = DgKdvTevkifat.ItemContainerGenerator.ContainerFromItem(selected) as System.Windows.Controls.DataGridRow;
                if (row != null)
                {
                    DgKdvTevkifat.CurrentCell = new System.Windows.Controls.DataGridCellInfo(selected, DgKdvTevkifat.Columns[0]);
                    DgKdvTevkifat.BeginEdit();
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteKdvTevkifat_Click(object sender, RoutedEventArgs e)
        {
            if (DgKdvTevkifat.SelectedItem is VatWithholdingRate selected)
            {
                _kdvTevkifatList.Remove(selected);
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnAddMtvCar_Click(object sender, RoutedEventArgs e)
        {
            var newBracket = new CarTaxBracket
            {
                Alt = 0,
                Ust = 1000,
                Yil1 = 1000,
                Yil2 = 800,
                Yil3 = 600,
                Yil4 = 400,
                Yil5Plus = 200
            };
            _mtvCarList.Add(newBracket);
            DgMtvOtomobil.SelectedItem = newBracket;
            DgMtvOtomobil.ScrollIntoView(newBracket);
        }

        private void BtnEditMtvCar_Click(object sender, RoutedEventArgs e)
        {
            if (DgMtvOtomobil.SelectedItem is CarTaxBracket selected)
            {
                // DataGrid'de düzenleme modunu etkinleştir
                var row = DgMtvOtomobil.ItemContainerGenerator.ContainerFromItem(selected) as System.Windows.Controls.DataGridRow;
                if (row != null)
                {
                    DgMtvOtomobil.CurrentCell = new System.Windows.Controls.DataGridCellInfo(selected, DgMtvOtomobil.Columns[0]);
                    DgMtvOtomobil.BeginEdit();
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteMtvCar_Click(object sender, RoutedEventArgs e)
        {
            if (DgMtvOtomobil.SelectedItem is CarTaxBracket selected)
            {
                _mtvCarList.Remove(selected);
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnAddFuel_Click(object sender, RoutedEventArgs e)
        {
            var newFuel = new FuelTaxRateViewModel
            {
                Tanim = "Yeni Yakıt",
                Oran = 1.0m
            };
            _fuelList.Add(newFuel);
            DgAkaryakit.SelectedItem = newFuel;
            DgAkaryakit.ScrollIntoView(newFuel);
        }

        private void BtnEditFuel_Click(object sender, RoutedEventArgs e)
        {
            if (DgAkaryakit.SelectedItem is FuelTaxRateViewModel selected)
            {
                // DataGrid'de düzenleme modunu etkinleştir
                var row = DgAkaryakit.ItemContainerGenerator.ContainerFromItem(selected) as System.Windows.Controls.DataGridRow;
                if (row != null)
                {
                    DgAkaryakit.CurrentCell = new System.Windows.Controls.DataGridCellInfo(selected, DgAkaryakit.Columns[0]);
                    DgAkaryakit.BeginEdit();
                }
            }
            else
            {
                MessageBox.Show("Lütfen düzenlemek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDeleteFuel_Click(object sender, RoutedEventArgs e)
        {
            if (DgAkaryakit.SelectedItem is FuelTaxRateViewModel selected)
            {
                _fuelList.Remove(selected);
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update from UI to model
                _taxRates.KdvTevkifat.Oranlar = _kdvTevkifatList.ToList();
                
                if (!_taxRates.MotorluTasitlarVergisi.ContainsKey("2025"))
                {
                    _taxRates.MotorluTasitlarVergisi["2025"] = new MotorVehicleTaxData();
                }
                _taxRates.MotorluTasitlarVergisi["2025"].Otomobil = _mtvCarList.ToList();

                _taxRates.OzelTuketimVergisi.Akaryakit = _fuelList.Select(f => new FuelTaxRate { Tanim = f.Tanim, Oran = f.Oran }).ToList();

                if (!_taxRates.EmlakVergisi.ContainsKey("2025"))
                {
                    _taxRates.EmlakVergisi["2025"] = new PropertyTaxData();
                }
                _taxRates.EmlakVergisi["2025"].BinaOran = (decimal)NumBinaOran.Value;
                _taxRates.EmlakVergisi["2025"].AraziOran = (decimal)NumAraziOran.Value;

                if (!_taxRates.VergiGecikme.ContainsKey("2025"))
                {
                    _taxRates.VergiGecikme["2025"] = new TaxDelayInterestData();
                }
                _taxRates.VergiGecikme["2025"].AylikOran = (decimal)NumDelay2025.Value;

                if (!_taxRates.VergiGecikme.ContainsKey("2024"))
                {
                    _taxRates.VergiGecikme["2024"] = new TaxDelayInterestData();
                }
                _taxRates.VergiGecikme["2024"].AylikOran = (decimal)NumDelay2024.Value;

                // Update metadata
                _taxRates.LastUpdated = DateTime.Now;
                _taxRates.Source = "Manuel Düzenleme";
                _taxRates.Version = "1.0";

                // Save to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(_taxRates, options);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(_jsonFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_jsonFilePath, json);

                // Reload rates in TaxCalculationService
                TaxCalculationService.Instance.LoadTaxRates();

                MessageBox.Show("Vergi oranları başarıyla kaydedildi ve uygulandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // ViewModel for fuel to enable editing
    public class FuelTaxRateViewModel
    {
        public string Tanim { get; set; } = "";
        public decimal Oran { get; set; }
    }
}
