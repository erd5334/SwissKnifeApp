using System;
using System.IO;
using System.Text.Json;
using SwissKnifeApp.Models;

namespace SwissKnifeApp
{
    public class TestJsonLoad
    {
        public static void TestLoad()
        {
            try
            {
                var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "tax-rates.json");
                Console.WriteLine($"JSON Path: {jsonPath}");
                Console.WriteLine($"File Exists: {File.Exists(jsonPath)}");

                if (File.Exists(jsonPath))
                {
                    var json = File.ReadAllText(jsonPath);
                    Console.WriteLine($"JSON Length: {json.Length}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var taxRates = JsonSerializer.Deserialize<TaxRatesData>(json, options);

                    if (taxRates == null)
                    {
                        Console.WriteLine("taxRates is NULL after deserialization!");
                    }
                    else
                    {
                        Console.WriteLine($"Version: {taxRates.Version}");
                        Console.WriteLine($"KdvTevkifat is null: {taxRates.KdvTevkifat == null}");
                        
                        if (taxRates.KdvTevkifat != null)
                        {
                            Console.WriteLine($"KdvTevkifat.Oranlar is null: {taxRates.KdvTevkifat.Oranlar == null}");
                            Console.WriteLine($"KdvTevkifat.Oranlar Count: {taxRates.KdvTevkifat.Oranlar?.Count ?? 0}");
                            
                            if (taxRates.KdvTevkifat.Oranlar != null)
                            {
                                foreach (var oran in taxRates.KdvTevkifat.Oranlar)
                                {
                                    Console.WriteLine($"  - {oran.Tanim}: KDV {oran.KdvOran}%, Tevkifat {oran.TevkifatOran}%");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}
