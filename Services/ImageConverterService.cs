using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Svg;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISSize = SixLabors.ImageSharp.Size;
using ISResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using SDImage = System.Drawing.Image;
using System.Drawing.Imaging;

namespace SwissKnifeApp.Services
{
    public class ImageConversionOptions
    {
        public string Format { get; set; } = "JPG"; // JPG, PNG, BMP, GIF, WEBP, ICO, SVG
        public int Quality { get; set; } = 90; // 0-100
        public int Width { get; set; } = 0; // px
        public int Height { get; set; } = 0; // px
        public int ScalePercent { get; set; } = 100; // 1-100+
        public bool KeepAspect { get; set; } = true;
        public bool Grayscale { get; set; } = false;
        public bool Invert { get; set; } = false;
        public bool BlackWhite { get; set; } = false;
        public int Saturation { get; set; } = 0; // -100..+100
        public int Brightness { get; set; } = 0; // -100..+100
    }

    public class ImageConversionResult
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public string TargetExtension { get; set; } = ".jpg";
    }

    public class ImageConverterService
    {
        private static readonly string[] ImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".svg", ".ico" };
        public string[] SupportedExtensions => ImageExtensions;

        public async Task<ImageConversionResult?> ConvertAsync(string sourcePath, ImageConversionOptions options)
        {
            byte[]? sourceBytes = null;
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (ext == ".svg")
            {
                sourceBytes = await ConvertSvgToPng(sourcePath, options.Width, options.Height);
                if (sourceBytes == null) return null;
                return await ConvertFromBytesAsync(sourceBytes, options, originalIsSvg: true);
            }
            else
            {
                return await ConvertFromPathAsync(sourcePath, options);
            }
        }

        public async Task<ImageConversionResult?> ConvertFromPathAsync(string path, ImageConversionOptions options)
        {
            return await Task.Run(() => ConvertInternal(() => Image.Load<Rgba32>(path), options));
        }

        public async Task<ImageConversionResult?> ConvertFromBytesAsync(byte[] bytes, ImageConversionOptions options, bool originalIsSvg = false)
        {
            return await Task.Run(() => ConvertInternal(() => Image.Load<Rgba32>(bytes), options, originalIsSvg));
        }

        private ImageConversionResult? ConvertInternal(Func<Image<Rgba32>> load, ImageConversionOptions options, bool originalIsSvg = false)
        {
            using var image = load();

            // Resize
            if (options.ScalePercent != 100 && options.ScalePercent > 0)
            {
                var newW = Math.Max(1, image.Width * options.ScalePercent / 100);
                var newH = Math.Max(1, image.Height * options.ScalePercent / 100);
                image.Mutate(x => x.Resize(newW, newH));
            }
            else if (options.Width > 0 || options.Height > 0)
            {
                var targetW = options.Width > 0 ? options.Width : image.Width;
                var targetH = options.Height > 0 ? options.Height : image.Height;
                var resizeOptions = new ResizeOptions
                {
                    Size = new ISSize(targetW, targetH),
                    Mode = options.KeepAspect ? ISResizeMode.Max : ISResizeMode.Stretch
                };
                image.Mutate(x => x.Resize(resizeOptions));
            }

            // Filters
            image.Mutate(ctx =>
            {
                if (options.Grayscale) ctx.Grayscale();
                if (options.Invert) ctx.Invert();
                if (options.BlackWhite) ctx.BinaryThreshold(0.5f);
                if (options.Saturation != 0) ctx.Saturate(1f + options.Saturation / 100f);
                if (options.Brightness != 0) ctx.Brightness(1f + options.Brightness / 100f);
            });

            // Encode
            var result = new ImageConversionResult();
            string outExt = GetExtension(options.Format);

            if (options.Format == "ICO")
            {
                using var msPng = new MemoryStream();
                image.SaveAsPng(msPng);
                result.TargetExtension = ".ico";
                result.Bytes = ConvertToIco(msPng.ToArray());
                return result;
            }
            else if (options.Format == "SVG")
            {
                using var msSvg = new MemoryStream();
                image.SaveAsPng(msSvg);
                var base64 = Convert.ToBase64String(msSvg.ToArray());
                var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"" + image.Width + "\" height=\"" + image.Height + "\">" +
                                 "\n  <image width=\"" + image.Width + "\" height=\"" + image.Height + "\" xlink:href=\"data:image/png;base64," + base64 + "\"/>" +
                                 "\n</svg>";
                result.TargetExtension = ".svg";
                result.Bytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
                return result;
            }
            else
            {
                IImageEncoder encoder = options.Format switch
                {
                    "JPG" => new JpegEncoder { Quality = Math.Clamp(options.Quality, 0, 100) },
                    "PNG" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                    "BMP" => new BmpEncoder(),
                    "GIF" => new GifEncoder(),
                    "WEBP" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }, // placeholder
                    _ => new JpegEncoder { Quality = Math.Clamp(options.Quality, 0, 100) }
                };
                if (options.Format == "WEBP")
                {
                    outExt = ".png";
                }

                using var ms = new MemoryStream();
                image.Save(ms, encoder);
                result.TargetExtension = outExt;
                result.Bytes = ms.ToArray();
                return result;
            }
        }

        public static string GetExtension(string format) => format switch
        {
            "JPG" => ".jpg",
            "PNG" => ".png",
            "BMP" => ".bmp",
            "GIF" => ".gif",
            "WEBP" => ".webp",
            "ICO" => ".ico",
            "SVG" => ".svg",
            _ => ".jpg"
        };

        private static byte[] ConvertToIco(byte[] pngBytes)
        {
            try
            {
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);

                // ICO header
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)1);

                // Directory entry
                using var pngMs = new MemoryStream(pngBytes);
                using var img = SDImage.FromStream(pngMs);
                byte width = (byte)(img.Width >= 256 ? 0 : img.Width);
                byte height = (byte)(img.Height >= 256 ? 0 : img.Height);

                bw.Write(width);
                bw.Write(height);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
                bw.Write(pngBytes.Length);
                bw.Write(22);

                // PNG data
                bw.Write(pngBytes);
                return ms.ToArray();
            }
            catch
            {
                return pngBytes;
            }
        }

        private static async Task<byte[]?> ConvertSvgToPng(string svgPath, int width, int height)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var svgDoc = SvgDocument.Open(svgPath);
                    if (width > 0 && height > 0)
                    {
                        svgDoc.Width = width;
                        svgDoc.Height = height;
                    }
                    using var bitmap = svgDoc.Draw();
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}
