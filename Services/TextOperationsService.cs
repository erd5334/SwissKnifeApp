using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SwissKnifeApp.Services
{
    public class TextOperationsService
    {
        private static readonly char[] WordSplitChars = new[] { ' ', '\n', '\r', '\t' };

        public string ToUpper(string? text, CultureInfo? culture = null)
            => (text ?? string.Empty).ToUpper(culture ?? CultureInfo.CurrentCulture);

        public string ToLower(string? text, CultureInfo? culture = null)
            => (text ?? string.Empty).ToLower(culture ?? CultureInfo.CurrentCulture);

        public string TrimSpaces(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return string.Join(" ", text.Split(WordSplitChars, StringSplitOptions.RemoveEmptyEntries));
        }

        public int CountChars(string? text) => (text ?? string.Empty).Length;

        public int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(WordSplitChars, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public int CountSentences(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public int CountParagraphs(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public string ToTitleCase(string? text, CultureInfo? culture = null)
        {
            var t = text ?? string.Empty;
            var ci = culture ?? CultureInfo.CurrentCulture;
            return ci.TextInfo.ToTitleCase(t.ToLower(ci));
        }

        public string ReverseText(string? text)
        {
            var t = text ?? string.Empty;
            return new string(t.Reverse().ToArray());
        }

        public string ReverseWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var words = text.Split(WordSplitChars, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Reverse());
        }

        public string EncodeBase64(string? text)
        {
            var t = text ?? string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(t));
        }

        public bool TryDecodeBase64(string? base64, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrWhiteSpace(base64)) return true;
            try
            {
                result = Encoding.UTF8.GetString(Convert.FromBase64String(base64!));
                return true;
            }
            catch
            {
                result = string.Empty;
                return false;
            }
        }

        public string GenerateLoremIpsum(int wordCount = 30, int? seed = null)
        {
            string[] loremWords = new[]
            {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "ut", "enim", "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi"
            };

            var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
            return string.Join(" ", Enumerable.Range(0, Math.Max(0, wordCount)).Select(_ => loremWords[rnd.Next(loremWords.Length)]));
        }
    }
}
