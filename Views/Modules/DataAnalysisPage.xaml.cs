using Microsoft.Win32;
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
using System.Windows;
using System.Windows.Controls;

namespace SwissKnifeApp.Views.Modules
{
    public partial class DataAnalysisPage : UserControl
    {
        private DataTable _dataTable = new DataTable();

        public DataAnalysisPage()
        {
            InitializeComponent();
            // EPPlus requires setting the license context at runtime.
            // Use environment variable to be compatible across EPPlus versions.
            Environment.SetEnvironmentVariable("EPPLUS_LICENSE_CONTEXT", "NonCommercial", EnvironmentVariableTarget.Process);
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Veri Dosyaları|*.csv;*.xlsx;*.json|Tüm Dosyalar|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string path = dlg.FileName;
                    if (path.EndsWith(".csv"))
                        _dataTable = ReadCsv(path);
                    else if (path.EndsWith(".xlsx"))
                        _dataTable = ReadExcel(path);
                    else if (path.EndsWith(".json"))
                        _dataTable = ReadJson(path);

                    dataGrid.ItemsSource = _dataTable.DefaultView;
                    var columnNames = _dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                    cmbColumns.ItemsSource = columnNames;
                    // Populate other selectors for mid/advanced analyses
                    cmbHistogramColumn.ItemsSource = columnNames;
                    cmbXColumn.ItemsSource = columnNames;
                    cmbYColumn.ItemsSource = columnNames;
                    txtStatsResult.Text = $"Yüklendi: {_dataTable.Rows.Count} satır, {_dataTable.Columns.Count} sütun.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya okunamadı:\n" + ex.Message);
                }
            }
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

            // Establish columns from the first object's keys (preserve order)
            var keys = arr.First().Keys.ToList();
            foreach (var key in keys)
                dt.Columns.Add(key, typeof(object));

            // Add rows by mapping values in the same key order for consistency
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

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0) return;
            var dv = _dataTable.DefaultView;
            string filter = txtFilter.Text.Trim();
            if (string.IsNullOrEmpty(filter))
                dv.RowFilter = "";
            else
            {
                var sb = new StringBuilder();
                foreach (DataColumn col in _dataTable.Columns)
                {
                    if (sb.Length > 0) sb.Append(" OR ");
                    sb.AppendFormat("[{0}] LIKE '%{1}%'", col.ColumnName, filter.Replace("'", "''"));
                }
                dv.RowFilter = sb.ToString();
            }
        }

        private void BtnStats_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColumns.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbColumns.SelectedItem.ToString();
            var values = _dataTable.AsEnumerable()
                .Select(r => r[column])
                .Where(v => double.TryParse(v.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0)
            {
                txtStatsResult.Text = "Seçilen sütun sayısal veri içermiyor.";
                return;
            }

            double avg = values.Average();
            double min = values.Min();
            double max = values.Max();
            double sd = Math.Sqrt(values.Sum(v => Math.Pow(v - avg, 2)) / values.Count);
            txtStatsResult.Text = $"📈 Sütun: {column}\n• Ortalama: {avg:F2}\n• Min: {min}\n• Max: {max}\n• Std Sapma: {sd:F2}";

            UpdateChart();
        }

        private void BtnCorrelation_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable.Columns.Count < 2)
            {
                MessageBox.Show("En az 2 sayısal sütun olmalı.");
                return;
            }

            var numericCols = _dataTable.Columns.Cast<DataColumn>()
                .Where(c => _dataTable.AsEnumerable().All(r => double.TryParse(r[c].ToString(), out _) || string.IsNullOrEmpty(r[c].ToString())))
                .ToList();

            if (numericCols.Count < 2)
            {
                txtStatsResult.Text = "Korelasyon analizi için en az 2 sayısal sütun gerekir.";
                return;
            }

            var corrText = new StringBuilder("📊 Korelasyon Matrisi:\n");
            foreach (var c1 in numericCols)
            {
                foreach (var c2 in numericCols)
                {
                    double corr = PearsonCorrelation(c1, c2);
                    corrText.Append($"{c1.ColumnName}↔{c2.ColumnName}: {corr:F2}\n");
                }
            }
            txtStatsResult.Text = corrText.ToString();
        }

        private double PearsonCorrelation(DataColumn c1, DataColumn c2)
        {
            var vals1 = _dataTable.AsEnumerable()
                .Select(r => r[c1].ToString())
                .Where(s => double.TryParse(s, out _))
                .Select(s => double.Parse(s!)).ToArray();

            var vals2 = _dataTable.AsEnumerable()
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

        private void CmbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0 || cmbColumns.SelectedItem == null)
                return;

            string col = cmbColumns.SelectedItem.ToString();
            var values = _dataTable.AsEnumerable()
                .Select(r => r[col])
                .Where(v => double.TryParse(v.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0) return;

            var model = new PlotModel { Title = $"{col} Analizi" };
            var type = (cmbChartType.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (type)
            {
                case "Bar Grafiği":
                    // Use BarSeries with a CategoryAxis for a standard bar chart
                    var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Kategori" };
                    for (int i = 0; i < values.Count; i++)
                        categoryAxis.Labels.Add((i + 1).ToString());
                    var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = col };
                    model.Axes.Add(categoryAxis);
                    model.Axes.Add(valueAxis);

                    var barSeriesChart = new BarSeries { Title = col };
                    for (int i = 0; i < values.Count; i++)
                        barSeriesChart.Items.Add(new BarItem(values[i]));
                    model.Series.Add(barSeriesChart);
                    break;

                case "Pasta Grafiği":
                    var pie = new PieSeries { Title = col, StrokeThickness = 1 };
                    for (int i = 0; i < values.Count; i++)
                        pie.Slices.Add(new PieSlice($"{i + 1}", values[i]));
                    model.Series.Add(pie);
                    break;

                case "Çizgi Grafiği":
                    var line = new LineSeries { Title = col, MarkerType = MarkerType.Circle };
                    for (int i = 0; i < values.Count; i++)
                        line.Points.Add(new DataPoint(i, values[i]));
                    model.Series.Add(line);
                    break;

                case "Dağılım Grafiği":
                    var scatter = new ScatterSeries { Title = col };
                    for (int i = 0; i < values.Count; i++)
                        scatter.Points.Add(new ScatterPoint(i, values[i]));
                    model.Series.Add(scatter);
                    break;
            }

            oxyChart.Model = model;
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pdf = new PdfDocument();
                var page = pdf.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("Verdana", 12);
                gfx.DrawString("Türk Çakısı - Veri Analiz Raporu", font, XBrushes.SteelBlue, new XPoint(40, 40));

                double y = 70;
                foreach (DataColumn col in _dataTable.Columns)
                {
                    gfx.DrawString(col.ColumnName, font, XBrushes.Black, new XPoint(40, y));
                    y += 20;
                }

                string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VeriAnalizRaporu.pdf");
                pdf.Save(file);
                MessageBox.Show($"PDF raporu oluşturuldu:\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rapor oluşturulamadı:\n" + ex.Message);
            }
        }

        // ======================= ORTA SEVİYE ANALİZ ==========================
        private void BtnHistogram_Click(object sender, RoutedEventArgs e)
        {
            if (cmbHistogramColumn.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string col = cmbHistogramColumn.SelectedItem.ToString();
            var values = _dataTable.AsEnumerable()
                .Select(r => r[col])
                .Where(v => double.TryParse(v.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            if (values.Count == 0)
            {
                txtHistogramInfo.Text = "Seçilen sütun sayısal veri içermiyor.";
                return;
            }

            int binCount = 10;
            double min = values.Min();
            double max = values.Max();
            // Tüm değerler eşitse bölme hatasını engelle
            double range = Math.Max(1e-9, max - min);
            double binSize = range / binCount;
            var bins = new int[binCount];

            foreach (var v in values)
            {
                int idx = (int)((v - min) / binSize);
                if (idx >= binCount) idx = binCount - 1;
                bins[idx]++;
            }

            var model = new PlotModel { Title = $"{col} Histogramı" };
            // Add axes with bin range labels for readability
            var categoryAxisHist = new CategoryAxis { Position = AxisPosition.Left, Title = "Aralıklar" };
            for (int i = 0; i < binCount; i++)
            {
                double start = min + i * binSize;
                double end = (i == binCount - 1) ? max : (min + (i + 1) * binSize);
                categoryAxisHist.Labels.Add($"{start:F2}-{end:F2}");
            }
            var valueAxisHist = new LinearAxis { Position = AxisPosition.Bottom, Title = "Frekans" };
            model.Axes.Add(categoryAxisHist);
            model.Axes.Add(valueAxisHist);

            var barSeries = new OxyPlot.Series.BarSeries { Title = "Frekans" };
            for (int i = 0; i < binCount; i++)
            {
                barSeries.Items.Add(new OxyPlot.Series.BarItem(bins[i]));
            }
            model.Series.Add(barSeries);
            histogramChart.Model = model;

            txtHistogramInfo.Text = $"• Veri sayısı: {values.Count}\n• Min: {min:F2}, Max: {max:F2}, Ortalama: {values.Average():F2}";
        }

        // ======================= İLERİ SEVİYE ANALİZ ==========================
        private void BtnRegression_Click(object sender, RoutedEventArgs e)
        {
            if (cmbXColumn.SelectedItem == null || cmbYColumn.SelectedItem == null)
            {
                MessageBox.Show("X ve Y sütunlarını seçiniz.");
                return;
            }

            string colX = cmbXColumn.SelectedItem.ToString();
            string colY = cmbYColumn.SelectedItem.ToString();

            var xVals = _dataTable.AsEnumerable()
                .Select(r => r[colX])
                .Where(v => double.TryParse(v.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            var yVals = _dataTable.AsEnumerable()
                .Select(r => r[colY])
                .Where(v => double.TryParse(v.ToString(), out _))
                .Select(v => Convert.ToDouble(v))
                .ToList();

            int n = Math.Min(xVals.Count, yVals.Count);
            if (n < 2)
            {
                txtRegressionInfo.Text = "Yeterli veri yok.";
                return;
            }

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

            // R-kare hesapla
            double ssTot = yVals.Sum(y => Math.Pow(y - avgY, 2));
            double ssRes = 0;
            for (int i = 0; i < n; i++)
            {
                double predicted = slope * xVals[i] + intercept;
                ssRes += Math.Pow(yVals[i] - predicted, 2);
            }
            double r2 = 1 - (ssRes / ssTot);

            txtRegressionInfo.Text =
                $"Regresyon Denklemi: Y = {slope:F3}X + {intercept:F3}\nR² = {r2:F3}";

            // Grafik çiz
            var model = new PlotModel { Title = "Regresyon Analizi" };
            var scatter = new ScatterSeries { Title = "Veri Noktaları" };
            for (int i = 0; i < n; i++)
                scatter.Points.Add(new ScatterPoint(xVals[i], yVals[i]));

            var line = new LineSeries { Title = "Tahmin Doğrusu" };
            double minX = xVals.Min(), maxX = xVals.Max();
            line.Points.Add(new DataPoint(minX, slope * minX + intercept));
            line.Points.Add(new DataPoint(maxX, slope * maxX + intercept));

            model.Series.Add(scatter);
            model.Series.Add(line);
            regressionChart.Model = model;
        }

    }
}
