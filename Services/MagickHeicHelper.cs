using System;
using System.IO;
using ImageMagick;

namespace SwissKnifeApp.Services
{
    public static class MagickHeicHelper
    {
        /// <summary>
        /// HEIC/HEIF dosyasını PNG byte dizisine dönüştürür.
        /// </summary>
        /// <param name="heicPath">Kaynak HEIC/HEIF dosya yolu</param>
        /// <returns>PNG byte dizisi veya null</returns>
        public static byte[]? ConvertHeicToPngBytes(string heicPath)
        {
            try
            {
                using var image = new MagickImage(heicPath);
                using var ms = new MemoryStream();
                image.Format = MagickFormat.Png;
                image.Write(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}// This file is required for Magick.NET integration.
// It will be used to load HEIC/HEIF images when ImageSharp cannot handle them.
