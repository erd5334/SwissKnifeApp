using SwissKnifeApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwissKnifeApp.Services
{
    public class TaxCalculationService
    {
        private static TaxCalculationService? _instance;
        private static readonly object _lock = new object();

        public static TaxCalculationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TaxCalculationService();
                        }
                    }
                }
                return _instance;
            }
        }

        private TaxRatesData? _taxRates;
        private readonly string _jsonFilePath;
        // Keep the first successfully loaded rates as a baseline to merge missing sections later
        private static TaxRatesData? _baselineRates;

        // Hardcoded fallbacks when scraper provides empty arrays
        private static readonly List<VatWithholdingRate> DefaultVatWithholdingRates = new()
        {
            new VatWithholdingRate { Tanim = "Makine, Teçhizat, Demirbaş", KdvOran = 18, TevkifatOran = 50 },
            new VatWithholdingRate { Tanim = "Bakım Onarım", KdvOran = 20, TevkifatOran = 50 },
            new VatWithholdingRate { Tanim = "Yapı İşleri", KdvOran = 20, TevkifatOran = 30 },
            new VatWithholdingRate { Tanim = "Temizlik", KdvOran = 20, TevkifatOran = 50 },
            new VatWithholdingRate { Tanim = "Özel Güvenlik", KdvOran = 20, TevkifatOran = 50 },
            new VatWithholdingRate { Tanim = "Yemek Servisi", KdvOran = 20, TevkifatOran = 50 },
            new VatWithholdingRate { Tanim = "Hurda Teslimleri", KdvOran = 20, TevkifatOran = 90 },
            new VatWithholdingRate { Tanim = "Metal, Bakır, Çinko", KdvOran = 20, TevkifatOran = 90 },
            new VatWithholdingRate { Tanim = "Pamuk, Kösele, Küspe", KdvOran = 20, TevkifatOran = 90 }
        };

        private static readonly MotorVehicleTaxData DefaultMotorVehicleTax2025 = new()
        {
            Otomobil = new List<CarTaxBracket>
            {
                new CarTaxBracket{ Alt=0, Ust=1300, Yil1=2193, Yil2=3289, Yil3=4932, Yil4=6851, Yil5Plus=9302 },
                new CarTaxBracket{ Alt=1301, Ust=1600, Yil1=3835, Yil2=5753, Yil3=8629, Yil4=11984, Yil5Plus=16267 },
                new CarTaxBracket{ Alt=1601, Ust=1800, Yil1=5753, Yil2=8629, Yil3=12944, Yil4=17976, Yil5Plus=24406 },
                new CarTaxBracket{ Alt=1801, Ust=2000, Yil1=8629, Yil2=12944, Yil3=19416, Yil4=26964, Yil5Plus=36609 },
                new CarTaxBracket{ Alt=2001, Ust=2500, Yil1=12944, Yil2=19416, Yil3=29124, Yil4=40445, Yil5Plus=54914 },
                new CarTaxBracket{ Alt=2501, Ust=3000, Yil1=17259, Yil2=25888, Yil3=38832, Yil4=53927, Yil5Plus=73218 },
                new CarTaxBracket{ Alt=3001, Ust=3500, Yil1=23011, Yil2=34517, Yil3=51775, Yil4=71927, Yil5Plus=97624 },
                new CarTaxBracket{ Alt=3501, Ust=4000, Yil1=30741, Yil2=46112, Yil3=69168, Yil4=96087, Yil5Plus=130432 },
                new CarTaxBracket{ Alt=4001, Ust=999999, Yil1=40329, Yil2=60494, Yil3=90741, Yil4=126087, Yil5Plus=171124 }
            },
            Motosiklet = new List<MotorcycleTaxBracket>
            {
                new MotorcycleTaxBracket{ Alt=0, Ust=100, Tutar=219 },
                new MotorcycleTaxBracket{ Alt=101, Ust=250, Tutar=493 },
                new MotorcycleTaxBracket{ Alt=251, Ust=650, Tutar=1096 },
                new MotorcycleTaxBracket{ Alt=651, Ust=1200, Tutar=2466 },
                new MotorcycleTaxBracket{ Alt=1201, Ust=999999, Tutar=4932 }
            }
        };

        private static readonly List<FuelTaxRate> DefaultFuelSctRates = new()
        {
            new FuelTaxRate{ Tanim="Benzin", Oran=5.17m },
            new FuelTaxRate{ Tanim="Motorin", Oran=3.44m },
            new FuelTaxRate{ Tanim="LPG", Oran=2.07m }
        };

        // Defaults for other taxes
        private static readonly Dictionary<string, CapitalGainTaxData> DefaultCapitalGain = new()
        {
            ["2025"] = new CapitalGainTaxData{ GayrimenkulOran = 50, MenkulOran = 50 }
        };

        private static readonly Dictionary<string, LuxuryHousingTaxData> DefaultLuxuryHousing = new()
        {
            ["2025"] = new LuxuryHousingTaxData
            {
                Esik = 12500000,
                Oranlar = new List<TaxBracket>
                {
                    new TaxBracket{ Alt=12500000, Ust=25000000, Oran=0.6m },
                    new TaxBracket{ Alt=25000000, Ust=50000000, Oran=0.9m },
                    new TaxBracket{ Alt=50000000, Ust=75000000, Oran=1.2m },
                    new TaxBracket{ Alt=75000000, Ust=999999999, Oran=1.5m }
                }
            }
        };

        private static readonly Dictionary<string, PropertyTaxData> DefaultPropertyTax = new()
        {
            ["2025"] = new PropertyTaxData{ BinaOran = 0.2m, AraziOran = 0.1m }
        };

        private static readonly Dictionary<string, InheritanceTaxData> DefaultInheritance = new()
        {
            ["2025"] = new InheritanceTaxData
            {
                Dilimler = new List<TaxBracket>
                {
                    new TaxBracket{ Alt=0, Ust=1500000, Oran=1 },
                    new TaxBracket{ Alt=1500000, Ust=3400000, Oran=3 },
                    new TaxBracket{ Alt=3400000, Ust=6500000, Oran=5 },
                    new TaxBracket{ Alt=6500000, Ust=12000000, Oran=7 },
                    new TaxBracket{ Alt=12000000, Ust=999999999, Oran=10 }
                },
                Istisna = new InheritanceTaxExemption{ EsVeCocuk = 440000, Diger = 110000 }
            }
        };

        private static readonly Dictionary<string, TaxDelayInterestData> DefaultDelayInterest = new()
        {
            ["2025"] = new TaxDelayInterestData{ AylikOran = 3.5m },
            ["2024"] = new TaxDelayInterestData{ AylikOran = 3.5m }
        };

        public TaxCalculationService()
        {
            _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tax-rates.json");
            LoadTaxRates();
            // Capture baseline once on first successful load
            if (_taxRates != null && _baselineRates == null)
            {
                _baselineRates = DeepClone(_taxRates);
                System.Diagnostics.Debug.WriteLine("Baseline tax rates captured.");
            }
        }

        public void LoadTaxRates()
        {
            try
            {
                    System.Diagnostics.Debug.WriteLine("LoadTaxRates başladı...");
                if (File.Exists(_jsonFilePath))
                {
                        System.Diagnostics.Debug.WriteLine($"JSON dosyası bulundu: {_jsonFilePath}");
                    var json = File.ReadAllText(_jsonFilePath);
                        System.Diagnostics.Debug.WriteLine($"JSON okundu. Uzunluk: {json.Length}");
                    
                    _taxRates = JsonSerializer.Deserialize<TaxRatesData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (_taxRates == null)
                    {
                        System.Diagnostics.Debug.WriteLine("TaxRatesData deserialize edilemedi!");
                    }
                    else
                    {
                        // Merge missing sections from baseline (if scraper produced partial JSON)
                        MergeMissingSectionsFromBaseline();
                        // Apply hardcoded fallbacks if still empty
                        ApplyHardcodedFallbacksIfEmpty();
                        System.Diagnostics.Debug.WriteLine($"TaxRates yüklendi. Version: {_taxRates.Version}");
                        System.Diagnostics.Debug.WriteLine($"KDV Tevkifat kategorileri: {_taxRates.KdvTevkifat?.Oranlar?.Count ?? 0}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Dosya bulunamadı: {_jsonFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTaxRates hatası: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    // Don't throw - let the app continue with empty data
            }
        }

        private static TaxRatesData DeepClone(TaxRatesData source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<TaxRatesData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private void MergeMissingSectionsFromBaseline()
        {
            if (_taxRates == null || _baselineRates == null) return;

            // KDV Tevkifatı oranları boş ise baseline'dan al
            if (_taxRates.KdvTevkifat == null)
            {
                _taxRates.KdvTevkifat = _baselineRates.KdvTevkifat;
            }
            else if (_taxRates.KdvTevkifat.Oranlar == null || _taxRates.KdvTevkifat.Oranlar.Count == 0)
            {
                if (_baselineRates.KdvTevkifat?.Oranlar != null && _baselineRates.KdvTevkifat.Oranlar.Count > 0)
                {
                    _taxRates.KdvTevkifat.Oranlar = new List<VatWithholdingRate>(_baselineRates.KdvTevkifat.Oranlar);
                }
            }

            // MTV aralıkları boş ise baseline'dan al
            if (_taxRates.MotorluTasitlarVergisi == null || _taxRates.MotorluTasitlarVergisi.Count == 0)
            {
                _taxRates.MotorluTasitlarVergisi = _baselineRates.MotorluTasitlarVergisi;
            }
            else
            {
                foreach (var key in _baselineRates.MotorluTasitlarVergisi.Keys)
                {
                    if (!_taxRates.MotorluTasitlarVergisi.ContainsKey(key))
                    {
                        _taxRates.MotorluTasitlarVergisi[key] = _baselineRates.MotorluTasitlarVergisi[key];
                    }
                    else
                    {
                        var cur = _taxRates.MotorluTasitlarVergisi[key];
                        var baseVal = _baselineRates.MotorluTasitlarVergisi[key];
                        if ((cur.Otomobil == null || cur.Otomobil.Count == 0) && baseVal.Otomobil != null && baseVal.Otomobil.Count > 0)
                            cur.Otomobil = new List<CarTaxBracket>(baseVal.Otomobil);
                        if ((cur.Motosiklet == null || cur.Motosiklet.Count == 0) && baseVal.Motosiklet != null && baseVal.Motosiklet.Count > 0)
                            cur.Motosiklet = new List<MotorcycleTaxBracket>(baseVal.Motosiklet);
                    }
                }
            }

            // Yakıt ÖTV oranları boş ise baseline'dan al (bonus: akaryakıt SCT hesapları için)
            if (_taxRates.OzelTuketimVergisi == null)
            {
                _taxRates.OzelTuketimVergisi = _baselineRates.OzelTuketimVergisi;
            }
            else
            {
                if ((_taxRates.OzelTuketimVergisi.Akaryakit == null || _taxRates.OzelTuketimVergisi.Akaryakit.Count == 0)
                    && _baselineRates.OzelTuketimVergisi.Akaryakit != null && _baselineRates.OzelTuketimVergisi.Akaryakit.Count > 0)
                {
                    _taxRates.OzelTuketimVergisi.Akaryakit = new List<FuelTaxRate>(_baselineRates.OzelTuketimVergisi.Akaryakit);
                }
                if (_taxRates.OzelTuketimVergisi.Sigara != null && _baselineRates.OzelTuketimVergisi.Sigara != null)
                {
                    if (_taxRates.OzelTuketimVergisi.Sigara.MaktuOran == 0 && _baselineRates.OzelTuketimVergisi.Sigara.MaktuOran > 0)
                        _taxRates.OzelTuketimVergisi.Sigara.MaktuOran = _baselineRates.OzelTuketimVergisi.Sigara.MaktuOran;
                    if (_taxRates.OzelTuketimVergisi.Sigara.NisbiOran == 0 && _baselineRates.OzelTuketimVergisi.Sigara.NisbiOran > 0)
                        _taxRates.OzelTuketimVergisi.Sigara.NisbiOran = _baselineRates.OzelTuketimVergisi.Sigara.NisbiOran;
                }
            }
        }

        private void ApplyHardcodedFallbacksIfEmpty()
        {
            if (_taxRates == null) return;

            // KDV Tevkifat
            if (_taxRates.KdvTevkifat == null)
            {
                _taxRates.KdvTevkifat = new VatWithholdingData { Oranlar = new List<VatWithholdingRate>(DefaultVatWithholdingRates) };
            }
            else if (_taxRates.KdvTevkifat.Oranlar == null || _taxRates.KdvTevkifat.Oranlar.Count == 0)
            {
                _taxRates.KdvTevkifat.Oranlar = new List<VatWithholdingRate>(DefaultVatWithholdingRates);
            }

            // MTV
            if (_taxRates.MotorluTasitlarVergisi == null || _taxRates.MotorluTasitlarVergisi.Count == 0)
            {
                _taxRates.MotorluTasitlarVergisi = new Dictionary<string, MotorVehicleTaxData>
                {
                    ["2025"] = DefaultMotorVehicleTax2025
                };
            }
            else
            {
                if (!_taxRates.MotorluTasitlarVergisi.ContainsKey("2025") ||
                    (_taxRates.MotorluTasitlarVergisi["2025"].Otomobil?.Count ?? 0) == 0)
                {
                    _taxRates.MotorluTasitlarVergisi["2025"] = DefaultMotorVehicleTax2025;
                }
            }

            // Akaryakıt ÖTV
            if (_taxRates.OzelTuketimVergisi == null)
            {
                _taxRates.OzelTuketimVergisi = new SpecialConsumptionTaxData
                {
                    Akaryakit = new List<FuelTaxRate>(DefaultFuelSctRates),
                    Sigara = new CigaretteTaxData { MaktuOran = 0.85m, NisbiOran = 63.38m },
                    Alkol = new List<AlcoholTaxRate>()
                };
            }
            else
            {
                if (_taxRates.OzelTuketimVergisi.Akaryakit == null || _taxRates.OzelTuketimVergisi.Akaryakit.Count == 0)
                    _taxRates.OzelTuketimVergisi.Akaryakit = new List<FuelTaxRate>(DefaultFuelSctRates);
            }

            // Capital Gain
            if (_taxRates.DegerArtisKazanci == null || _taxRates.DegerArtisKazanci.Count == 0)
                _taxRates.DegerArtisKazanci = new Dictionary<string, CapitalGainTaxData>(DefaultCapitalGain);
            else if (!_taxRates.DegerArtisKazanci.ContainsKey("2025"))
                _taxRates.DegerArtisKazanci["2025"] = DefaultCapitalGain["2025"];

            // Luxury Housing
            if (_taxRates.DegerliKonutVergisi == null || _taxRates.DegerliKonutVergisi.Count == 0)
                _taxRates.DegerliKonutVergisi = new Dictionary<string, LuxuryHousingTaxData>(DefaultLuxuryHousing);
            else if (!_taxRates.DegerliKonutVergisi.ContainsKey("2025"))
                _taxRates.DegerliKonutVergisi["2025"] = DefaultLuxuryHousing["2025"];

            // Property Tax
            if (_taxRates.EmlakVergisi == null || _taxRates.EmlakVergisi.Count == 0)
                _taxRates.EmlakVergisi = new Dictionary<string, PropertyTaxData>(DefaultPropertyTax);
            else if (!_taxRates.EmlakVergisi.ContainsKey("2025"))
                _taxRates.EmlakVergisi["2025"] = DefaultPropertyTax["2025"];

            // Inheritance
            if (_taxRates.VerasetIntikal == null || _taxRates.VerasetIntikal.Count == 0)
                _taxRates.VerasetIntikal = new Dictionary<string, InheritanceTaxData>(DefaultInheritance);
            else if (!_taxRates.VerasetIntikal.ContainsKey("2025"))
                _taxRates.VerasetIntikal["2025"] = DefaultInheritance["2025"];

            // Delay Interest
            if (_taxRates.VergiGecikme == null || _taxRates.VergiGecikme.Count == 0)
                _taxRates.VergiGecikme = new Dictionary<string, TaxDelayInterestData>(DefaultDelayInterest);
            else
            {
                foreach (var kv in DefaultDelayInterest)
                    if (!_taxRates.VergiGecikme.ContainsKey(kv.Key))
                        _taxRates.VergiGecikme[kv.Key] = kv.Value;
            }
        }

        public async Task RefreshTaxRatesAsync()
        {
            // TODO: Web scraping ile güncel oranları çek
            LoadTaxRates();
        }

        public DateTime GetLastUpdateDate()
        {
            return _taxRates?.LastUpdated ?? DateTime.MinValue;
        }

        public string GetVersion()
        {
            return _taxRates?.Version ?? "Bilinmiyor";
        }

        // Gelir Vergisi Hesaplama
        public TaxCalculationResult CalculateIncomeTax(decimal matrah, int yil, bool ucretGeliri)
        {
            if (_taxRates == null || !_taxRates.GelirVergisi.ContainsKey(yil.ToString()))
                throw new Exception($"{yil} yılı için vergi dilimi bulunamadı.");

            var yearData = _taxRates.GelirVergisi[yil.ToString()];
            var brackets = ucretGeliri ? yearData.Ucret : yearData.UcretDisi;

            var result = new TaxCalculationResult
            {
                Matrah = matrah,
                Dilimler = new List<TaxBracketResult>()
            };

            decimal toplamVergi = 0;
            decimal kalanMatrah = matrah;

            foreach (var bracket in brackets.OrderBy(b => b.Alt))
            {
                if (kalanMatrah <= 0) break;

                decimal dilimMatrah = 0;

                if (matrah > bracket.Ust)
                {
                    dilimMatrah = bracket.Ust - bracket.Alt;
                }
                else if (matrah > bracket.Alt)
                {
                    dilimMatrah = matrah - bracket.Alt;
                }

                if (dilimMatrah > 0)
                {
                    decimal dilimVergi = (dilimMatrah * bracket.Oran) / 100;
                    toplamVergi += dilimVergi;

                    result.Dilimler.Add(new TaxBracketResult
                    {
                        Aciklama = $"{bracket.Alt:N0} - {bracket.Ust:N0} TL arası (%{bracket.Oran})",
                        Matrah = dilimMatrah,
                        Oran = bracket.Oran,
                        VergiTutari = dilimVergi
                    });

                    kalanMatrah -= dilimMatrah;
                }
            }

            result.VergiTutari = toplamVergi;
            result.NetTutar = matrah - toplamVergi;

            return result;
        }

        // KDV Hesaplama
        public (decimal kdvTutari, decimal toplam) CalculateKdvDahil(decimal netTutar, decimal kdvOrani)
        {
            decimal kdvTutari = netTutar * (kdvOrani / 100);
            decimal toplam = netTutar + kdvTutari;
            return (kdvTutari, toplam);
        }

        public (decimal netTutar, decimal kdvTutari) CalculateKdvHaric(decimal brutTutar, decimal kdvOrani)
        {
            decimal netTutar = brutTutar / (1 + (kdvOrani / 100));
            decimal kdvTutari = brutTutar - netTutar;
            return (netTutar, kdvTutari);
        }

        // Kurumlar Vergisi Hesaplama
        public decimal CalculateCorporateTax(decimal matrah, int yil, bool finansKurumu = false)
        {
            if (_taxRates == null || !_taxRates.KurumlarVergisi.ContainsKey(yil.ToString()))
            {
                // Fallback için 2018-2020 kontrolü
                if (yil >= 2018 && yil <= 2020 && _taxRates.KurumlarVergisi.ContainsKey("2018-2020"))
                {
                    var rate = _taxRates.KurumlarVergisi["2018-2020"];
                    decimal oran = finansKurumu ? rate.Finans : rate.Normal;
                    return matrah * (oran / 100);
                }
                throw new Exception($"{yil} yılı için kurumlar vergisi oranı bulunamadı.");
            }

            var yearData = _taxRates.KurumlarVergisi[yil.ToString()];
            decimal vergiOrani = finansKurumu ? yearData.Finans : yearData.Normal;
            return matrah * (vergiOrani / 100);
        }

        // Kira Gelir Vergisi Hesaplama
        public TaxCalculationResult CalculateRentIncomeTax(decimal kiraGeliri, int yil, bool konutIstisnasi, decimal digerGelirler = 0)
        {
            if (_taxRates == null)
                throw new Exception("Vergi oranları yüklenemedi.");

            // İstisna uygula
            decimal vergilendirilebilirMatrah = kiraGeliri;
            if (konutIstisnasi && _taxRates.KiraVergisi.ContainsKey(yil.ToString()))
            {
                var istisna = _taxRates.KiraVergisi[yil.ToString()].KonutIstisnasi;
                vergilendirilebilirMatrah = Math.Max(0, kiraGeliri - istisna);
            }

            // Toplam matrah
            decimal toplamMatrah = vergilendirilebilirMatrah + digerGelirler;

            // Gelir vergisi dilimlerine göre hesapla
            return CalculateIncomeTax(toplamMatrah, yil, false);
        }

        // Damga Vergisi Hesaplama
        public decimal CalculateStampTax(decimal matrah)
        {
            if (_taxRates == null)
                throw new Exception("Vergi oranları yüklenemedi.");

            return matrah * (_taxRates.DamgaVergisi.Genel / 1000);
        }

        public List<int> GetAvailableKdvRates()
        {
            return _taxRates?.Kdv.Oranlar ?? new List<int> { 1, 8, 10, 18, 20 };
        }

        public List<int> GetAvailableYears(string vergiTuru)
        {
            if (_taxRates == null) return new List<int>();

            return vergiTuru switch
            {
                "gelir" => _taxRates.GelirVergisi.Keys.Select(k => int.Parse(k)).OrderByDescending(y => y).ToList(),
                "kira" => _taxRates.KiraVergisi.Keys.Select(k => int.Parse(k)).OrderByDescending(y => y).ToList(),
                "kurumlar" => _taxRates.KurumlarVergisi.Keys
                    .Where(k => !k.Contains("-"))
                    .Select(k => int.Parse(k))
                    .OrderByDescending(y => y)
                    .ToList(),
                _ => new List<int>()
            };
        }

        // Değer Artış Kazancı Vergisi
        public TaxCalculationResult CalculateCapitalGainTax(int year, decimal alisFiyati, decimal satisFiyati, bool isGayrimenkul)
        {
            if (_taxRates == null || !_taxRates.DegerArtisKazanci.ContainsKey(year.ToString()))
                return new TaxCalculationResult();

            var data = _taxRates.DegerArtisKazanci[year.ToString()];
            var kazanc = satisFiyati - alisFiyati;
            var istisnaTutari = kazanc * 0.5m; // %50 istisna
            var vergiyeTabiMatrah = kazanc - istisnaTutari;
            
            // Gelir vergisi dilimleri üzerinden hesapla
            var incomeTaxResult = CalculateIncomeTax(vergiyeTabiMatrah, year, false);
            
            return new TaxCalculationResult
            {
                Matrah = kazanc,
                VergiTutari = incomeTaxResult.VergiTutari,
                NetTutar = satisFiyati - incomeTaxResult.VergiTutari,
                Dilimler = new List<TaxBracketResult>
                {
                    new TaxBracketResult { Aciklama = "Kazanç", Matrah = kazanc, Oran = 0, VergiTutari = 0 },
                    new TaxBracketResult { Aciklama = "İstisna (%50)", Matrah = istisnaTutari, Oran = 50, VergiTutari = 0 },
                    new TaxBracketResult { Aciklama = "Vergiye Tabi Matrah", Matrah = vergiyeTabiMatrah, Oran = 0, VergiTutari = incomeTaxResult.VergiTutari }
                }
            };
        }

        // Değerli Konut Vergisi
        public TaxCalculationResult CalculateLuxuryHousingTax(int year, decimal konutDegeri)
        {
            if (_taxRates == null || !_taxRates.DegerliKonutVergisi.ContainsKey(year.ToString()))
                return new TaxCalculationResult();

            var data = _taxRates.DegerliKonutVergisi[year.ToString()];
            
            if (konutDegeri < data.Esik)
            {
                return new TaxCalculationResult
                {
                    Matrah = konutDegeri,
                    VergiTutari = 0,
                    NetTutar = konutDegeri,
                    Dilimler = new List<TaxBracketResult>
                    {
                        new TaxBracketResult { Aciklama = "Değer eşiğin altında, vergi yok", Matrah = konutDegeri, Oran = 0, VergiTutari = 0 }
                    }
                };
            }

            decimal toplamVergi = 0;
            var dilimler = new List<TaxBracketResult>();

            foreach (var bracket in data.Oranlar.OrderBy(b => b.Alt))
            {
                if (konutDegeri <= bracket.Alt) break;

                var dilimMatrah = Math.Min(konutDegeri, bracket.Ust) - bracket.Alt;
                var dilimVergi = dilimMatrah * (bracket.Oran / 100);
                toplamVergi += dilimVergi;

                dilimler.Add(new TaxBracketResult
                {
                    Aciklama = $"{bracket.Alt:N0} - {bracket.Ust:N0} TL (%{bracket.Oran})",
                    Matrah = dilimMatrah,
                    Oran = bracket.Oran,
                    VergiTutari = dilimVergi
                });
            }

            return new TaxCalculationResult
            {
                Matrah = konutDegeri,
                VergiTutari = toplamVergi,
                NetTutar = konutDegeri - toplamVergi,
                Dilimler = dilimler
            };
        }

        // Emlak Vergisi
        public decimal CalculatePropertyTax(int year, decimal emlakDegeri, bool isBina)
        {
            if (_taxRates == null || !_taxRates.EmlakVergisi.ContainsKey(year.ToString()))
                return 0;

            var data = _taxRates.EmlakVergisi[year.ToString()];
            var oran = isBina ? data.BinaOran : data.AraziOran;
            return emlakDegeri * (oran / 100);
        }

        // KDV Tevkifatı Hesaplama
        public (decimal KdvTutari, decimal TevkifatTutari, decimal OdenecekKdv) CalculateVatWithholding(decimal tutar, string hizmetTuru)
        {
            if (_taxRates == null) return (0, 0, 0);

            var tevkifat = _taxRates.KdvTevkifat.Oranlar.FirstOrDefault(t => t.Tanim == hizmetTuru);
            if (tevkifat == null) return (0, 0, 0);

            var kdvTutari = tutar * (tevkifat.KdvOran / 100);
            var tevkifatTutari = kdvTutari * (tevkifat.TevkifatOran / 100);
            var odenecekKdv = kdvTutari - tevkifatTutari;

            return (kdvTutari, tevkifatTutari, odenecekKdv);
        }

        // Kira Stopajı
        public decimal CalculateRentWithholding(int year, decimal aylikKira, int aySayisi)
        {
            if (_taxRates == null || !_taxRates.KiraStopaj.ContainsKey(year.ToString()))
                return 0;

            var data = _taxRates.KiraStopaj[year.ToString()];
            var yillikKira = aylikKira * aySayisi;

            if (yillikKira <= data.IstisnaSiniri)
                return 0;

            return yillikKira * (data.Oran / 100);
        }

        // Konaklama Vergisi
        public decimal CalculateAccommodationTax(int year, decimal konaklamaBedeli)
        {
            if (_taxRates == null || !_taxRates.KonaklamaVergisi.ContainsKey(year.ToString()))
                return 0;

            var data = _taxRates.KonaklamaVergisi[year.ToString()];
            return konaklamaBedeli * (data.Oran / 100);
        }

        // MTV Hesaplama
        public decimal CalculateMotorVehicleTax(int year, int motorHacmi, int aracYasi, bool isMotorcycle)
        {
            if (_taxRates == null || !_taxRates.MotorluTasitlarVergisi.ContainsKey(year.ToString()))
                return 0;

            var data = _taxRates.MotorluTasitlarVergisi[year.ToString()];

            if (isMotorcycle)
            {
                var bracket = data.Motosiklet.FirstOrDefault(b => motorHacmi >= b.Alt && motorHacmi <= b.Ust);
                return bracket?.Tutar ?? 0;
            }
            else
            {
                // Otomobil için son dilimi kontrol et (Ust = 999999)
                var bracket = data.Otomobil.FirstOrDefault(b => 
                    (b.Ust < 999999 && motorHacmi >= b.Alt && motorHacmi <= b.Ust) ||
                    (b.Ust >= 999999 && motorHacmi >= b.Alt));
                
                if (bracket == null) return 0;

                return aracYasi switch
                {
                    1 => bracket.Yil1,
                    2 => bracket.Yil2,
                    3 => bracket.Yil3,
                    4 => bracket.Yil4,
                    _ => bracket.Yil5Plus
                };
            }
        }

        // ÖTV Hesaplama - Akaryakıt
        public decimal CalculateFuelSCT(string yakitTuru, decimal litre)
        {
            if (_taxRates == null) return 0;

            var fuel = _taxRates.OzelTuketimVergisi.Akaryakit.FirstOrDefault(f => f.Tanim == yakitTuru);
            return fuel != null ? litre * fuel.Oran : 0;
        }

        // ÖTV Hesaplama - Sigara
        public decimal CalculateCigaretteSCT(decimal fiyat, int adet)
        {
            if (_taxRates == null) return 0;

            var data = _taxRates.OzelTuketimVergisi.Sigara;
            var nisbiOtv = fiyat * (data.NisbiOran / 100);
            var maktuOtv = adet * data.MaktuOran;
            return nisbiOtv + maktuOtv;
        }

        // Veraset ve İntikal Vergisi
        public TaxCalculationResult CalculateInheritanceTax(int year, decimal mirasci, bool isSpouseOrChild)
        {
            if (_taxRates == null || !_taxRates.VerasetIntikal.ContainsKey(year.ToString()))
                return new TaxCalculationResult();

            var data = _taxRates.VerasetIntikal[year.ToString()];
            var istisna = isSpouseOrChild ? data.Istisna.EsVeCocuk : data.Istisna.Diger;
            var vergiyeTabiMatrah = Math.Max(0, mirasci - istisna);

            if (vergiyeTabiMatrah == 0)
            {
                return new TaxCalculationResult
                {
                    Matrah = mirasci,
                    VergiTutari = 0,
                    NetTutar = mirasci,
                    Dilimler = new List<TaxBracketResult>
                    {
                        new TaxBracketResult { Aciklama = "İstisna tutarın altında", Matrah = mirasci, Oran = 0, VergiTutari = 0 }
                    }
                };
            }

            decimal toplamVergi = 0;
            var dilimler = new List<TaxBracketResult>();

            foreach (var bracket in data.Dilimler.OrderBy(b => b.Alt))
            {
                if (vergiyeTabiMatrah <= bracket.Alt) break;

                var dilimMatrah = Math.Min(vergiyeTabiMatrah, bracket.Ust) - bracket.Alt;
                var dilimVergi = dilimMatrah * (bracket.Oran / 100);
                toplamVergi += dilimVergi;

                dilimler.Add(new TaxBracketResult
                {
                    Aciklama = $"{bracket.Alt:N0} - {bracket.Ust:N0} TL (%{bracket.Oran})",
                    Matrah = dilimMatrah,
                    Oran = bracket.Oran,
                    VergiTutari = dilimVergi
                });
            }

            return new TaxCalculationResult
            {
                Matrah = vergiyeTabiMatrah,
                VergiTutari = toplamVergi,
                NetTutar = mirasci - toplamVergi,
                Dilimler = dilimler
            };
        }

        // Vergi Gecikme Faizi
        public decimal CalculateTaxDelayInterest(int year, decimal vergiBorcu, int gunSayisi)
        {
            if (_taxRates == null || !_taxRates.VergiGecikme.ContainsKey(year.ToString()))
                return 0;

            var data = _taxRates.VergiGecikme[year.ToString()];
            var aylikOran = data.AylikOran / 100;
            var gunlukOran = aylikOran / 30;
            return vergiBorcu * gunlukOran * gunSayisi;
        }

        // Tevkifat kategorilerini getir
        public List<string> GetVatWithholdingCategories()
        {
                System.Diagnostics.Debug.WriteLine($"GetVatWithholdingCategories çağrıldı. _taxRates null mu? {_taxRates == null}");
            
                if (_taxRates == null)
                {
                    System.Diagnostics.Debug.WriteLine("_taxRates null!");
                    return new List<string>();
                }
            
                System.Diagnostics.Debug.WriteLine($"KdvTevkifat null mu? {_taxRates.KdvTevkifat == null}");
            
                if (_taxRates.KdvTevkifat == null)
                {
                    System.Diagnostics.Debug.WriteLine("KdvTevkifat null!");
                    return new List<string>();
                }
            
                System.Diagnostics.Debug.WriteLine($"KdvTevkifat.Oranlar null mu? {_taxRates.KdvTevkifat.Oranlar == null}");
                System.Diagnostics.Debug.WriteLine($"KdvTevkifat.Oranlar Count: {_taxRates.KdvTevkifat.Oranlar?.Count ?? 0}");
            
                var categories = _taxRates.KdvTevkifat.Oranlar.Select(o => o.Tanim).ToList();
                System.Diagnostics.Debug.WriteLine($"Döndürülen kategori sayısı: {categories.Count}");
            
                return categories;
        }

        // Yakıt türlerini getir
        public List<string> GetFuelTypes()
        {
            return _taxRates?.OzelTuketimVergisi.Akaryakit.Select(f => f.Tanim).ToList() ?? new List<string>();
        }

        #region Multi-Year Comparison

        /// <summary>
        /// Yıllara göre vergi karşılaştırması yapar
        /// </summary>
        public List<TaxComparisonResult> CompareMultiYear(decimal matrah, List<int> years, bool ucretGeliri)
        {
            var results = new List<TaxComparisonResult>();

            foreach (var year in years)
            {
                try
                {
                    var result = CalculateIncomeTax(matrah, year, ucretGeliri);
                    var efektifOran = matrah > 0 ? (result.VergiTutari / matrah * 100) : 0;

                    results.Add(new TaxComparisonResult
                    {
                        Year = year,
                        Matrah = matrah,
                        VergiTutari = result.VergiTutari,
                        NetTutar = result.NetTutar,
                        EfektifVergiOrani = efektifOran,
                        DilimSayisi = result.Dilimler.Count
                    });
                }
                catch
                {
                    // Yıl verileri yoksa atla
                }
            }

            return results;
        }

        /// <summary>
        /// Tüm mevcut yıllar için karşılaştırma yapar
        /// </summary>
        public List<TaxComparisonResult> CompareAllYears(decimal matrah, bool ucretGeliri)
        {
            var years = GetAvailableYears("gelir");
            return CompareMultiYear(matrah, years, ucretGeliri);
        }

        #endregion

        #region Tax Planning Calculator

        /// <summary>
        /// Vergi planlaması - optimum gelir dağılımı önerir
        /// </summary>
        public TaxPlanningResult PlanTax(decimal yillikGelir, int year)
        {
            var result = new TaxPlanningResult
            {
                YillikGelir = yillikGelir,
                Year = year
            };

            // Normal vergi hesabı
            var normalTax = CalculateIncomeTax(yillikGelir, year, true);
            result.NormalVergi = normalTax.VergiTutari;
            result.NormalNetGelir = normalTax.NetTutar;

            // Geliri 2 yıla bölme senaryosu
            var halfIncome = yillikGelir / 2;
            var splitTax = CalculateIncomeTax(halfIncome, year, true);
            result.BolunmusVergi = splitTax.VergiTutari * 2;
            result.BolunmusTasarruf = result.NormalVergi - result.BolunmusVergi;

            // Vergi optimizasyonu önerileri
            result.Recommendations = new List<string>();

            if (_taxRates != null && _taxRates.GelirVergisi.ContainsKey(year.ToString()))
            {
                var brackets = _taxRates.GelirVergisi[year.ToString()].Ucret;
                var currentBracket = brackets.FirstOrDefault(b => yillikGelir > b.Alt && yillikGelir <= b.Ust);
                var lowerBracket = brackets.Where(b => b.Ust < yillikGelir).OrderByDescending(b => b.Ust).FirstOrDefault();

                if (currentBracket != null && lowerBracket != null)
                {
                    var savingsAtLowerBracket = yillikGelir - lowerBracket.Ust;
                    if (savingsAtLowerBracket > 0)
                    {
                        result.Recommendations.Add($"💡 {savingsAtLowerBracket:N0} TL'lik geliri erteleyerek %{currentBracket.Oran - lowerBracket.Oran} daha az vergi ödeyebilirsiniz.");
                    }
                }
            }

            result.Recommendations.Add("📌 BES katkı payları ile vergi matrahını düşürebilirsiniz.");
            result.Recommendations.Add("📌 Eğitim ve sağlık giderleri matrahtan düşülebilir.");
            result.Recommendations.Add("📌 Bağış ve yardımlar vergi avantajı sağlar.");

            return result;
        }

        #endregion

        #region Export Functions

        /// <summary>
        /// Vergi hesaplamalarını Excel'e aktarır
        /// </summary>
        public void ExportToExcel(TaxCalculationResult result, string vergiTuru, string outputPath)
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Vergi Hesaplaması");

            // Başlık
            ws.Cells["A1"].Value = $"{vergiTuru} - Vergi Hesaplama Raporu";
            ws.Cells["A1:D1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;

            // Özet
            ws.Cells["A3"].Value = "Matrah:";
            ws.Cells["B3"].Value = result.Matrah;
            ws.Cells["B3"].Style.Numberformat.Format = "#,##0 TL";

            ws.Cells["A4"].Value = "Toplam Vergi:";
            ws.Cells["B4"].Value = result.VergiTutari;
            ws.Cells["B4"].Style.Numberformat.Format = "#,##0 TL";

            ws.Cells["A5"].Value = "Net Tutar:";
            ws.Cells["B5"].Value = result.NetTutar;
            ws.Cells["B5"].Style.Numberformat.Format = "#,##0 TL";

            // Dilim detayları
            ws.Cells["A7"].Value = "Dilim";
            ws.Cells["B7"].Value = "Matrah";
            ws.Cells["C7"].Value = "Oran";
            ws.Cells["D7"].Value = "Vergi";
            ws.Cells["A7:D7"].Style.Font.Bold = true;

            int row = 8;
            foreach (var dilim in result.Dilimler)
            {
                ws.Cells[$"A{row}"].Value = dilim.Aciklama;
                ws.Cells[$"B{row}"].Value = dilim.Matrah;
                ws.Cells[$"C{row}"].Value = $"%{dilim.Oran}";
                ws.Cells[$"D{row}"].Value = dilim.VergiTutari;
                row++;
            }

            ws.Cells.AutoFitColumns();
            package.SaveAs(new FileInfo(outputPath));
        }

        /// <summary>
        /// Vergi hesaplamalarını PDF'e aktarır
        /// </summary>
        public void ExportToPdf(TaxCalculationResult result, string vergiTuru, string outputPath)
        {
            var pdf = new PdfSharp.Pdf.PdfDocument();
            var page = pdf.AddPage();
            var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

            var titleFont = new PdfSharp.Drawing.XFont("Verdana", 18);
            var headerFont = new PdfSharp.Drawing.XFont("Verdana", 12);
            var normalFont = new PdfSharp.Drawing.XFont("Verdana", 10);

            double y = 50;

            // Başlık
            gfx.DrawString($"{vergiTuru} - Vergi Hesaplama Raporu", titleFont, PdfSharp.Drawing.XBrushes.SteelBlue, new PdfSharp.Drawing.XPoint(50, y));
            y += 40;

            gfx.DrawString($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont, PdfSharp.Drawing.XBrushes.Gray, new PdfSharp.Drawing.XPoint(50, y));
            y += 30;

            // Özet bilgiler
            gfx.DrawString($"Matrah: {result.Matrah:N0} TL", headerFont, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(50, y));
            y += 25;
            gfx.DrawString($"Toplam Vergi: {result.VergiTutari:N0} TL", headerFont, PdfSharp.Drawing.XBrushes.Red, new PdfSharp.Drawing.XPoint(50, y));
            y += 25;
            gfx.DrawString($"Net Tutar: {result.NetTutar:N0} TL", headerFont, PdfSharp.Drawing.XBrushes.Green, new PdfSharp.Drawing.XPoint(50, y));
            y += 40;

            // Dilimler
            gfx.DrawString("Vergi Dilimleri", headerFont, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(50, y));
            y += 25;

            foreach (var dilim in result.Dilimler)
            {
                gfx.DrawString($"• {dilim.Aciklama}: {dilim.VergiTutari:N0} TL", normalFont, PdfSharp.Drawing.XBrushes.DarkGray, new PdfSharp.Drawing.XPoint(60, y));
                y += 20;
            }

            pdf.Save(outputPath);
        }

        #endregion

        #region Tax Deadline Reminders

        /// <summary>
        /// Vergi takvimini ve son ödeme tarihlerini döndürür
        /// </summary>
        public List<TaxDeadline> GetTaxDeadlines(int year)
        {
            return new List<TaxDeadline>
            {
                // Gelir Vergisi
                new TaxDeadline { VergiTuru = "Gelir Vergisi Beyannamesi", SonTarih = new DateTime(year, 3, 31), Aciklama = "Yıllık gelir vergisi beyannamesi" },
                new TaxDeadline { VergiTuru = "Gelir Vergisi 1. Taksit", SonTarih = new DateTime(year, 3, 31), Aciklama = "1. taksit ödeme" },
                new TaxDeadline { VergiTuru = "Gelir Vergisi 2. Taksit", SonTarih = new DateTime(year, 7, 31), Aciklama = "2. taksit ödeme" },
                
                // Kurumlar Vergisi
                new TaxDeadline { VergiTuru = "Kurumlar Vergisi Beyannamesi", SonTarih = new DateTime(year, 4, 30), Aciklama = "Yıllık kurumlar vergisi beyannamesi" },
                
                // KDV
                new TaxDeadline { VergiTuru = "Aylık KDV Beyannamesi", SonTarih = new DateTime(year, 2, 28), Aciklama = "Her ayın 28'ine kadar" },
                
                // Geçici Vergi
                new TaxDeadline { VergiTuru = "1. Dönem Geçici Vergi", SonTarih = new DateTime(year, 5, 17), Aciklama = "Ocak-Şubat-Mart dönemi" },
                new TaxDeadline { VergiTuru = "2. Dönem Geçici Vergi", SonTarih = new DateTime(year, 8, 17), Aciklama = "Nisan-Mayıs-Haziran dönemi" },
                new TaxDeadline { VergiTuru = "3. Dönem Geçici Vergi", SonTarih = new DateTime(year, 11, 17), Aciklama = "Temmuz-Ağustos-Eylül dönemi" },
                new TaxDeadline { VergiTuru = "4. Dönem Geçici Vergi", SonTarih = new DateTime(year + 1, 2, 17), Aciklama = "Ekim-Kasım-Aralık dönemi" },
                
                // MTV
                new TaxDeadline { VergiTuru = "MTV 1. Taksit", SonTarih = new DateTime(year, 1, 31), Aciklama = "Motorlu Taşıtlar Vergisi 1. taksit" },
                new TaxDeadline { VergiTuru = "MTV 2. Taksit", SonTarih = new DateTime(year, 7, 31), Aciklama = "Motorlu Taşıtlar Vergisi 2. taksit" },
                
                // Emlak Vergisi
                new TaxDeadline { VergiTuru = "Emlak Vergisi 1. Taksit", SonTarih = new DateTime(year, 5, 31), Aciklama = "Emlak Vergisi 1. taksit" },
                new TaxDeadline { VergiTuru = "Emlak Vergisi 2. Taksit", SonTarih = new DateTime(year, 11, 30), Aciklama = "Emlak Vergisi 2. taksit" },
                
                // Damga Vergisi
                new TaxDeadline { VergiTuru = "Damga Vergisi Beyannamesi", SonTarih = new DateTime(year, 2, 28), Aciklama = "Aylık damga vergisi beyannamesi" }
            };
        }

        /// <summary>
        /// Yaklaşan vergi tarihlerini döndürür
        /// </summary>
        public List<TaxDeadline> GetUpcomingDeadlines(int daysAhead = 30)
        {
            var today = DateTime.Today;
            var currentYear = today.Year;
            var allDeadlines = GetTaxDeadlines(currentYear);
            
            return allDeadlines
                .Where(d => d.SonTarih >= today && d.SonTarih <= today.AddDays(daysAhead))
                .OrderBy(d => d.SonTarih)
                .ToList();
        }

        #endregion

        #region Historical Data Archive

        private readonly string _historyPath;

        /// <summary>
        /// Vergi hesaplamasını arşive kaydeder
        /// </summary>
        public void SaveToHistory(TaxCalculationResult result, string vergiTuru, string aciklama = "")
        {
            var historyDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "TaxHistory");
            Directory.CreateDirectory(historyDir);

            var entry = new TaxHistoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                Tarih = DateTime.Now,
                VergiTuru = vergiTuru,
                Matrah = result.Matrah,
                VergiTutari = result.VergiTutari,
                NetTutar = result.NetTutar,
                Aciklama = aciklama,
                DilimlerJson = System.Text.Json.JsonSerializer.Serialize(result.Dilimler)
            };

            var historyFile = Path.Combine(historyDir, $"{DateTime.Now:yyyy-MM}.json");
            var entries = new List<TaxHistoryEntry>();

            if (File.Exists(historyFile))
            {
                var existing = File.ReadAllText(historyFile);
                entries = System.Text.Json.JsonSerializer.Deserialize<List<TaxHistoryEntry>>(existing) ?? new List<TaxHistoryEntry>();
            }

            entries.Add(entry);
            File.WriteAllText(historyFile, System.Text.Json.JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Vergi geçmişini getirir
        /// </summary>
        public List<TaxHistoryEntry> GetHistory(int? year = null, int? month = null)
        {
            var historyDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "TaxHistory");
            if (!Directory.Exists(historyDir))
                return new List<TaxHistoryEntry>();

            var allEntries = new List<TaxHistoryEntry>();
            var files = Directory.GetFiles(historyDir, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (year.HasValue && !fileName.StartsWith(year.Value.ToString())) continue;
                    if (month.HasValue && year.HasValue && fileName != $"{year.Value}-{month.Value:D2}") continue;

                    var content = File.ReadAllText(file);
                    var entries = System.Text.Json.JsonSerializer.Deserialize<List<TaxHistoryEntry>>(content);
                    if (entries != null)
                        allEntries.AddRange(entries);
                }
                catch { }
            }

            return allEntries.OrderByDescending(e => e.Tarih).ToList();
        }

        /// <summary>
        /// Belirli tarih aralığındaki geçmişi getirir
        /// </summary>
        public List<TaxHistoryEntry> GetHistoryByDateRange(DateTime start, DateTime end)
        {
            var all = GetHistory();
            return all.Where(e => e.Tarih >= start && e.Tarih <= end).ToList();
        }

        #endregion
    }

    #region Supporting Classes

    public class TaxComparisonResult
    {
        public int Year { get; set; }
        public decimal Matrah { get; set; }
        public decimal VergiTutari { get; set; }
        public decimal NetTutar { get; set; }
        public decimal EfektifVergiOrani { get; set; }
        public int DilimSayisi { get; set; }
    }

    public class TaxPlanningResult
    {
        public decimal YillikGelir { get; set; }
        public int Year { get; set; }
        public decimal NormalVergi { get; set; }
        public decimal NormalNetGelir { get; set; }
        public decimal BolunmusVergi { get; set; }
        public decimal BolunmusTasarruf { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public class TaxDeadline
    {
        public string VergiTuru { get; set; } = "";
        public DateTime SonTarih { get; set; }
        public string Aciklama { get; set; } = "";
        public int KalanGun => (SonTarih - DateTime.Today).Days;
        public bool IsUrgent => KalanGun <= 7;
    }

    public class TaxHistoryEntry
    {
        public string Id { get; set; } = "";
        public DateTime Tarih { get; set; }
        public string VergiTuru { get; set; } = "";
        public decimal Matrah { get; set; }
        public decimal VergiTutari { get; set; }
        public decimal NetTutar { get; set; }
        public string Aciklama { get; set; } = "";
        public string DilimlerJson { get; set; } = "";
    }

    #endregion
}
