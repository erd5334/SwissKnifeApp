using System;
using System.Collections.Generic;

namespace SwissKnifeApp.Models
{
    public class TaxRatesData
    {
        public string Version { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public string Source { get; set; } = "";
        public Dictionary<string, YearlyIncomeTax> GelirVergisi { get; set; } = new();
        public KdvData Kdv { get; set; } = new();
        public Dictionary<string, RentTaxData> KiraVergisi { get; set; } = new();
        public Dictionary<string, CorporateTaxData> KurumlarVergisi { get; set; } = new();
        public StampTaxData DamgaVergisi { get; set; } = new();
        public Dictionary<string, CapitalGainTaxData> DegerArtisKazanci { get; set; } = new();
        public Dictionary<string, LuxuryHousingTaxData> DegerliKonutVergisi { get; set; } = new();
        public Dictionary<string, PropertyTaxData> EmlakVergisi { get; set; } = new();
        public VatWithholdingData KdvTevkifat { get; set; } = new();
        public Dictionary<string, RentWithholdingData> KiraStopaj { get; set; } = new();
        public Dictionary<string, AccommodationTaxData> KonaklamaVergisi { get; set; } = new();
        public Dictionary<string, MotorVehicleTaxData> MotorluTasitlarVergisi { get; set; } = new();
        public SpecialConsumptionTaxData OzelTuketimVergisi { get; set; } = new();
        public Dictionary<string, InheritanceTaxData> VerasetIntikal { get; set; } = new();
        public Dictionary<string, TaxDelayInterestData> VergiGecikme { get; set; } = new();
    }

    public class YearlyIncomeTax
    {
        public List<TaxBracket> Ucret { get; set; } = new();
        public List<TaxBracket> UcretDisi { get; set; } = new();
    }

    public class TaxBracket
    {
        public decimal Alt { get; set; }
        public decimal Ust { get; set; }
        public decimal Oran { get; set; }
        public decimal SabitVergi { get; set; }
    }

    public class KdvData
    {
        public List<int> Oranlar { get; set; } = new();
    }

    public class RentTaxData
    {
        public decimal KonutIstisnasi { get; set; }
        public decimal BeyanSiniri { get; set; }
    }

    public class CorporateTaxData
    {
        public decimal Normal { get; set; }
        public decimal Finans { get; set; }
    }

    public class StampTaxData
    {
        public decimal Genel { get; set; }
    }

    public class TaxCalculationResult
    {
        public decimal Matrah { get; set; }
        public decimal VergiTutari { get; set; }
        public decimal NetTutar { get; set; }
        public List<TaxBracketResult> Dilimler { get; set; } = new();
    }

    public class TaxBracketResult
    {
        public string Aciklama { get; set; } = "";
        public decimal Matrah { get; set; }
        public decimal Oran { get; set; }
        public decimal VergiTutari { get; set; }
    }

    // Değer Artış Kazancı Vergisi
    public class CapitalGainTaxData
    {
        public decimal GayrimenkulOran { get; set; }
        public decimal MenkulOran { get; set; }
    }

    // Değerli Konut Vergisi
    public class LuxuryHousingTaxData
    {
        public decimal Esik { get; set; }
        public List<TaxBracket> Oranlar { get; set; } = new();
    }

    // Emlak Vergisi
    public class PropertyTaxData
    {
        public decimal BinaOran { get; set; }
        public decimal AraziOran { get; set; }
    }

    // KDV Tevkifatı
    public class VatWithholdingData
    {
        public List<VatWithholdingRate> Oranlar { get; set; } = new();
    }

    public class VatWithholdingRate
    {
        public string Tanim { get; set; } = "";
        public decimal KdvOran { get; set; }
        public decimal TevkifatOran { get; set; }
    }

    // Kira Stopajı
    public class RentWithholdingData
    {
        public decimal Oran { get; set; }
        public decimal IstisnaSiniri { get; set; }
    }

    // Konaklama Vergisi
    public class AccommodationTaxData
    {
        public decimal Oran { get; set; }
    }

    // Motorlu Taşıtlar Vergisi
    public class MotorVehicleTaxData
    {
        public List<CarTaxBracket> Otomobil { get; set; } = new();
        public List<MotorcycleTaxBracket> Motosiklet { get; set; } = new();
    }

    public class CarTaxBracket
    {
        public int Alt { get; set; }
        public int Ust { get; set; }
        public decimal Yil1 { get; set; }
        public decimal Yil2 { get; set; }
        public decimal Yil3 { get; set; }
        public decimal Yil4 { get; set; }
        public decimal Yil5Plus { get; set; }
    }

    public class MotorcycleTaxBracket
    {
        public int Alt { get; set; }
        public int Ust { get; set; }
        public decimal Tutar { get; set; }
    }

    // ÖTV
    public class SpecialConsumptionTaxData
    {
        public List<FuelTaxRate> Akaryakit { get; set; } = new();
        public CigaretteTaxData Sigara { get; set; } = new();
        public List<AlcoholTaxRate> Alkol { get; set; } = new();
    }

    public class FuelTaxRate
    {
        public string Tanim { get; set; } = "";
        public decimal Oran { get; set; }
    }

    public class CigaretteTaxData
    {
        public decimal MaktuOran { get; set; }
        public decimal NisbiOran { get; set; }
    }

    public class AlcoholTaxRate
    {
        public string Tanim { get; set; } = "";
        public decimal Oran { get; set; }
    }

    // Veraset ve İntikal Vergisi
    public class InheritanceTaxData
    {
        public List<TaxBracket> Dilimler { get; set; } = new();
        public InheritanceTaxExemption Istisna { get; set; } = new();
    }

    public class InheritanceTaxExemption
    {
        public decimal EsVeCocuk { get; set; }
        public decimal Diger { get; set; }
    }

    // Vergi Gecikme Faizi
    public class TaxDelayInterestData
    {
        public decimal AylikOran { get; set; }
    }
}
