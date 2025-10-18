using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;

namespace SwissKnifeApp.Services
{
    public class ColorPickerService
    {
        private readonly List<string> _codeLanguages = new()
        {
            "XAML Background", "XAML Foreground", "React (sx)", "React (inline style)", "XAML", "CSS", "C#", "HTML", "Python", "VB.NET", "Excel VBA", "NET MAUI", "Flutter", "Java", "Kotlin", "Swift", "Objective-C", "Android XML", "SCSS", "LESS", "Tailwind CSS", "QML", "Unity C#", "MATLAB", "Figma", "SVG", "Dart", "PowerShell", "Rust", "Go", "C++ (Qt)", "Delphi", "PHP", "Laravel"
        };

        public IReadOnlyList<string> CodeLanguages => _codeLanguages;

        public string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        public string ToRgb(Color color) => $"rgb({color.R}, {color.G}, {color.B})";
        public string ToArgb(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        public string GenerateCode(string language, Color color)
        {
            var hex = ToHex(color);
            var rgb = ToRgb(color);
            return language switch
            {
                "XAML Background" => $"Background=\"{hex}\"",
                "XAML Foreground" => $"Foreground=\"{hex}\"",
                "React (sx)" => $"sx={{ backgroundColor: '{hex}' }}",
                "React (inline style)" => $"style={{ backgroundColor: '{hex}' }}",
                "XAML" => $"<SolidColorBrush Color=\"{hex}\" />",
                "CSS" => $"background-color: {hex};",
                "C#" => $"Color color = (Color)ColorConverter.ConvertFromString(\"{hex}\");",
                "HTML" => $"<div style=\"{hex}\">\n  ...\n</div>",
                "Python" => $"color = '{hex}'",
                "VB.NET" => $"Dim color As Color = ColorTranslator.FromHtml(\"{hex}\")",
                "Excel VBA" => $"Range(\"A1\").Interior.Color = RGB({color.R}, {color.G}, {color.B})",
                "NET MAUI" => $"Color.FromArgb(\"{hex}\")",
                "Flutter" => $"Color(0xFF{hex[1..]})",
                "Java" => $"Color color = Color.decode(\"{hex}\");",
                "Kotlin" => $"val color = Color.parseColor(\"{hex}\")",
                "Swift" => $"let color = UIColor(hex: \"{hex}\")",
                "Objective-C" => $"UIColor *color = [UIColor colorWithHexString: @\"{hex}\"];",
                "Android XML" => $"<color name=\"custom\">{hex}</color>",
                "SCSS" => $"$color: {hex};",
                "LESS" => $"@color: {hex};",
                "Tailwind CSS" => $"bg-[{hex}]",
                "QML" => $"color: \"{hex}\"",
                "Unity C#" => $"Color color = new Color32({color.R}, {color.G}, {color.B}, 255);",
                "MATLAB" => $"color = [{color.R}/255, {color.G}/255, {color.B}/255];",
                "Figma" => $"HEX: {hex} | RGB: {color.R}, {color.G}, {color.B}",
                "SVG" => $"fill=\"{hex}\"",
                "Dart" => $"Color(0xFF{hex[1..]})",
                "PowerShell" => $"$color = '{hex}'",
                "Rust" => $"let color = \"{hex}\";",
                "Go" => $"color := \"{hex}\"",
                "C++ (Qt)" => $"QColor color(\"{hex}\");",
                "Delphi" => $"Color := StringToColor(\"{hex}\");",
                "PHP" => $"$color = '{hex}';",
                "Laravel" => $"$color = '{hex}';",
                _ => hex
            };
        }
    }
}
