using System;
using System.Collections.Generic;

namespace SwissKnifeApp.Services
{
    public class UnitConverterService
    {
        private static readonly Dictionary<string, List<string>> Units = new()
        {
            ["Uzunluk"] = new List<string> { "Metre", "Kilometre", "Santimetre", "Milimetre" },
            ["Ağırlık"] = new List<string> { "Gram", "Kilogram", "Ton" },
            ["Sıcaklık"] = new List<string> { "Celsius", "Fahrenheit", "Kelvin" }
        };

        public IReadOnlyList<string> GetUnits(string type)
        {
            if (Units.TryGetValue(type, out var list)) return list;
            return Array.Empty<string>();
        }

        public double Convert(string type, string from, string to, double value)
        {
            if (string.Equals(type, "Uzunluk", StringComparison.Ordinal))
            {
                // convert to meters
                double meters = from switch
                {
                    "Kilometre" => value * 1000.0,
                    "Santimetre" => value / 100.0,
                    "Milimetre" => value / 1000.0,
                    _ => value // Metre
                };
                return to switch
                {
                    "Kilometre" => meters / 1000.0,
                    "Santimetre" => meters * 100.0,
                    "Milimetre" => meters * 1000.0,
                    _ => meters // Metre
                };
            }

            if (string.Equals(type, "Ağırlık", StringComparison.Ordinal))
            {
                // convert to grams
                double grams = from switch
                {
                    "Kilogram" => value * 1000.0,
                    "Ton" => value * 1_000_000.0,
                    _ => value // Gram
                };
                return to switch
                {
                    "Kilogram" => grams / 1000.0,
                    "Ton" => grams / 1_000_000.0,
                    _ => grams // Gram
                };
            }

            if (string.Equals(type, "Sıcaklık", StringComparison.Ordinal))
            {
                // to Celsius
                double celsius = from switch
                {
                    "Fahrenheit" => (value - 32.0) * 5.0 / 9.0,
                    "Kelvin" => value - 273.15,
                    _ => value // Celsius
                };
                return to switch
                {
                    "Fahrenheit" => celsius * 9.0 / 5.0 + 32.0,
                    "Kelvin" => celsius + 273.15,
                    _ => celsius // Celsius
                };
            }

            return value;
        }

        // Time utilities
        public DateTime TimestampToDate(long seconds)
            => DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;

        public long DateToTimestamp(DateTime dateTime)
            => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

        public TimeSpan AbsoluteDifference(DateTime a, DateTime b)
            => a > b ? a - b : b - a;

        public (int Years, int Months, int Days, int TotalMonths, int TotalDays) CalculateAge(DateTime birth, DateTime now)
        {
            int years = now.Year - birth.Year;
            int months = now.Month - birth.Month;
            int days = now.Day - birth.Day;
            if (days < 0)
            {
                months--;
                var prevMonth = now.Month == 1 ? 12 : now.Month - 1;
                var prevYear = prevMonth == 12 ? now.Year - 1 : now.Year;
                days += DateTime.DaysInMonth(prevYear, prevMonth);
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }
            var totalMonths = years * 12 + months;
            var totalDays = (int)(now - birth).TotalDays;
            return (years, months, days, totalMonths, totalDays);
        }

        public (int Years, int Months, int Days, int TotalMonths, int TotalDays) CalculateAgeBetween(DateTime start, DateTime end)
        {
            if (end < start) throw new ArgumentException("End date must be after start date");
            int years = end.Year - start.Year;
            int months = end.Month - start.Month;
            int days = end.Day - start.Day;
            if (days < 0)
            {
                months--;
                var prevMonth = end.Month == 1 ? 12 : end.Month - 1;
                var prevYear = prevMonth == 12 ? end.Year - 1 : end.Year;
                days += DateTime.DaysInMonth(prevYear, prevMonth);
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }
            var totalMonths = years * 12 + months;
            var totalDays = (int)(end - start).TotalDays;
            return (years, months, days, totalMonths, totalDays);
        }

        public DateTime ConvertTimeZone(DateTime date, string sourceTimeZoneId, string targetTimeZoneId)
        {
            var src = TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId);
            var tgt = TimeZoneInfo.FindSystemTimeZoneById(targetTimeZoneId);
            var utc = TimeZoneInfo.ConvertTimeToUtc(date, src);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tgt);
        }
    }
}
