using System;

namespace SwissKnifeApp.Models
{
    public enum RenameMode
    {
        Sequential = 0,
        DateTime = 1,
        Replace = 2,
        CustomTemplate = 3
    }

    public class RenameOptions
    {
        public RenameMode Mode { get; set; }
        public string BaseName { get; set; } = "file";
        public int StartNumber { get; set; } = 1;
        public int Digits { get; set; } = 3;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public bool DatePrefix { get; set; } = true;
        public DateTime DateNow { get; set; } = DateTime.Now;
        public string? SearchText { get; set; }
        public string? ReplaceText { get; set; }
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public string? Template { get; set; }
    }

    public class DiffStats
    {
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Changed { get; set; }
        public int WordDiffCount { get; set; }
        public double Similarity { get; set; }
        public int CharDiffCount { get; set; }
    }
}
