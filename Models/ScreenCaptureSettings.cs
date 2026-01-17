using System;

namespace SwissKnifeApp.Models
{
    public class ScreenCaptureSettings
    {
        public string SaveDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public string FileNameFormat { get; set; } = "Screenshot_{0:yyyyMMdd_HHmmss}.png";
        public bool AutoSave { get; set; } = true;
        public bool ShowPreview { get; set; } = true;
        public string ImageFormat { get; set; } = "PNG"; // PNG, JPG, BMP
        public int JpegQuality { get; set; } = 90;
        public bool IncludeCursor { get; set; } = false;
    }

    public class CaptureResult
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CapturedAt { get; set; }
        public string CaptureType { get; set; } = string.Empty; // FullScreen, Window, Region
    }
}
