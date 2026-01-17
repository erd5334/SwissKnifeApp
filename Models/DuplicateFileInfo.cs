using System;

namespace SwissKnifeApp.Models
{
    public class DuplicateFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string GroupKey { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
        public int DuplicateCount { get; set; }
    }

    public class DuplicateGroup
    {
        public string GroupKey { get; set; } = string.Empty;
        public string RepresentativeFile { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalWastedSpace { get; set; }
        public string TotalWastedSpaceFormatted { get; set; } = string.Empty;
        public System.Collections.ObjectModel.ObservableCollection<DuplicateFileInfo> Files { get; set; } = new();
    }
}
