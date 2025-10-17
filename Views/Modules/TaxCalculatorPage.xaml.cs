using SwissKnifeApp.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class TaxCalculatorPage : Page
    {
        private readonly TaxCalculationService _taxService;
        private readonly TaxRatesScraperService _scraperService;

        public TaxCalculatorPage()
        {
            InitializeComponent();
            _taxService = new TaxCalculationService();
            _scraperService = new TaxRatesScraperService();
            
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // Son güncelleme tarihini göster
            var lastUpdate = _taxService.GetLastUpdateDate();
            TxtLastUpdate.Text = lastUpdate == DateTime.MinValue 
                ? "Henüz güncellenmedi" 
                : lastUpdate.ToString("dd.MM.yyyy HH:mm");

            // Yıl combobox'larını doldur
            var incomeYears = _taxService.GetAvailableYears("gelir");
            foreach (var year in incomeYears)
            {
                CmbIncomeYear.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
                CmbRentYear.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
            }
            if (CmbIncomeYear.Items.Count > 0) 
            {
                CmbIncomeYear.SelectedIndex = 0;
                CmbRentYear.SelectedIndex = 0;
            }

            var corporateYears = _taxService.GetAvailableYears("kurumlar");
            foreach (var year in corporateYears)
            {
                CmbCorporateYear.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
            }
            if (CmbCorporateYear.Items.Count > 0) CmbCorporateYear.SelectedIndex = 0;

            // KDV Tevkifat kategorilerini yükle
            var categories = _taxService.GetVatWithholdingCategories();
            foreach (var cat in categories)
            {
                CmbWithholdingCategory.Items.Add(new ComboBoxItem { Content = cat, Tag = cat });
            }
            if (CmbWithholdingCategory.Items.Count > 0) CmbWithholdingCategory.SelectedIndex = 0;
        }

        private async void BtnRefreshRates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                BtnRefreshRates.IsEnabled = false;

                bool success = await _scraperService.UpdateTaxRatesAsync();

                if (success)
                {
                    // Servisi yeniden yükle
                    _taxService.RefreshTaxRatesAsync().Wait();
                    
                    var lastUpdate = _taxService.GetLastUpdateDate();
                    TxtLastUpdate.Text = lastUpdate.ToString("dd.MM.yyyy HH:mm");
                    
                    MessageBox.Show("Vergi oranları başarıyla güncellendi!", "Başarılı", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Vergi oranları güncellenirken bir hata oluştu.", "Hata", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güncelleme hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                BtnRefreshRates.IsEnabled = true;
            }
        }

        // Gelir Vergisi Hesaplama
        private void BtnCalculateIncomeTax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbIncomeYear.SelectedItem == null || NumIncomeMatrah.Value == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = (int)((ComboBoxItem)CmbIncomeYear.SelectedItem).Tag;
                decimal matrah = (decimal)NumIncomeMatrah.Value.Value;
                bool ucretGeliri = ((ComboBoxItem)CmbIncomeType.SelectedItem).Tag.ToString() == "ucret";

                var result = _taxService.CalculateIncomeTax(matrah, year, ucretGeliri);

                TxtIncomeMatrahResult.Text = $"{result.Matrah:N2} TL";
                TxtIncomeTaxResult.Text = $"{result.VergiTutari:N2} TL";
                TxtIncomeNetResult.Text = $"{result.NetTutar:N2} TL";

                IncomeTaxBrackets.ItemsSource = result.Dilimler;
                IncomeTaxResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // KDV Hesaplama
        private void BtnCalculateKdv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumKdvAmount.Value == null || CmbKdvRate.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal amount = (decimal)NumKdvAmount.Value.Value;
                decimal rate = decimal.Parse(((ComboBoxItem)CmbKdvRate.SelectedItem).Tag.ToString() ?? "20");
                bool isDahil = ((ComboBoxItem)CmbKdvOperation.SelectedItem).Tag.ToString() == "dahil";

                if (isDahil)
                {
                    var (kdv, total) = _taxService.CalculateKdvDahil(amount, rate);
                    TxtKdvNetAmount.Text = $"{amount:N2} TL";
                    TxtKdvAmount.Text = $"{kdv:N2} TL";
                    TxtKdvTotal.Text = $"{total:N2} TL";
                }
                else
                {
                    var (net, kdv) = _taxService.CalculateKdvHaric(amount, rate);
                    TxtKdvNetAmount.Text = $"{net:N2} TL";
                    TxtKdvAmount.Text = $"{kdv:N2} TL";
                    TxtKdvTotal.Text = $"{amount:N2} TL";
                }

                KdvResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Kurumlar Vergisi Hesaplama
        private void BtnCalculateCorporateTax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbCorporateYear.SelectedItem == null || NumCorporateMatrah.Value == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = (int)((ComboBoxItem)CmbCorporateYear.SelectedItem).Tag;
                decimal matrah = (decimal)NumCorporateMatrah.Value.Value;
                bool isFinancial = ChkFinancial.IsChecked == true;

                decimal tax = _taxService.CalculateCorporateTax(matrah, year, isFinancial);
                decimal rate = (tax / matrah) * 100;

                TxtCorporateMatrah.Text = $"{matrah:N2} TL";
                TxtCorporateRate.Text = $"%{rate:N0}";
                TxtCorporateTax.Text = $"{tax:N2} TL";

                CorporateTaxResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Kira Vergisi Hesaplama
        private void BtnCalculateRentTax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbRentYear.SelectedItem == null || NumRentIncome.Value == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = (int)((ComboBoxItem)CmbRentYear.SelectedItem).Tag;
                decimal rentIncome = (decimal)NumRentIncome.Value.Value;
                decimal otherIncome = (decimal)(NumOtherIncome.Value ?? 0);
                bool useExemption = ChkRentExemption.IsChecked == true;

                var result = _taxService.CalculateRentIncomeTax(rentIncome, year, useExemption, otherIncome);

                TxtRentIncomeResult.Text = $"{rentIncome:N2} TL";
                TxtRentTaxableAmount.Text = $"{result.Matrah:N2} TL";
                TxtRentTax.Text = $"{result.VergiTutari:N2} TL";
                TxtRentNet.Text = $"{rentIncome - result.VergiTutari:N2} TL";

                RentTaxResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbIncomeYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IncomeTaxResult.Visibility = Visibility.Collapsed;
        }

        private void CmbCorporateYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CorporateTaxResult.Visibility = Visibility.Collapsed;
        }

        private void CmbRentYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRentYear.SelectedItem != null)
            {
                int year = (int)((ComboBoxItem)CmbRentYear.SelectedItem).Tag;
                // İstisna tutarını güncelle (örnek: 2025: 47.000 TL)
                var exemption = year == 2025 ? "47.000" : year == 2024 ? "33.000" : "Bilinmiyor";
                TxtRentExemptionAmount.Text = $"({year}: {exemption} TL)";
            }
            RentTaxResult.Visibility = Visibility.Collapsed;
        }

        private void CmbKdvOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // NumericUpDown için Watermark property yok, XAML'de TextBoxHelper.Watermark kullanılıyor
            if (KdvResult != null)
            {
                KdvResult.Visibility = Visibility.Collapsed;
            }
        }

        // Damga Vergisi Hesaplama
        private void BtnCalculateStampTax_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumStampAmount.Value == null)
                {
                    MessageBox.Show("Lütfen işlem tutarını girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal amount = (decimal)NumStampAmount.Value.Value;
                decimal tax = _taxService.CalculateStampTax(amount);

                TxtStampBase.Text = $"{amount:N2} TL";
                TxtStampTax.Text = $"{tax:N2} TL";
                TxtStampTotal.Text = $"{(amount + tax):N2} TL";

                StampResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MTV Hesaplama
        private void BtnCalculateMtv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumEngineSize.Value == null)
                {
                    MessageBox.Show("Lütfen motor hacmini girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbMtvYear.SelectedItem).Tag.ToString() ?? "2025");
                int engineSize = (int)NumEngineSize.Value.Value;
                bool isMotorcycle = ((ComboBoxItem)CmbVehicleType.SelectedItem).Tag.ToString() == "motorcycle";
                int carAge = isMotorcycle ? 1 : int.Parse(((ComboBoxItem)CmbCarAge.SelectedItem).Tag.ToString() ?? "1");

                decimal tax = _taxService.CalculateMotorVehicleTax(year, engineSize, carAge, isMotorcycle);

                TxtMtvAmount.Text = $"{tax:N2} TL";
                MtvResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbVehicleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PnlCarAge == null) return;
            
            bool isMotorcycle = ((ComboBoxItem)CmbVehicleType.SelectedItem).Tag.ToString() == "motorcycle";
            PnlCarAge.Visibility = isMotorcycle ? Visibility.Collapsed : Visibility.Visible;
            
            if (MtvResult != null)
                MtvResult.Visibility = Visibility.Collapsed;
        }

        // KDV Tevkifatı Hesaplama
        private void BtnCalculateWithholding_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumWithholdingAmount.Value == null || CmbWithholdingCategory.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal amount = (decimal)NumWithholdingAmount.Value.Value;
                string category = ((ComboBoxItem)CmbWithholdingCategory.SelectedItem).Content.ToString() ?? "";

                var (vat, withholding, netVat) = _taxService.CalculateVatWithholding(amount, category);

                TxtWithholdingBase.Text = $"{amount:N2} TL";
                TxtWithholdingVat.Text = $"{vat:N2} TL";
                TxtWithholdingAmount.Text = $"{withholding:N2} TL";
                TxtWithholdingNet.Text = $"{netVat:N2} TL";

                WithholdingResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Vergi Gecikme Faizi Hesaplama
        private void BtnCalculateDelay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumTaxDebt.Value == null || NumDelayDays.Value == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbDelayYear.SelectedItem).Tag.ToString() ?? "2025");
                decimal debt = (decimal)NumTaxDebt.Value.Value;
                int days = (int)NumDelayDays.Value.Value;

                decimal interest = _taxService.CalculateTaxDelayInterest(year, debt, days);

                TxtDelayDebt.Text = $"{debt:N2} TL";
                TxtDelayInterest.Text = $"{interest:N2} TL";
                TxtDelayTotal.Text = $"{(debt + interest):N2} TL";

                DelayResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Değer Artış Kazancı Hesaplama
        private void BtnCalculateCapitalGain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumPurchasePrice.Value == null || NumSalePrice.Value == null)
                {
                    MessageBox.Show("Lütfen tüm alanları doldurun!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbCapitalGainYear.SelectedItem).Tag.ToString() ?? "2025");
                decimal purchase = (decimal)NumPurchasePrice.Value.Value;
                decimal sale = (decimal)NumSalePrice.Value.Value;
                bool isGayrimenkul = ((ComboBoxItem)CmbAssetType.SelectedItem).Tag.ToString() == "gayrimenkul";

                if (sale <= purchase)
                {
                    MessageBox.Show("Satış fiyatı alış fiyatından yüksek olmalıdır!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = _taxService.CalculateCapitalGainTax(year, purchase, sale, isGayrimenkul);

                TxtCapitalGain.Text = $"{result.Matrah:N2} TL";
                TxtCapitalExemption.Text = $"{result.Matrah * 0.5m:N2} TL";
                TxtCapitalTax.Text = $"{result.VergiTutari:N2} TL";
                TxtCapitalNet.Text = $"{result.NetTutar:N2} TL";

                CapitalGainResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Değerli Konut Vergisi Hesaplama
        private void BtnCalculateLuxuryHousing_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumHouseValue.Value == null)
                {
                    MessageBox.Show("Lütfen konut değerini girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbLuxuryYear.SelectedItem).Tag.ToString() ?? "2025");
                decimal value = (decimal)NumHouseValue.Value.Value;

                var result = _taxService.CalculateLuxuryHousingTax(year, value);

                TxtLuxuryValue.Text = $"{value:N2} TL";
                TxtLuxuryTax.Text = $"{result.VergiTutari:N2} TL";

                LuxuryBrackets.ItemsSource = result.Dilimler;
                LuxuryResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Emlak Vergisi Hesaplama
        private void BtnCalculateProperty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumPropertyValue.Value == null)
                {
                    MessageBox.Show("Lütfen emlak değerini girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbPropertyYear.SelectedItem).Tag.ToString() ?? "2025");
                decimal value = (decimal)NumPropertyValue.Value.Value;
                bool isBina = ((ComboBoxItem)CmbPropertyType.SelectedItem).Tag.ToString() == "bina";

                decimal tax = _taxService.CalculatePropertyTax(year, value, isBina);

                TxtPropertyValue.Text = $"{value:N2} TL";
                TxtPropertyTax.Text = $"{tax:N2} TL";

                PropertyResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ÖTV (Akaryakıt) Hesaplama
        private void BtnCalculateSct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumFuelLiters.Value == null)
                {
                    MessageBox.Show("Lütfen litre miktarını girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fuelType = ((ComboBoxItem)CmbFuelType.SelectedItem).Tag.ToString() ?? "Benzin";
                decimal liters = (decimal)NumFuelLiters.Value.Value;

                decimal tax = _taxService.CalculateFuelSCT(fuelType, liters);

                TxtSctLiters.Text = $"{liters:N2} Lt";
                TxtSctAmount.Text = $"{tax:N2} TL";

                SctResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Veraset ve İntikal Vergisi Hesaplama
        private void BtnCalculateInheritance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NumInheritanceAmount.Value == null)
                {
                    MessageBox.Show("Lütfen miras tutarını girin!", "Uyarı", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int year = int.Parse(((ComboBoxItem)CmbInheritanceYear.SelectedItem).Tag.ToString() ?? "2025");
                decimal amount = (decimal)NumInheritanceAmount.Value.Value;
                bool isSpouseOrChild = ((ComboBoxItem)CmbHeirType.SelectedItem).Tag.ToString() == "escocuk";

                var result = _taxService.CalculateInheritanceTax(year, amount, isSpouseOrChild);

                TxtInheritanceAmount.Text = $"{amount:N2} TL";
                TxtInheritanceTax.Text = $"{result.VergiTutari:N2} TL";
                TxtInheritanceNet.Text = $"{result.NetTutar:N2} TL";

                InheritanceBrackets.ItemsSource = result.Dilimler;
                InheritanceResult.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hesaplama hatası: {ex.Message}", "Hata", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
