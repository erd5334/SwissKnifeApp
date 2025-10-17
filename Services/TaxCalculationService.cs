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
        private TaxRatesData? _taxRates;
        private readonly string _jsonFilePath;

        public TaxCalculationService()
        {
            _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tax-rates.json");
            LoadTaxRates();
        }

        private void LoadTaxRates()
        {
            try
            {
                if (File.Exists(_jsonFilePath))
                {
                    var json = File.ReadAllText(_jsonFilePath);
                    _taxRates = JsonSerializer.Deserialize<TaxRatesData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Vergi oranları yüklenirken hata: {ex.Message}");
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
                var bracket = data.Otomobil.FirstOrDefault(b => motorHacmi >= b.Alt && motorHacmi <= b.Ust);
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
            return _taxRates?.KdvTevkifat.Oranlar.Select(o => o.Tanim).ToList() ?? new List<string>();
        }

        // Yakıt türlerini getir
        public List<string> GetFuelTypes()
        {
            return _taxRates?.OzelTuketimVergisi.Akaryakit.Select(f => f.Tanim).ToList() ?? new List<string>();
        }
    }
}
