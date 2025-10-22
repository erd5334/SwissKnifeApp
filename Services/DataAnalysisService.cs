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

        #region Advanced Statistics (Median, Mode, Quartiles)

        public class AdvancedStatisticsResult
        {
            public string ColumnName { get; set; } = "";
            public double Average { get; set; }
            public double Median { get; set; }
            public double Mode { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double Q1 { get; set; }
            public double Q3 { get; set; }
            public double IQR { get; set; }
            public double StandardDeviation { get; set; }
            public int Count { get; set; }
        }

        public AdvancedStatisticsResult? CalculateAdvancedStatistics(DataTable dataTable, string columnName)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .OrderBy(x => x)
                .ToList();

            if (values.Count == 0)
                return null;

            double avg = values.Average();
            double min = values.Min();
            double max = values.Max();
            double sd = Math.Sqrt(values.Sum(v => Math.Pow(v - avg, 2)) / values.Count);

            // Median
            double median = values.Count % 2 == 0
                ? (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0
                : values[values.Count / 2];

            // Mode
            var grouped = values.GroupBy(x => x).OrderByDescending(g => g.Count());
            double mode = grouped.First().Key;

            // Quartiles
            double q1 = GetPercentile(values, 25);
            double q3 = GetPercentile(values, 75);
            double iqr = q3 - q1;

            return new AdvancedStatisticsResult
            {
                ColumnName = columnName,
                Average = avg,
                Median = median,
                Mode = mode,
                Min = min,
                Max = max,
                Q1 = q1,
                Q3 = q3,
                IQR = iqr,
                StandardDeviation = sd,
                Count = values.Count
            };
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            int n = sortedValues.Count;
            double index = (percentile / 100.0) * (n - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);

            if (lower == upper)
                return sortedValues[lower];

            return sortedValues[lower] + (index - lower) * (sortedValues[upper] - sortedValues[lower]);
        }

        #endregion

        #region Data Cleaning

        public class DataCleaningResult
        {
            public int MissingValuesCount { get; set; }
            public int OutliersCount { get; set; }
            public List<int> OutlierRowIndices { get; set; } = new();
            public string Report { get; set; } = "";
        }

        public DataCleaningResult AnalyzeDataQuality(DataTable dataTable, string columnName)
        {
            var result = new DataCleaningResult();
            var sb = new StringBuilder();

            // Missing values
            int missingCount = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                if (string.IsNullOrWhiteSpace(row[columnName]?.ToString()))
                    missingCount++;
            }
            result.MissingValuesCount = missingCount;
            sb.AppendLine($"📊 Eksik Değer: {missingCount} satır");

            // Outliers (IQR method)
            var values = dataTable.AsEnumerable()
                .Select((r, idx) => new { Value = r[columnName], Index = idx })
                .Where(x => double.TryParse(x.Value?.ToString(), out _))
                .Select(x => new { Value = Convert.ToDouble(x.Value), x.Index })
                .ToList();

            if (values.Count > 0)
            {
                var sortedValues = values.Select(x => x.Value).OrderBy(x => x).ToList();
                double q1 = GetPercentile(sortedValues, 25);
                double q3 = GetPercentile(sortedValues, 75);
                double iqr = q3 - q1;
                double lowerBound = q1 - 1.5 * iqr;
                double upperBound = q3 + 1.5 * iqr;

                var outliers = values.Where(x => x.Value < lowerBound || x.Value > upperBound).ToList();
                result.OutliersCount = outliers.Count;
                result.OutlierRowIndices = outliers.Select(x => x.Index).ToList();
                
                sb.AppendLine($"🔍 Aykırı Değer: {outliers.Count} satır");
                sb.AppendLine($"   Alt Sınır: {lowerBound:F2}, Üst Sınır: {upperBound:F2}");
            }

            result.Report = sb.ToString();
            return result;
        }

        public DataTable FillMissingValues(DataTable dataTable, string columnName, string method = "mean")
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0)
                return dataTable;

            double fillValue = method.ToLower() switch
            {
                "mean" => values.Average(),
                "median" => values.OrderBy(x => x).ElementAt(values.Count / 2),
                _ => values.Average()
            };

            foreach (DataRow row in dataTable.Rows)
            {
                if (string.IsNullOrWhiteSpace(row[columnName]?.ToString()))
                    row[columnName] = fillValue;
            }

            return dataTable;
        }

        public DataTable RemoveOutliers(DataTable dataTable, string columnName)
        {
            var analysis = AnalyzeDataQuality(dataTable, columnName);
            
            // Remove rows with outliers (in reverse order to maintain indices)
            foreach (var idx in analysis.OutlierRowIndices.OrderByDescending(x => x))
            {
                if (idx < dataTable.Rows.Count)
                    dataTable.Rows.RemoveAt(idx);
            }

            return dataTable;
        }

        #endregion

        #region Box Plot

        public PlotModel CreateBoxPlot(DataTable dataTable, string columnName)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .OrderBy(x => x)
                .ToList();

            if (values.Count == 0)
                return new PlotModel { Title = "Veri bulunamadı" };

            var model = new PlotModel { Title = $"{columnName} - Box Plot" };

            double min = values.Min();
            double max = values.Max();
            double q1 = GetPercentile(values, 25);
            double median = GetPercentile(values, 50);
            double q3 = GetPercentile(values, 75);
            double iqr = q3 - q1;
            double lowerWhisker = Math.Max(min, q1 - 1.5 * iqr);
            double upperWhisker = Math.Min(max, q3 + 1.5 * iqr);

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
            categoryAxis.Labels.Add(columnName);
            model.Axes.Add(categoryAxis);
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left });

            var boxSeries = new BoxPlotSeries
            {
                Title = columnName,
                BoxWidth = 0.3
            };

            var item = new BoxPlotItem(0, lowerWhisker, q1, median, q3, upperWhisker);
            
            // Add outliers
            var outliers = values.Where(v => v < lowerWhisker || v > upperWhisker).ToList();
            item.Outliers = outliers;

            boxSeries.Items.Add(item);
            model.Series.Add(boxSeries);

            return model;
        }

        #endregion

        #region Export Functions

        public void ExportToExcel(DataTable dataTable, string outputPath)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Data");

            // Headers
            for (int i = 0; i < dataTable.Columns.Count; i++)
                ws.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;

            // Data
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                for (int j = 0; j < dataTable.Columns.Count; j++)
                    ws.Cells[i + 2, j + 1].Value = dataTable.Rows[i][j];
            }

            // Style headers
            using (var range = ws.Cells[1, 1, 1, dataTable.Columns.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            ws.Cells.AutoFitColumns();
            package.SaveAs(new FileInfo(outputPath));
        }

        public void ExportToJson(DataTable dataTable, string outputPath)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                    dict[col.ColumnName] = row[col] ?? DBNull.Value;
                rows.Add(dict);
            }

            string json = JsonConvert.SerializeObject(rows, Formatting.Indented);
            File.WriteAllText(outputPath, json);
        }

        public void ExportChartToPng(PlotModel plotModel, string outputPath, int width = 800, int height = 600)
        {
            using var stream = File.Create(outputPath);
            var pngExporter = new OxyPlot.Wpf.PngExporter { Width = width, Height = height };
            pngExporter.Export(plotModel, stream);
        }

        #endregion

        #region Time Series Analysis

        public class TimeSeriesResult
        {
            public List<double> Values { get; set; } = new();
            public List<double> Trend { get; set; } = new();
            public List<double> MovingAverage { get; set; } = new();
            public List<double> Forecast { get; set; } = new();
            public double AverageTrend { get; set; }
            public string Summary { get; set; } = "";
        }

        public TimeSeriesResult AnalyzeTimeSeries(DataTable dataTable, string columnName, int movingAverageWindow = 3, int forecastSteps = 5)
        {
            var values = dataTable.AsEnumerable()
                .Select(r => r[columnName])
                .Where(v => double.TryParse(v?.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count < movingAverageWindow)
                return new TimeSeriesResult { Summary = "Yeterli veri yok" };

            var result = new TimeSeriesResult { Values = values };

            // Trend Analysis (Linear regression on time)
            var trend = new List<double>();
            double avgX = (values.Count - 1) / 2.0;
            double avgY = values.Average();
            double numerator = 0, denominator = 0;

            for (int i = 0; i < values.Count; i++)
            {
                numerator += (i - avgX) * (values[i] - avgY);
                denominator += Math.Pow(i - avgX, 2);
            }

            double slope = numerator / denominator;
            double intercept = avgY - slope * avgX;
            result.AverageTrend = slope;

            for (int i = 0; i < values.Count; i++)
                trend.Add(slope * i + intercept);
            result.Trend = trend;

            // Moving Average
            var movingAvg = new List<double>();
            for (int i = 0; i < values.Count; i++)
            {
                if (i < movingAverageWindow - 1)
                {
                    movingAvg.Add(double.NaN);
                }
                else
                {
                    double sum = 0;
                    for (int j = 0; j < movingAverageWindow; j++)
                        sum += values[i - j];
                    movingAvg.Add(sum / movingAverageWindow);
                }
            }
            result.MovingAverage = movingAvg;

            // Simple Forecast (using trend)
            var forecast = new List<double>();
            for (int i = 0; i < forecastSteps; i++)
            {
                int futureIndex = values.Count + i;
                forecast.Add(slope * futureIndex + intercept);
            }
            result.Forecast = forecast;

            result.Summary = $"Trend: {(slope > 0 ? "Artış" : slope < 0 ? "Azalış" : "Sabit")} ({slope:F4})\n" +
                            $"Hareketli Ortalama Penceresi: {movingAverageWindow}\n" +
                            $"Tahmin Adımları: {forecastSteps}";

            return result;
        }

        public PlotModel CreateTimeSeriesChart(TimeSeriesResult timeSeriesResult, string columnName)
        {
            var model = new PlotModel { Title = $"{columnName} - Zaman Serisi Analizi" };

            // Actual values
            var actualSeries = new LineSeries { Title = "Gerçek Değerler", Color = OxyColors.Blue };
            for (int i = 0; i < timeSeriesResult.Values.Count; i++)
                actualSeries.Points.Add(new DataPoint(i, timeSeriesResult.Values[i]));
            model.Series.Add(actualSeries);

            // Trend
            var trendSeries = new LineSeries { Title = "Trend", Color = OxyColors.Red, StrokeThickness = 2 };
            for (int i = 0; i < timeSeriesResult.Trend.Count; i++)
                trendSeries.Points.Add(new DataPoint(i, timeSeriesResult.Trend[i]));
            model.Series.Add(trendSeries);

            // Moving Average
            var maSeries = new LineSeries { Title = "Hareketli Ortalama", Color = OxyColors.Green };
            for (int i = 0; i < timeSeriesResult.MovingAverage.Count; i++)
            {
                if (!double.IsNaN(timeSeriesResult.MovingAverage[i]))
                    maSeries.Points.Add(new DataPoint(i, timeSeriesResult.MovingAverage[i]));
            }
            model.Series.Add(maSeries);

            // Forecast
            var forecastSeries = new LineSeries { Title = "Tahmin", Color = OxyColors.Orange, LineStyle = LineStyle.Dash };
            for (int i = 0; i < timeSeriesResult.Forecast.Count; i++)
                forecastSeries.Points.Add(new DataPoint(timeSeriesResult.Values.Count + i, timeSeriesResult.Forecast[i]));
            model.Series.Add(forecastSeries);

            return model;
        }

        #endregion

        #region Heatmap (Correlation Matrix)

        public PlotModel CreateCorrelationHeatmap(DataTable dataTable)
        {
            var numericCols = dataTable.Columns.Cast<DataColumn>()
                .Where(c => dataTable.AsEnumerable()
                    .All(r => double.TryParse(r[c].ToString(), out _) || string.IsNullOrEmpty(r[c].ToString())))
                .ToList();

            if (numericCols.Count < 2)
                return new PlotModel { Title = "Yeterli sayısal sütun yok" };

            var model = new PlotModel { Title = "Korelasyon Isı Haritası" };

            var heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = numericCols.Count,
                Y0 = 0,
                Y1 = numericCols.Count,
                Interpolate = false,
                RenderMethod = HeatMapRenderMethod.Rectangles
            };

            var data = new double[numericCols.Count, numericCols.Count];
            for (int i = 0; i < numericCols.Count; i++)
            {
                for (int j = 0; j < numericCols.Count; j++)
                {
                    double corr = CalculatePearsonCorrelation(dataTable, numericCols[i], numericCols[j]);
                    data[i, j] = corr;
                }
            }

            heatMapSeries.Data = data;

            model.Series.Add(heatMapSeries);
            model.Axes.Add(new LinearColorAxis { Position = AxisPosition.Right, Palette = OxyPalettes.BlueWhiteRed(256), Minimum = -1, Maximum = 1 });

            var xAxis = new CategoryAxis { Position = AxisPosition.Bottom, Angle = -45 };
            var yAxis = new CategoryAxis { Position = AxisPosition.Left };
            
            foreach (var col in numericCols)
            {
                xAxis.Labels.Add(col.ColumnName);
                yAxis.Labels.Add(col.ColumnName);
            }

            model.Axes.Add(xAxis);
            model.Axes.Add(yAxis);

            return model;
        }

        #endregion

        #region Pivot Table and Grouping

        public DataTable CreatePivotTable(DataTable dataTable, string rowColumn, string columnColumn, string valueColumn, string aggregation = "sum")
        {
            var pivot = new DataTable();
            
            var rowValues = dataTable.AsEnumerable()
                .Select(r => r[rowColumn]?.ToString())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var colValues = dataTable.AsEnumerable()
                .Select(r => r[columnColumn]?.ToString())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            pivot.Columns.Add(rowColumn, typeof(string));
            foreach (var col in colValues)
                pivot.Columns.Add(col ?? "NULL", typeof(double));

            foreach (var row in rowValues)
            {
                var newRow = pivot.NewRow();
                newRow[rowColumn] = row;

                foreach (var col in colValues)
                {
                    var filteredRows = dataTable.AsEnumerable()
                        .Where(r => r[rowColumn]?.ToString() == row && r[columnColumn]?.ToString() == col);

                    var values = filteredRows
                        .Select(r => r[valueColumn])
                        .Where(v => double.TryParse(v?.ToString(), out _))
                        .Select(v => Convert.ToDouble(v))
                        .ToList();

                    if (values.Count > 0)
                    {
                        double result = aggregation.ToLower() switch
                        {
                            "sum" => values.Sum(),
                            "avg" => values.Average(),
                            "count" => values.Count,
                            "min" => values.Min(),
                            "max" => values.Max(),
                            _ => values.Sum()
                        };
                        newRow[col ?? "NULL"] = result;
                    }
                    else
                    {
                        newRow[col ?? "NULL"] = 0;
                    }
                }

                pivot.Rows.Add(newRow);
            }

            return pivot;
        }

        public DataTable GroupByAndAggregate(DataTable dataTable, string groupColumn, string valueColumn, string aggregation = "sum")
        {
            var result = new DataTable();
            result.Columns.Add(groupColumn, typeof(string));
            result.Columns.Add($"{aggregation}({valueColumn})", typeof(double));

            var grouped = dataTable.AsEnumerable()
                .GroupBy(r => r[groupColumn]?.ToString())
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var values = group
                    .Select(r => r[valueColumn])
                    .Where(v => double.TryParse(v?.ToString(), out _))
                    .Select(v => Convert.ToDouble(v))
                    .ToList();

                if (values.Count > 0)
                {
                    double aggValue = aggregation.ToLower() switch
                    {
                        "sum" => values.Sum(),
                        "avg" => values.Average(),
                        "count" => values.Count,
                        "min" => values.Min(),
                        "max" => values.Max(),
                        _ => values.Sum()
                    };

                    var newRow = result.NewRow();
                    newRow[groupColumn] = group.Key ?? "NULL";
                    newRow[$"{aggregation}({valueColumn})"] = aggValue;
                    result.Rows.Add(newRow);
                }
            }

            return result;
        }

        #endregion

        #region Data Comparison and Merge

        public class DataComparisonResult
        {
            public int Table1Rows { get; set; }
            public int Table2Rows { get; set; }
            public int CommonRows { get; set; }
            public int DifferentRows { get; set; }
            public List<string> OnlyInTable1 { get; set; } = new();
            public List<string> OnlyInTable2 { get; set; } = new();
            public string Summary { get; set; } = "";
        }

        public DataComparisonResult CompareDataTables(DataTable table1, DataTable table2, string keyColumn)
        {
            var result = new DataComparisonResult
            {
                Table1Rows = table1.Rows.Count,
                Table2Rows = table2.Rows.Count
            };

            if (!table1.Columns.Contains(keyColumn) || !table2.Columns.Contains(keyColumn))
            {
                result.Summary = "Key sütunu her iki tabloda da mevcut değil.";
                return result;
            }

            var keys1 = table1.AsEnumerable().Select(r => r[keyColumn]?.ToString() ?? "").Where(k => !string.IsNullOrEmpty(k)).ToHashSet();
            var keys2 = table2.AsEnumerable().Select(r => r[keyColumn]?.ToString() ?? "").Where(k => !string.IsNullOrEmpty(k)).ToHashSet();

            result.OnlyInTable1 = keys1.Except(keys2).ToList();
            result.OnlyInTable2 = keys2.Except(keys1).ToList();
            result.CommonRows = keys1.Intersect(keys2).Count();
            result.DifferentRows = result.OnlyInTable1.Count + result.OnlyInTable2.Count;

            result.Summary = $"📊 Karşılaştırma Sonucu:\n" +
                            $"• Tablo 1 Satırları: {result.Table1Rows}\n" +
                            $"• Tablo 2 Satırları: {result.Table2Rows}\n" +
                            $"• Ortak Satırlar: {result.CommonRows}\n" +
                            $"• Sadece Tablo 1'de: {result.OnlyInTable1.Count}\n" +
                            $"• Sadece Tablo 2'de: {result.OnlyInTable2.Count}";

            return result;
        }

        public DataTable MergeDataTables(DataTable table1, DataTable table2, string keyColumn, string mergeType = "inner")
        {
            var result = new DataTable();

            // Add columns from both tables
            foreach (DataColumn col in table1.Columns)
                result.Columns.Add($"T1_{col.ColumnName}", col.DataType);

            foreach (DataColumn col in table2.Columns)
            {
                if (col.ColumnName != keyColumn)
                    result.Columns.Add($"T2_{col.ColumnName}", col.DataType);
            }

            var keys1 = table1.AsEnumerable().ToDictionary(r => r[keyColumn]?.ToString() ?? "", r => r);
            var keys2 = table2.AsEnumerable().ToDictionary(r => r[keyColumn]?.ToString() ?? "", r => r);

            switch (mergeType.ToLower())
            {
                case "inner":
                    foreach (var key in keys1.Keys.Intersect(keys2.Keys))
                    {
                        var newRow = result.NewRow();
                        foreach (DataColumn col in table1.Columns)
                            newRow[$"T1_{col.ColumnName}"] = keys1[key][col.ColumnName];
                        foreach (DataColumn col in table2.Columns)
                            if (col.ColumnName != keyColumn)
                                newRow[$"T2_{col.ColumnName}"] = keys2[key][col.ColumnName];
                        result.Rows.Add(newRow);
                    }
                    break;

                case "left":
                    foreach (var key in keys1.Keys)
                    {
                        var newRow = result.NewRow();
                        foreach (DataColumn col in table1.Columns)
                            newRow[$"T1_{col.ColumnName}"] = keys1[key][col.ColumnName];
                        if (keys2.ContainsKey(key))
                        {
                            foreach (DataColumn col in table2.Columns)
                                if (col.ColumnName != keyColumn)
                                    newRow[$"T2_{col.ColumnName}"] = keys2[key][col.ColumnName];
                        }
                        result.Rows.Add(newRow);
                    }
                    break;

                case "outer":
                    var allKeys = keys1.Keys.Union(keys2.Keys);
                    foreach (var key in allKeys)
                    {
                        var newRow = result.NewRow();
                        if (keys1.ContainsKey(key))
                        {
                            foreach (DataColumn col in table1.Columns)
                                newRow[$"T1_{col.ColumnName}"] = keys1[key][col.ColumnName];
                        }
                        if (keys2.ContainsKey(key))
                        {
                            foreach (DataColumn col in table2.Columns)
                                if (col.ColumnName != keyColumn)
                                    newRow[$"T2_{col.ColumnName}"] = keys2[key][col.ColumnName];
                        }
                        result.Rows.Add(newRow);
                    }
                    break;
            }

            return result;
        }

        #endregion

        #region Pagination

        public DataTable GetPagedData(DataTable dataTable, int pageNumber, int pageSize)
        {
            var pagedTable = dataTable.Clone();
            int startIndex = (pageNumber - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, dataTable.Rows.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                pagedTable.ImportRow(dataTable.Rows[i]);
            }

            return pagedTable;
        }

        public int GetTotalPages(DataTable dataTable, int pageSize)
        {
            return (int)Math.Ceiling((double)dataTable.Rows.Count / pageSize);
        }

        #endregion
    }
}
