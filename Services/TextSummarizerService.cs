using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SwissKnifeApp.Services
{
    public class TextSummarizerService
    {
        public List<string> GetSentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();
            return sentences;
        }

        public List<string> GetWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            var words = Regex.Split(text, @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
                .ToList();
            return words;
        }

        public string SummarizeText(string text, double ratio, string language)
        {
            var sentences = GetSentences(text);
            if (sentences.Count == 0) return string.Empty;

            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);

            var sentenceScores = new Dictionary<string, double>();
            foreach (var sentence in sentences)
            {
                var sentenceWords = GetWords(sentence);
                double score = 0;
                foreach (var word in sentenceWords)
                {
                    var normalizedWord = NormalizeWord(word, language);
                    if (wordFreq.ContainsKey(normalizedWord))
                        score += wordFreq[normalizedWord];
                }
                if (sentenceWords.Count > 0)
                    score /= sentenceWords.Count;
                sentenceScores[sentence] = score;
            }

            var targetCount = Math.Max(1, (int)(sentences.Count * ratio));
            var selected = sentenceScores
                .OrderByDescending(x => x.Value)
                .Take(targetCount)
                .Select(x => x.Key)
                .ToHashSet();

            var summary = sentences.Where(s => selected.Contains(s));
            return string.Join(" ", summary);
        }

        public Dictionary<string, double> FindKeywords(string text, int count, string language)
        {
            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);
            return wordFreq
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<string, double> FindImportantSentences(string text, int count, string language)
        {
            var sentences = GetSentences(text);
            var words = GetWords(text);
            var wordFreq = CalculateWordFrequency(words, language);
            var sentenceScores = new Dictionary<string, double>();

            foreach (var sentence in sentences)
            {
                var sentenceWords = GetWords(sentence);
                double score = 0;
                foreach (var word in sentenceWords)
                {
                    var normalizedWord = NormalizeWord(word, language);
                    if (wordFreq.ContainsKey(normalizedWord))
                        score += wordFreq[normalizedWord];
                }
                var position = sentences.IndexOf(sentence);
                if (position < sentences.Count * 0.1)
                    score *= 1.2; // position bonus
                if (sentenceWords.Count > 0)
                    score /= sentenceWords.Count;
                sentenceScores[sentence] = score;
            }

            return sentenceScores
                .OrderByDescending(x => x.Value)
                .Take(count)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<string, double> CalculateWordFrequency(List<string> words, string language)
        {
            var stopWords = GetStopWords(language);
            var wordFreq = new Dictionary<string, double>();
            foreach (var word in words)
            {
                var normalized = NormalizeWord(word, language);
                if (stopWords.Contains(normalized))
                    continue;
                if (!wordFreq.ContainsKey(normalized))
                    wordFreq[normalized] = 0;
                wordFreq[normalized]++;
            }
            if (wordFreq.Count == 0) return wordFreq;
            var maxFreq = wordFreq.Values.Max();
            return wordFreq.ToDictionary(kv => kv.Key, kv => kv.Value / maxFreq);
        }

        public string NormalizeWord(string word, string language)
        {
            var normalized = (word ?? string.Empty).ToLowerInvariant();
            if (language == "tr")
            {
                normalized = normalized
                    .Replace('ı', 'i')
                    .Replace('ğ', 'g')
                    .Replace('ü', 'u')
                    .Replace('ş', 's')
                    .Replace('ö', 'o')
                    .Replace('ç', 'c');
            }
            return normalized;
        }

        public HashSet<string> GetStopWords(string language)
        {
            if (language == "tr")
            {
                return new HashSet<string>
                {
                    "bir", "ve", "veya", "ancak", "fakat", "çünkü", "için", "ile", "bu", "şu", "o",
                    "ben", "sen", "biz", "siz", "onlar", "şey", "var", "yok", "gibi", "kadar",
                    "daha", "en", "çok", "az", "şimdi", "sonra", "önce", "burada", "orada",
                    "her", "bazı", "hiç", "de", "da", "mi", "mı", "mu", "mü", "ki", "ise",
                    "olan", "olarak", "ama", "sadece", "bile", "artık", "hala", "dahi",
                    "ne", "nasıl", "neden", "niçin", "nerede", "kim", "kime", "ne zaman"
                };
            }
            return new HashSet<string>
            {
                "the", "a", "an", "and", "or", "but", "if", "then", "else", "when",
                "at", "from", "by", "on", "off", "for", "in", "out", "over", "to",
                "into", "with", "is", "are", "was", "were", "be", "been", "being",
                "have", "has", "had", "do", "does", "did", "will", "would", "should",
                "could", "may", "might", "must", "can", "this", "that", "these", "those",
                "i", "you", "he", "she", "it", "we", "they", "what", "which", "who",
                "where", "when", "why", "how", "all", "each", "every", "both", "few",
                "more", "most", "other", "some", "such", "no", "nor", "not", "only",
                "own", "same", "so", "than", "too", "very", "just", "as"
            };
        }
    }
}
