using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SwissKnifeApp.Services
{
    public enum TextPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center
    }

    public class TextOverlayOptions
    {
        public string Text { get; set; } = string.Empty;
        public float FontSize { get; set; } = 24f; // scaled outside
        public string ColorHex { get; set; } = "#FFFFFF";
        public TextPosition Position { get; set; } = TextPosition.TopLeft;
    }

    public class PhotoCollageService
    {
        public void CreateCollage(
            string fileName,
            int size,
            IList<string> photoPaths,
            string template,
            string backgroundColorHex,
            int borderWidthPx,
            TextOverlayOptions? overlay = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (photoPaths == null) throw new ArgumentNullException(nameof(photoPaths));
            if (string.IsNullOrWhiteSpace(template)) template = "4";
            if (string.IsNullOrWhiteSpace(backgroundColorHex)) backgroundColorHex = "#FFFFFF";

            using var image = new Image<Rgba32>(size, size);
            image.Mutate(ctx => ctx.Fill(Rgba32.ParseHex(backgroundColorHex)));

            DrawPhotosOnImage(image, template, size, borderWidthPx, photoPaths);

            if (overlay != null && !string.IsNullOrWhiteSpace(overlay.Text))
            {
                DrawTextOnImage(image, overlay);
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg")
                image.SaveAsJpeg(fileName);
            else
                image.SaveAsPng(fileName);
        }

        private void DrawPhotosOnImage(Image<Rgba32> canvas, string template, int size, int borderWidth, IList<string> photos)
        {
            switch (template)
            {
                case "2H":
                    DrawGridPhotos(canvas, 1, 2, size, borderWidth, photos);
                    break;
                case "2V":
                    DrawGridPhotos(canvas, 2, 1, size, borderWidth, photos);
                    break;
                case "3":
                    DrawCustom3Photos(canvas, size, borderWidth, photos);
                    break;
                case "4":
                    DrawGridPhotos(canvas, 2, 2, size, borderWidth, photos);
                    break;
                case "6":
                    DrawGridPhotos(canvas, 2, 3, size, borderWidth, photos);
                    break;
                case "9":
                    DrawGridPhotos(canvas, 3, 3, size, borderWidth, photos);
                    break;
                // Diğer şablonlar UI önizlemede destekli; ihtiyaç olursa buraya da eklenebilir
                default:
                    DrawGridPhotos(canvas, 2, 2, size, borderWidth, photos);
                    break;
            }
        }

        private void DrawGridPhotos(Image<Rgba32> canvas, int rows, int cols, int size, int borderWidth, IList<string> photos)
        {
            int cellWidth = (size - (cols + 1) * borderWidth) / cols;
            int cellHeight = (size - (rows + 1) * borderWidth) / rows;

            int photoIndex = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (photoIndex >= photos.Count) return;

                    int x = borderWidth + col * (cellWidth + borderWidth);
                    int y = borderWidth + row * (cellHeight + borderWidth);

                    DrawSinglePhoto(canvas, x, y, cellWidth, cellHeight, photos[photoIndex]);
                    photoIndex++;
                }
            }
        }

        private void DrawCustom3Photos(Image<Rgba32> canvas, int size, int borderWidth, IList<string> photos)
        {
            int topHeight = (size - 3 * borderWidth) / 2;
            int topWidth = (size - 3 * borderWidth) / 2;
            int bottomHeight = topHeight;
            int bottomWidth = size - 2 * borderWidth;

            if (photos.Count > 0)
                DrawSinglePhoto(canvas, borderWidth, borderWidth, topWidth, topHeight, photos[0]);
            if (photos.Count > 1)
                DrawSinglePhoto(canvas, 2 * borderWidth + topWidth, borderWidth, topWidth, topHeight, photos[1]);
            if (photos.Count > 2)
                DrawSinglePhoto(canvas, borderWidth, 2 * borderWidth + topHeight, bottomWidth, bottomHeight, photos[2]);
        }

        private void DrawSinglePhoto(Image<Rgba32> canvas, int x, int y, int width, int height, string photoPath)
        {
            try
            {
                using var photo = SixLabors.ImageSharp.Image.Load<Rgba32>(photoPath);
                photo.Mutate(ctx => ctx.Resize(width, height, KnownResamplers.Lanczos3));
                canvas.Mutate(ctx => ctx.DrawImage(photo, new SixLabors.ImageSharp.Point(x, y), 1f));
            }
            catch
            {
                // Foto okunamazsa sessiz geç
            }
        }

        private void DrawTextOnImage(Image<Rgba32> image, TextOverlayOptions overlay)
        {
            var textColor = Rgba32.ParseHex(overlay.ColorHex);
            float margin = 20f; // ölçeksiz, fontSize zaten ölçekli geldiğini varsayıyoruz

            float x = margin, y = margin;
            float size = image.Width; // kare

            switch (overlay.Position)
            {
                case TextPosition.TopLeft:
                    x = margin; y = margin; break;
                case TextPosition.TopCenter:
                    x = size / 2f; y = margin; break;
                case TextPosition.TopRight:
                    x = size - margin; y = margin; break;
                case TextPosition.BottomLeft:
                    x = margin; y = size - margin; break;
                case TextPosition.BottomCenter:
                    x = size / 2f; y = size - margin; break;
                case TextPosition.BottomRight:
                    x = size - margin; y = size - margin; break;
                case TextPosition.Center:
                    x = size / 2f; y = size / 2f; break;
            }

            try
            {
                var font = SystemFonts.CreateFont("Arial", overlay.FontSize, FontStyle.Bold);
                image.Mutate(ctx =>
                {
                    ctx.DrawText(overlay.Text, font, textColor, new PointF(x, y));
                });
            }
            catch
            {
                // Yazı tipi bulunamazsa sessiz geç
            }
        }
    }
}
