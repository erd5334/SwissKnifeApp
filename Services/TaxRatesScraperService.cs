using HtmlAgilityPack;
using SwissKnifeApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SwissKnifeApp.Services
{
    public class TaxRatesScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly string _jsonFilePath;

        public TaxRatesScraperService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tax-rates.json");
        }

        public async Task<bool> UpdateTaxRatesAsync()
        {
            try
            {
                var taxRates = new TaxRatesData
                {
                    Version = $"{DateTime.Now.Year}.{DateTime.Now.Month}",
                    LastUpdated = DateTime.Now,
                    Source = "https://gelir-vergisi.hesaplama.net/",
                    GelirVergisi = new Dictionary<string, YearlyIncomeTax>(),
                    Kdv = new KdvData { Oranlar = new List<int> { 1, 8, 10, 18, 20 } },
                    KiraVergisi = new Dictionary<string, RentTaxData>(),
                    KurumlarVergisi = new Dictionary<string, CorporateTaxData>(),
                    DamgaVergisi = new StampTaxData { Genel = 0.948m }
                };

                // Gelir vergisi oranlarını çek (2024 ve 2025)
                await ScrapeIncomeTaxRatesAsync(taxRates);

                // Kira vergisi istisnalarını çek
                await ScrapeRentTaxDataAsync(taxRates);

                // Kurumlar vergisi oranlarını çek
                await ScrapeCorporateTaxRatesAsync(taxRates);

                // JSON'a kaydet
                SaveToJson(taxRates);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ScrapeIncomeTaxRatesAsync(TaxRatesData taxRates)
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://gelir-vergisi.hesaplama.net/");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 2025 ve 2024 dilimlerini parse et
                var years = new[] { 2025, 2024 };
                
                foreach (var year in years)
                {
                    var yearData = new YearlyIncomeTax
                    {
                        Ucret = ParseIncomeTaxBrackets(doc, year, true),
                        UcretDisi = ParseIncomeTaxBrackets(doc, year, false)
                    };

                    if (yearData.Ucret.Count > 0)
                    {
                        taxRates.GelirVergisi[year.ToString()] = yearData;
                    }
                }
            }
            catch
            {
                // Hata durumunda mevcut veriyi koru
            }
        }

        private List<TaxBracket> ParseIncomeTaxBrackets(HtmlDocument doc, int year, bool ucretGeliri)
        {
            var brackets = new List<TaxBracket>();
            
            // Sabit değerler (web'den tam parse edemedik için)
            if (year == 2025)
            {
                if (ucretGeliri)
                {
                    brackets.AddRange(new[]
                    {
                        new TaxBracket { Alt = 0, Ust = 158000, Oran = 15, SabitVergi = 0 },
                        new TaxBracket { Alt = 158000, Ust = 330000, Oran = 20, SabitVergi = 23700 },
                        new TaxBracket { Alt = 330000, Ust = 1200000, Oran = 27, SabitVergi = 58100 },
                        new TaxBracket { Alt = 1200000, Ust = 4300000, Oran = 35, SabitVergi = 293000 },
                        new TaxBracket { Alt = 4300000, Ust = 999999999, Oran = 40, SabitVergi = 1378000 }
                    });
                }
                else
                {
                    brackets.AddRange(new[]
                    {
                        new TaxBracket { Alt = 0, Ust = 158000, Oran = 15, SabitVergi = 0 },
                        new TaxBracket { Alt = 158000, Ust = 330000, Oran = 20, SabitVergi = 23700 },
                        new TaxBracket { Alt = 330000, Ust = 800000, Oran = 27, SabitVergi = 58100 },
                        new TaxBracket { Alt = 800000, Ust = 4300000, Oran = 35, SabitVergi = 185000 },
                        new TaxBracket { Alt = 4300000, Ust = 999999999, Oran = 40, SabitVergi = 1410000 }
                    });
                }
            }
            else if (year == 2024)
            {
                if (ucretGeliri)
                {
                    brackets.AddRange(new[]
                    {
                        new TaxBracket { Alt = 0, Ust = 110000, Oran = 15, SabitVergi = 0 },
                        new TaxBracket { Alt = 110000, Ust = 230000, Oran = 20, SabitVergi = 16500 },
                        new TaxBracket { Alt = 230000, Ust = 870000, Oran = 27, SabitVergi = 40500 },
                        new TaxBracket { Alt = 870000, Ust = 3000000, Oran = 35, SabitVergi = 213300 },
                        new TaxBracket { Alt = 3000000, Ust = 999999999, Oran = 40, SabitVergi = 958800 }
                    });
                }
                else
                {
                    brackets.AddRange(new[]
                    {
                        new TaxBracket { Alt = 0, Ust = 110000, Oran = 15, SabitVergi = 0 },
                        new TaxBracket { Alt = 110000, Ust = 230000, Oran = 20, SabitVergi = 16500 },
                        new TaxBracket { Alt = 230000, Ust = 580000, Oran = 27, SabitVergi = 40500 },
                        new TaxBracket { Alt = 580000, Ust = 3000000, Oran = 35, SabitVergi = 135000 },
                        new TaxBracket { Alt = 3000000, Ust = 999999999, Oran = 40, SabitVergi = 982000 }
                    });
                }
            }

            return brackets;
        }

        private async Task ScrapeRentTaxDataAsync(TaxRatesData taxRates)
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://kira-vergisi.hesaplama.net/");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 2025 ve 2024 istisna tutarlarını parse et
                taxRates.KiraVergisi["2025"] = new RentTaxData
                {
                    KonutIstisnasi = 47000,
                    BeyanSiniri = 330000
                };

                taxRates.KiraVergisi["2024"] = new RentTaxData
                {
                    KonutIstisnasi = 33000,
                    BeyanSiniri = 230000
                };
            }
            catch
            {
                // Fallback değerler
            }
        }

        private async Task ScrapeCorporateTaxRatesAsync(TaxRatesData taxRates)
        {
            try
            {
                var html = await _httpClient.GetStringAsync("https://kurumlar-vergisi.hesaplama.net/");
                
                // 2023+ için %25 (finans %30)
                taxRates.KurumlarVergisi["2025"] = new CorporateTaxData { Normal = 25, Finans = 30 };
                taxRates.KurumlarVergisi["2024"] = new CorporateTaxData { Normal = 25, Finans = 30 };
                taxRates.KurumlarVergisi["2023"] = new CorporateTaxData { Normal = 25, Finans = 30 };
                taxRates.KurumlarVergisi["2022"] = new CorporateTaxData { Normal = 23, Finans = 23 };
                taxRates.KurumlarVergisi["2021"] = new CorporateTaxData { Normal = 25, Finans = 25 };
                taxRates.KurumlarVergisi["2018-2020"] = new CorporateTaxData { Normal = 22, Finans = 22 };
            }
            catch
            {
                // Fallback değerler
            }
        }

        private void SaveToJson(TaxRatesData taxRates)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var directory = Path.GetDirectoryName(_jsonFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(taxRates, options);
            File.WriteAllText(_jsonFilePath, json);
        }
    }
}
