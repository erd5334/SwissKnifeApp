using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
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
        public float FontSize { get; set; } = 24f;
        public string ColorHex { get; set; } = "#FFFFFF";
        public TextPosition Position { get; set; } = TextPosition.TopLeft;
    }

    public class PhotoTransformOptions
    {
        public double Zoom { get; set; } = 1.0;
        public double OffsetX { get; set; } = 0.0;
        public double OffsetY { get; set; } = 0.0;
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
            int cornerRadiusPx = 0,
            IDictionary<int, PhotoTransformOptions>? transforms = null,
            TextOverlayOptions? overlay = null,
            string borderColorHex = "#FFFFFF") // Kenarlık rengini de parametre olarak alalım
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            if (photoPaths == null) throw new ArgumentNullException(nameof(photoPaths));

            // Renk formatlarını düzelt (#AARRGGBB -> #RRGGBBAA veya #RRGGBB)
            string bgColor = FixHexColor(backgroundColorHex);
            string borderColor = FixHexColor(borderColorHex);

            using var canvas = new Image<Rgba32>(size, size);
            
            try 
            {
                var bg = Color.ParseHex(bgColor);
                canvas.Mutate(ctx => ctx.Fill(bg));
            }
            catch 
            {
                canvas.Mutate(ctx => ctx.Fill(Color.White));
            }

            // Kenarlık rengini de DrawPhotosWithLayout'a geçirelim
            DrawPhotosWithLayout(canvas, template, size, borderWidthPx, cornerRadiusPx, photoPaths, transforms, borderColor);

            if (overlay != null && !string.IsNullOrWhiteSpace(overlay.Text))
            {
                DrawTextOnImage(canvas, overlay);
            }

            string ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg")
                canvas.SaveAsJpeg(fileName, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
            else
                canvas.SaveAsPng(fileName);
        }

        private string FixHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return "#FFFFFF";
            hex = hex.Trim().Replace("#", "");
            
            if (hex.Length == 8)
            {
                // AARRGGBB -> RRGGBBAA (WPF Alpha baştadır, ImageSharp'ta sondadır)
                string a = hex.Substring(0, 2);
                string r = hex.Substring(2, 2);
                string g = hex.Substring(4, 2);
                string b = hex.Substring(6, 2);
                return $"#{r}{g}{b}{a}";
            }
            return $"#{hex}";
        }

        private void DrawPhotosWithLayout(Image<Rgba32> canvas, string template, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            switch (template)
            {
                case "2H": DrawGrid(canvas, 1, 2, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "2V": DrawGrid(canvas, 2, 1, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "3": DrawCustom3(canvas, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "4": DrawGrid(canvas, 2, 2, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "4_1_3": DrawCustom4_1_3(canvas, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "5": DrawCustom5(canvas, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "6": DrawGrid(canvas, 2, 3, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "8": DrawCustom8(canvas, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "9": DrawGrid(canvas, 3, 3, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "12": DrawGrid(canvas, 3, 4, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                case "16": DrawGrid(canvas, 4, 4, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
                default: DrawGrid(canvas, 2, 2, size, borderWidth, cornerRadius, photos, transforms, borderColor); break;
            }
        }

        private void DrawGrid(Image<Rgba32> canvas, int rows, int cols, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            float cellWidth = (size - (cols + 1) * borderWidth) / (float)cols;
            float cellHeight = (size - (rows + 1) * borderWidth) / (float)rows;

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (index >= photos.Count) return;
                    float x = borderWidth + c * (cellWidth + borderWidth);
                    float y = borderWidth + r * (cellHeight + borderWidth);
                    DrawSinglePhoto(canvas, (int)x, (int)y, (int)cellWidth, (int)cellHeight, cornerRadius, photos[index], index, transforms, borderColor, borderWidth);
                    index++;
                }
            }
        }

        private void DrawCustom3(Image<Rgba32> canvas, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            float h = (size - 3 * borderWidth) / 2f;
            float w = (size - 3 * borderWidth) / 2f;
            if (photos.Count > 0) DrawSinglePhoto(canvas, borderWidth, borderWidth, (int)w, (int)h, cornerRadius, photos[0], 0, transforms, borderColor, borderWidth);
            if (photos.Count > 1) DrawSinglePhoto(canvas, (int)(2 * borderWidth + w), borderWidth, (int)w, (int)h, cornerRadius, photos[1], 1, transforms, borderColor, borderWidth);
            if (photos.Count > 2) DrawSinglePhoto(canvas, borderWidth, (int)(2 * borderWidth + h), (int)(size - 2 * borderWidth), (int)h, cornerRadius, photos[2], 2, transforms, borderColor, borderWidth);
        }

        private void DrawCustom4_1_3(Image<Rgba32> canvas, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            float topH = (size - 3 * borderWidth) * 2 / 3f;
            float botH = (size - 3 * borderWidth) / 3f;
            float botW = (size - 4 * borderWidth) / 3f;

            if (photos.Count > 0) DrawSinglePhoto(canvas, borderWidth, borderWidth, size - 2 * borderWidth, (int)topH, cornerRadius, photos[0], 0, transforms, borderColor, borderWidth);
            for (int i = 0; i < 3 && i + 1 < photos.Count; i++)
            {
                int x = (int)(borderWidth + i * (botW + borderWidth));
                int y = (int)(2 * borderWidth + topH);
                DrawSinglePhoto(canvas, x, y, (int)botW, (int)botH, cornerRadius, photos[i + 1], i + 1, transforms, borderColor, borderWidth);
            }
        }

        private void DrawCustom5(Image<Rgba32> canvas, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            float h = (size - 3 * borderWidth) / 2f;
            float topW = (size - 3 * borderWidth) / 2f;
            float botW = (size - 4 * borderWidth) / 3f;

            if (photos.Count > 0) DrawSinglePhoto(canvas, borderWidth, borderWidth, (int)topW, (int)h, cornerRadius, photos[0], 0, transforms, borderColor, borderWidth);
            if (photos.Count > 1) DrawSinglePhoto(canvas, (int)(2 * borderWidth + topW), borderWidth, (int)topW, (int)h, cornerRadius, photos[1], 1, transforms, borderColor, borderWidth);
            for (int i = 0; i < 3 && i + 2 < photos.Count; i++)
            {
                int x = (int)(borderWidth + i * (botW + borderWidth));
                int y = (int)(2 * borderWidth + h);
                DrawSinglePhoto(canvas, x, y, (int)botW, (int)h, cornerRadius, photos[i + 2], i + 2, transforms, borderColor, borderWidth);
            }
        }

        private void DrawCustom8(Image<Rgba32> canvas, int size, int borderWidth, int cornerRadius, IList<string> photos, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor)
        {
            float rowH = (size - 4 * borderWidth) / 3f;
            float topW = (size - 3 * borderWidth) / 2f;
            float botW = (size - 5 * borderWidth) / 4f;

            if (photos.Count > 0) DrawSinglePhoto(canvas, borderWidth, borderWidth, (int)topW, (int)rowH, cornerRadius, photos[0], 0, transforms, borderColor, borderWidth);
            if (photos.Count > 1) DrawSinglePhoto(canvas, (int)(2 * borderWidth + topW), borderWidth, (int)topW, (int)rowH, cornerRadius, photos[1], 1, transforms, borderColor, borderWidth);
            if (photos.Count > 2) DrawSinglePhoto(canvas, borderWidth, (int)(2 * borderWidth + rowH), (int)topW, (int)rowH, cornerRadius, photos[2], 2, transforms, borderColor, borderWidth);
            if (photos.Count > 3) DrawSinglePhoto(canvas, (int)(2 * borderWidth + topW), (int)(2 * borderWidth + rowH), (int)topW, (int)rowH, cornerRadius, photos[3], 3, transforms, borderColor, borderWidth);
            
            for (int i = 0; i < 4 && i + 4 < photos.Count; i++)
            {
                int x = (int)(borderWidth + i * (botW + borderWidth));
                int y = (int)(3 * borderWidth + 2 * rowH);
                DrawSinglePhoto(canvas, x, y, (int)botW, (int)rowH, cornerRadius, photos[i + 4], i + 4, transforms, borderColor, borderWidth);
            }
        }

        private void DrawSinglePhoto(Image<Rgba32> canvas, int x, int y, int width, int height, int cornerRadius, string photoPath, int index, IDictionary<int, PhotoTransformOptions>? transforms, string borderColor, int borderWidth)
        {
            if (!System.IO.File.Exists(photoPath)) return;

            try
            {
                using var photo = Image.Load<Rgba32>(photoPath);
                
                double zoom = 1.0;
                double offX = 0, offY = 0;
                if (transforms != null && transforms.TryGetValue(index, out var t))
                {
                    zoom = t.Zoom;
                    offX = t.OffsetX;
                    offY = t.OffsetY;
                }

                double scale = Math.Max((double)width / photo.Width, (double)height / photo.Height) * zoom;
                int targetW = (int)(photo.Width * scale);
                int targetH = (int)(photo.Height * scale);

                using var resizedPhoto = photo.Clone(ctx => ctx.Resize(targetW, targetH, KnownResamplers.Lanczos3));
                
                int panX = (int)((width - targetW) / 2.0 + (offX * width / 600.0));
                int panY = (int)((height - targetH) / 2.0 + (offY * height / 600.0));

                using var cellImg = new Image<Rgba32>(width, height);
                cellImg.Mutate(ctx => ctx.DrawImage(resizedPhoto, new Point(panX, panY), 1f));

                // Kenarlık Çizimi (Fotoğrafın etrafına değil, hücrenin içine çiziyoruz)
                if (borderWidth > 0)
                {
                    try {
                        var bColor = Color.ParseHex(borderColor);
                        cellImg.Mutate(ctx => ctx.Draw(bColor, borderWidth, new RectangularPolygon(0, 0, width, height)));
                    } catch { }
                }

                if (cornerRadius > 0)
                {
                    ApplyCornerRadius(cellImg, cornerRadius);
                }

                canvas.Mutate(ctx => ctx.DrawImage(cellImg, new Point(x, y), 1f));
            }
            catch { }
        }

        private void ApplyCornerRadius(Image<Rgba32> img, int radius)
        {
            img.Mutate(ctx => 
            {
                var size = img.Size;
                var roundedRectPath = BuildRoundedRect(0, 0, size.Width, size.Height, radius);
                var rect = new RectangularPolygon(0, 0, size.Width, size.Height);
                var mask = rect.Clip(roundedRectPath);

                ctx.SetGraphicsOptions(new GraphicsOptions { Antialias = true, AlphaCompositionMode = PixelAlphaCompositionMode.DestOut })
                   .Fill(Color.Black, mask);
            });
        }

        private IPath BuildRoundedRect(float x, float y, float width, float height, float radius)
        {
            radius = Math.Min(radius, Math.Min(width / 2, height / 2));
            var path = new PathBuilder();
            path.AddLine(x + radius, y, x + width - radius, y);
            path.AddArc(x + width - 2 * radius, y, 2 * radius, 2 * radius, 0, 270, 90);
            path.AddLine(x + width, y + radius, x + width, y + height - radius);
            path.AddArc(x + width - 2 * radius, y + height - 2 * radius, 2 * radius, 2 * radius, 0, 0, 90);
            path.AddLine(x + width - radius, y + height, x + radius, y + height);
            path.AddArc(x, y + height - 2 * radius, 2 * radius, 2 * radius, 0, 90, 90);
            path.AddLine(x, y + height - radius, x, y + radius);
            path.AddArc(x, y, 2 * radius, 2 * radius, 0, 180, 90);
            return path.Build();
        }

        private void DrawTextOnImage(Image<Rgba32> image, TextOverlayOptions overlay)
        {
            try
            {
                var textColor = Color.ParseHex(FixHexColor(overlay.ColorHex));
                var font = SystemFonts.CreateFont("Arial", overlay.FontSize, FontStyle.Bold);
                
                float margin = 40f; 
                float x = margin, y = margin;
                float w = image.Width;
                float h = image.Height;

                var textOptions = new RichTextOptions(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                switch (overlay.Position)
                {
                    case TextPosition.TopLeft: x = margin; y = margin; break;
                    case TextPosition.TopCenter: 
                        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                        x = w / 2; y = margin; break;
                    case TextPosition.TopRight: 
                        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                        x = w - margin; y = margin; break;
                    case TextPosition.BottomLeft: x = margin; y = h - margin - overlay.FontSize; break;
                    case TextPosition.BottomCenter: 
                        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                        x = w / 2; y = h - margin - overlay.FontSize; break;
                    case TextPosition.BottomRight: 
                        textOptions.HorizontalAlignment = HorizontalAlignment.Right;
                        x = w - margin; y = margin; break;
                    case TextPosition.Center: 
                        textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                        textOptions.VerticalAlignment = VerticalAlignment.Center;
                        x = w / 2; y = h / 2; break;
                }
                
                textOptions.Origin = new PointF(x, y);
                image.Mutate(ctx => ctx.DrawText(textOptions, overlay.Text, textColor));
            }
            catch { }
        }
    }
}
