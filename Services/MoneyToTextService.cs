using System;
using HBMoneyToWords;
using HBMoneyToWords.Extensions;
using HBMoneyToWords.Models;

namespace SwissKnifeApp.Services
{
    public class MoneyToTextService
    {
        public enum Language
        {
            Turkish,
            English
        }

        public enum Casing
        {
            Upper = 0,
            Lower = 1,
            Title = 2
        }

        public Language ParseLanguage(string? languageText)
        {
            var lang = (languageText ?? "Türkçe").ToLowerInvariant();
            return lang.Contains("türk") ? Language.Turkish : Language.English;
        }

        public Casing ParseCasingIndex(int? index)
        {
            return index == 0 ? Casing.Upper : index == 2 ? Casing.Title : Casing.Lower;
        }

        public string Convert(
            decimal amount,
            Language language,
            Casing casing,
            bool noSpaces,
            string? separator,
            bool firstLetterUpper)
        {
            try
            {
                // 1) Önce yalnızca harf formatını uygulayarak metni üret
                string text;
                if (language == Language.Turkish)
                {
                    text = casing switch
                    {
                        Casing.Upper => amount.ToWordsTurkish(options: FormatOptions.UpperCase),
                        Casing.Title => amount.ToWordsTurkish(options: FormatOptions.TitleCase),
                        _ => amount.ToWordsTurkish()
                    };
                }
                else
                {
                    text = casing switch
                    {
                        Casing.Upper => amount.ToWordsEnglish(options: FormatOptions.UpperCase),
                        Casing.Title => amount.ToWordsEnglish(options: FormatOptions.TitleCase),
                        _ => amount.ToWordsEnglish()
                    };
                }

                // 2) Küçük harf modunda "İlk harf büyük" opsiyonu
                if (casing == Casing.Lower && firstLetterUpper && !string.IsNullOrEmpty(text))
                {
                    var culture = language == Language.Turkish
                        ? new System.Globalization.CultureInfo("tr-TR")
                        : System.Globalization.CultureInfo.InvariantCulture;
                    var first = char.ToUpper(text[0], culture);
                    text = first + text.Substring(1);
                }

                // 3) Boşluksuz veya özel ayırıcı işlemleri (harf formatından bağımsız uygulanır)
                if (noSpaces)
                {
                    text = text.Replace(" ", string.Empty);
                }
                else if (!string.IsNullOrWhiteSpace(separator) && separator != " ")
                {
                    text = text.Replace(" ", separator!);
                }

                return text;
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }
    }
}
