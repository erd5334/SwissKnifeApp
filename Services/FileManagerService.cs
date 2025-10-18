using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SwissKnifeApp.Models;
using SwissKnifeApp.Views.Modules;

namespace SwissKnifeApp.Services
{
    public class FileManagerService
    {
        // ===== Encryption/Decryption (compatible with existing implementation) =====
        private static readonly byte[] StaticSalt = System.Text.Encoding.UTF8.GetBytes("SwissKnifeSalt2025");
        private const int Iterations = 10000; // Keep same as page logic for compatibility

        public void EncryptFile(string inputPath, string outputPath, string password)
        {
            using var aes = Aes.Create();
            using var kdf = new Rfc2898DeriveBytes(password, StaticSalt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = kdf.GetBytes(32);
            aes.IV = kdf.GetBytes(16);

            using var fsInput = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var fsOutput = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write);
            fsInput.CopyTo(cs);
        }

        public void DecryptFile(string inputPath, string outputPath, string password)
        {
            using var aes = Aes.Create();
            using var kdf = new Rfc2898DeriveBytes(password, StaticSalt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = kdf.GetBytes(32);
            aes.IV = kdf.GetBytes(16);

            using var fsInput = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var fsOutput = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cs.CopyTo(fsOutput);
        }

        // ===== Diff/Compare =====
        public (string left, string right, int added, int removed, int changed) CompareLineByLine(string text1, string text2, bool ignoreWhitespace, bool ignoreCase)
        {
            if (ignoreWhitespace)
            {
                text1 = Regex.Replace(text1, @"\s+", " ").Trim();
                text2 = Regex.Replace(text2, @"\s+", " ").Trim();
            }
            if (ignoreCase)
            {
                text1 = text1.ToLowerInvariant();
                text2 = text2.ToLowerInvariant();
            }

            var lines1 = text1.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lines2 = text2.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var leftResult = new List<string>();
            var rightResult = new List<string>();

            int added = 0, removed = 0, changed = 0;
            int maxLines = Math.Max(lines1.Length, lines2.Length);

            for (int i = 0; i < maxLines; i++)
            {
                var line1 = i < lines1.Length ? lines1[i] : "";
                var line2 = i < lines2.Length ? lines2[i] : "";

                if (line1 == line2)
                {
                    leftResult.Add($"  {line1}");
                    rightResult.Add($"  {line2}");
                }
                else if (string.IsNullOrEmpty(line1))
                {
                    leftResult.Add("");
                    rightResult.Add($"+ {line2}");
                    added++;
                }
                else if (string.IsNullOrEmpty(line2))
                {
                    leftResult.Add($"- {line1}");
                    rightResult.Add("");
                    removed++;
                }
                else
                {
                    leftResult.Add($"~ {line1}");
                    rightResult.Add($"~ {line2}");
                    changed++;
                }
            }

            return (string.Join("\n", leftResult), string.Join("\n", rightResult), added, removed, changed);
        }

        public (string left, string right, int diffCount) CompareWordByWord(string text1, string text2, bool ignoreWhitespace, bool ignoreCase)
        {
            if (ignoreWhitespace)
            {
                text1 = Regex.Replace(text1, @"\s+", " ").Trim();
                text2 = Regex.Replace(text2, @"\s+", " ").Trim();
            }
            if (ignoreCase)
            {
                text1 = text1.ToLowerInvariant();
                text2 = text2.ToLowerInvariant();
            }

            var words1 = text1.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var leftWords = new List<string>();
            var rightWords = new List<string>();
            int diffCount = 0;

            int maxWords = Math.Max(words1.Length, words2.Length);
            for (int i = 0; i < maxWords; i++)
            {
                var word1 = i < words1.Length ? words1[i] : "";
                var word2 = i < words2.Length ? words2[i] : "";

                if (word1 == word2)
                {
                    leftWords.Add(word1);
                    rightWords.Add(word2);
                }
                else
                {
                    leftWords.Add($"[{word1}]");
                    rightWords.Add($"[{word2}]");
                    diffCount++;
                }
            }

            return (string.Join(" ", leftWords), string.Join(" ", rightWords), diffCount);
        }

        public (double similarity, int diffCount) CompareCharByChar(string text1, string text2)
        {
            int diffCount = 0;
            int maxLength = Math.Max(text1.Length, text2.Length);

            for (int i = 0; i < maxLength; i++)
            {
                var char1 = i < text1.Length ? text1[i] : '\0';
                var char2 = i < text2.Length ? text2[i] : '\0';

                if (char1 != char2)
                    diffCount++;
            }

            double similarity = maxLength > 0 ? ((maxLength - diffCount) / (double)maxLength) * 100 : 100;
            return (similarity, diffCount);
        }

        // ===== Batch Rename =====
        public (int success, int error) ApplyRenameRules(IList<FileRenameItem> items, RenameOptions options, bool preview)
        {
            int counter = 0;
            int successCount = 0;
            int errorCount = 0;

            foreach (var file in items)
            {
                string newName = file.OriginalName;
                try
                {
                    switch (options.Mode)
                    {
                        case RenameMode.Sequential:
                            var num = (options.StartNumber + counter).ToString($"D{options.Digits}");
                            newName = $"{options.BaseName}_{num}{file.Extension}";
                            counter++;
                            break;

                        case RenameMode.DateTime:
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.OriginalName);
                            newName = options.DatePrefix
                                ? $"{options.DateNow.ToString(options.DateFormat)}_{nameWithoutExt}{file.Extension}"
                                : $"{nameWithoutExt}_{options.DateNow.ToString(options.DateFormat)}{file.Extension}";
                            break;

                        case RenameMode.Replace:
                            if (!string.IsNullOrEmpty(options.SearchText))
                            {
                                if (options.UseRegex)
                                {
                                    newName = Regex.Replace(file.OriginalName, options.SearchText, options.ReplaceText ?? "");
                                }
                                else
                                {
                                    var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                                    newName = file.OriginalName.Replace(options.SearchText, options.ReplaceText ?? "", comparison);
                                }
                            }
                            break;

                        case RenameMode.CustomTemplate:
                            var originalNameNoExt = Path.GetFileNameWithoutExtension(file.OriginalName);
                            newName = (options.Template ?? "{name}_{date}_{n}")
                                .Replace("{name}", originalNameNoExt)
                                .Replace("{date}", options.DateNow.ToString("yyyy-MM-dd"))
                                .Replace("{n}", counter.ToString("D3"))
                                .Replace("{ext}", file.Extension.TrimStart('.'));
                            newName += file.Extension;
                            counter++;
                            break;
                    }

                    foreach (var c in Path.GetInvalidFileNameChars())
                        newName = newName.Replace(c, '_');

                    file.NewName = newName;

                    if (!preview)
                    {
                        var directory = Path.GetDirectoryName(file.FullPath) ?? "";
                        var newPath = Path.Combine(directory, newName);
                        if (File.Exists(newPath))
                        {
                            errorCount++;
                            continue;
                        }
                        File.Move(file.FullPath, newPath);
                        file.FullPath = newPath;
                        file.OriginalName = newName;
                        successCount++;
                    }
                }
                catch
                {
                    errorCount++;
                }
            }

            return (successCount, errorCount);
        }
    }
}
