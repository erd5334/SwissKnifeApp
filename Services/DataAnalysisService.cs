using Newtonsoft.Json;
using OfficeOpenXml;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace SwissKnifeApp.Services
{
    public class DataAnalysisService
    {
        public DataAnalysisService()
        {
            // EPPlus license context
            Environment.SetEnvironmentVariable("EPPLUS_LICENSE_CONTEXT", "NonCommercial", EnvironmentVariableTarget.Process);
        }

        #region File Reading

        public DataTable ReadDataFile(string filePath)
        {
            if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return ReadCsv(filePath);
            else if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return ReadExcel(filePath);
            else if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return ReadJson(filePath);
            
            throw new NotSupportedException("Desteklenmeyen dosya formatı");
        }

        private DataTable ReadCsv(string path)
        {
            var dt = new DataTable();
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return dt;

            var headers = lines[0].Split(',');
            foreach (var h in headers)
                dt.Columns.Add(h.Trim());

            foreach (var line in lines.Skip(1))
                dt.Rows.Add(line.Split(','));

            return dt;
        }

        private DataTable ReadExcel(string path)
        {
            var dt = new DataTable();
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets[0];
            var headerRange = ws.Cells[1, 1, 1, ws.Dimension.End.Column];
            foreach (var cell in headerRange)
                dt.Columns.Add(cell.Text);

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                var values = new List<string>();
                for (int col = 1; col <= ws.Dimension.End.Column; col++)
                    values.Add(ws.Cells[row, col].Text);
                dt.Rows.Add(values.ToArray());
            }
            return dt;
        }

        private DataTable ReadJson(string path)
        {
            var dt = new DataTable();
            string json = File.ReadAllText(path);
            var arr = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            if (arr == null || arr.Count == 0) return dt;

            var keys = arr.First().Keys.ToList();
            foreach (var key in keys)
                dt.Columns.Add(key, typeof(object));

            foreach (var dict in arr)
            {
                var row = new object[keys.Count];
                for (int i = 0; i < keys.Count; i++)
                {
                    dict.TryGetValue(keys[i], out var value);
                    row[i] = value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        #endregion

        #region Data Filtering

        public string BuildFilterExpression(DataTable dataTable, string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
                return "";

            var sb = new StringBuilder();
            foreach (DataColumn col in dataTable.Columns)
            {
                if (sb.Length > 0) sb.Append(" OR ");
                sb.AppendFormat("[{0}] LIKE '%{1}%'", col.ColumnName, filterText.Replace("'", "''"));
            }
            return sb.ToString();
        }

        #endregion

        #region Statistical Analysis

        public class StatisticsResult
        {
            public string ColumnName { get; set; } = "";
            public double Average { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double StandardDeviation { get; set; }
            public int Count { get; set; }
        }

        public StatisticsResult? CalculateStatistics(DataTable dataTable, string columnName)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0)
                return null;

            double avg = values.Average();
            double min = values.Min();
            double max = values.Max();
            double sd = Math.Sqrt(values.Sum(v => Math.Pow(v - avg, 2)) / values.Count);

            return new StatisticsResult
            {
                ColumnName = columnName,
                Average = avg,
                Min = min,
                Max = max,
                StandardDeviation = sd,
                Count = values.Count
            };
        }

        public string GenerateCorrelationMatrix(DataTable dataTable)
        {
            var numericCols = dataTable.Columns.Cast<DataColumn>()
                .Where(c => dataTable.AsEnumerable()
                    .All(r => double.TryParse(r[c].ToString(), out _) || string.IsNullOrEmpty(r[c].ToString())))
                .ToList();

            if (numericCols.Count < 2)
                return "Korelasyon analizi için en az 2 sayısal sütun gerekir.";

            var corrText = new StringBuilder("📊 Korelasyon Matrisi:\n");
            foreach (var c1 in numericCols)
            {
                foreach (var c2 in numericCols)
                {
                    double corr = CalculatePearsonCorrelation(dataTable, c1, c2);
                    corrText.Append($"{c1.ColumnName}↔{c2.ColumnName}: {corr:F2}\n");
                }
            }
            return corrText.ToString();
        }

        public double CalculatePearsonCorrelation(DataTable dataTable, DataColumn c1, DataColumn c2)
        {
            var vals1 = dataTable.AsEnumerable()
                .Select(r => r[c1].ToString())
                .Where(s => double.TryParse(s, out _))
                .Select(s => double.Parse(s!)).ToArray();

            var vals2 = dataTable.AsEnumerable()
                .Select(r => r[c2].ToString())
                .Where(s => double.TryParse(s, out _))
                .Select(s => double.Parse(s!)).ToArray();

            int n = Math.Min(vals1.Length, vals2.Length);
            if (n == 0) return 0;

            double avg1 = vals1.Average();
            double avg2 = vals2.Average();

            double numerator = 0, denom1 = 0, denom2 = 0;
            for (int i = 0; i < n; i++)
            {
                double d1 = vals1[i] - avg1;
                double d2 = vals2[i] - avg2;
                numerator += d1 * d2;
                denom1 += d1 * d1;
                denom2 += d2 * d2;
            }

            return numerator / Math.Sqrt(denom1 * denom2);
        }

        #endregion

        #region Histogram Analysis

        public class HistogramResult
        {
            public int[] Bins { get; set; } = Array.Empty<int>();
            public double BinSize { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double Average { get; set; }
            public int DataCount { get; set; }
        }

        public HistogramResult? CalculateHistogram(DataTable dataTable, string columnName, int binCount = 10)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0)
                return null;

            double min = values.Min();
            double max = values.Max();
            double range = Math.Max(1e-9, max - min);
            double binSize = range / binCount;
            var bins = new int[binCount];

            foreach (var v in values)
            {
                int idx = (int)((v - min) / binSize);
                if (idx >= binCount) idx = binCount - 1;
                bins[idx]++;
            }

            return new HistogramResult
            {
                Bins = bins,
                BinSize = binSize,
                Min = min,
                Max = max,
                Average = values.Average(),
                DataCount = values.Count
            };
        }

        #endregion

        #region Regression Analysis

        public class RegressionResult
        {
            public double Slope { get; set; }
            public double Intercept { get; set; }
            public double RSquared { get; set; }
            public List<double> XValues { get; set; } = new();
            public List<double> YValues { get; set; } = new();
        }

        public RegressionResult? CalculateLinearRegression(DataTable dataTable, string xColumnName, string yColumnName)
        {
            var xVals = dataTable.AsEnumerable()
                .Select(r => r[xColumnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            var yVals = dataTable.AsEnumerable()
                .Select(r => r[yColumnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            int n = Math.Min(xVals.Count, yVals.Count);
            if (n < 2)
                return null;

            double avgX = xVals.Average();
            double avgY = yVals.Average();

            double covXY = 0, varX = 0;
            for (int i = 0; i < n; i++)
            {
                covXY += (xVals[i] - avgX) * (yVals[i] - avgY);
                varX += Math.Pow(xVals[i] - avgX, 2);
            }

            double slope = covXY / varX;
            double intercept = avgY - slope * avgX;

            // R-squared calculation
            double ssTot = yVals.Sum(y => Math.Pow(y - avgY, 2));
            double ssRes = 0;
            for (int i = 0; i < n; i++)
            {
                double predicted = slope * xVals[i] + intercept;
                ssRes += Math.Pow(yVals[i] - predicted, 2);
            }
            double r2 = 1 - (ssRes / ssTot);

            return new RegressionResult
            {
                Slope = slope,
                Intercept = intercept,
                RSquared = r2,
                XValues = xVals.Take(n).ToList(),
                YValues = yVals.Take(n).ToList()
            };
        }

        #endregion

        #region Chart Generation

        public PlotModel CreateBasicChart(DataTable dataTable, string columnName, string chartType)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            var model = new PlotModel { Title = $"{columnName} Analizi" };

            switch (chartType)
            {
                case "Bar Grafiği":
                    var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Kategori" };
                    for (int i = 0; i < values.Count; i++)
                        categoryAxis.Labels.Add((i + 1).ToString());
                    var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = columnName };
                    model.Axes.Add(categoryAxis);
                    model.Axes.Add(valueAxis);

                    var barSeries = new BarSeries { Title = columnName };
                    for (int i = 0; i < values.Count; i++)
                        barSeries.Items.Add(new BarItem(values[i]));
                    model.Series.Add(barSeries);
                    break;

                case "Pasta Grafiği":
                    var pie = new PieSeries { Title = columnName, StrokeThickness = 1 };
                    for (int i = 0; i < values.Count; i++)
                        pie.Slices.Add(new PieSlice($"{i + 1}", values[i]));
                    model.Series.Add(pie);
                    break;

                case "Çizgi Grafiği":
                    var line = new LineSeries { Title = columnName, MarkerType = MarkerType.Circle };
                    for (int i = 0; i < values.Count; i++)
                        line.Points.Add(new DataPoint(i, values[i]));
                    model.Series.Add(line);
                    break;

                case "Dağılım Grafiği":
                    var scatter = new ScatterSeries { Title = columnName };
                    for (int i = 0; i < values.Count; i++)
                        scatter.Points.Add(new ScatterPoint(i, values[i]));
                    model.Series.Add(scatter);
                    break;
            }

            return model;
        }

        public PlotModel CreateHistogramChart(string columnName, HistogramResult histogram, int binCount)
        {
            var model = new PlotModel { Title = $"{columnName} Histogramı" };
            
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Aralıklar" };
            for (int i = 0; i < binCount; i++)
            {
                double start = histogram.Min + i * histogram.BinSize;
                double end = (i == binCount - 1) ? histogram.Max : (histogram.Min + (i + 1) * histogram.BinSize);
                categoryAxis.Labels.Add($"{start:F2}-{end:F2}");
            }
            var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Frekans" };
            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            var barSeries = new BarSeries { Title = "Frekans" };
            for (int i = 0; i < binCount; i++)
            {
                barSeries.Items.Add(new BarItem(histogram.Bins[i]));
            }
            model.Series.Add(barSeries);

            return model;
        }

        public PlotModel CreateRegressionChart(RegressionResult regression)
        {
            var model = new PlotModel { Title = "Regresyon Analizi" };
            
            var scatter = new ScatterSeries { Title = "Veri Noktaları" };
            for (int i = 0; i < regression.XValues.Count; i++)
                scatter.Points.Add(new ScatterPoint(regression.XValues[i], regression.YValues[i]));

            var line = new LineSeries { Title = "Tahmin Doğrusu" };
            double minX = regression.XValues.Min();
            double maxX = regression.XValues.Max();
            line.Points.Add(new DataPoint(minX, regression.Slope * minX + regression.Intercept));
            line.Points.Add(new DataPoint(maxX, regression.Slope * maxX + regression.Intercept));

            model.Series.Add(scatter);
            model.Series.Add(line);
            
            return model;
        }

        #endregion

        #region PDF Export

        public void ExportToPdf(DataTable dataTable, string outputPath)
        {
            var pdf = new PdfDocument();
            var page = pdf.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12);
            gfx.DrawString("Türk Çakısı - Veri Analiz Raporu", font, XBrushes.SteelBlue, new XPoint(40, 40));

            double y = 70;
            foreach (DataColumn col in dataTable.Columns)
            {
                gfx.DrawString(col.ColumnName, font, XBrushes.Black, new XPoint(40, y));
                y += 20;
            }

            pdf.Save(outputPath);
        }

        #endregion
    }
}
