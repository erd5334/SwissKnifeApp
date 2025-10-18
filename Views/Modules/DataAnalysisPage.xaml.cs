using Microsoft.Win32;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SwissKnifeApp.Services;

namespace SwissKnifeApp.Views.Modules
{
    public partial class DataAnalysisPage : UserControl
    {
        private readonly DataAnalysisService _service;
        private DataTable _dataTable = new DataTable();

        public DataAnalysisPage()
        {
            InitializeComponent();
            _service = new DataAnalysisService();
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
                    _dataTable = _service.ReadDataFile(dlg.FileName);

                    dataGrid.ItemsSource = _dataTable.DefaultView;
                    var columnNames = _dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                    cmbColumns.ItemsSource = columnNames;
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

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0) return;
            var dv = _dataTable.DefaultView;
            string filter = txtFilter.Text.Trim();
            dv.RowFilter = _service.BuildFilterExpression(_dataTable, filter);
        }

        private void BtnStats_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColumns.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbColumns.SelectedItem.ToString()!;
            var stats = _service.CalculateStatistics(_dataTable, column);

            if (stats == null)
            {
                txtStatsResult.Text = "Seçilen sütun sayısal veri içermiyor.";
                return;
            }

            txtStatsResult.Text = $"📈 Sütun: {stats.ColumnName}\n• Ortalama: {stats.Average:F2}\n• Min: {stats.Min}\n• Max: {stats.Max}\n• Std Sapma: {stats.StandardDeviation:F2}";

            UpdateChart();
        }

        private void BtnCorrelation_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable.Columns.Count < 2)
            {
                MessageBox.Show("En az 2 sayısal sütun olmalı.");
                return;
            }

            string result = _service.GenerateCorrelationMatrix(_dataTable);
            txtStatsResult.Text = result;
        }

        private void CmbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0 || cmbColumns.SelectedItem == null)
                return;

            string col = cmbColumns.SelectedItem.ToString()!;
            var type = (cmbChartType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Bar Grafiği";

            var model = _service.CreateBasicChart(_dataTable, col, type);
            oxyChart.Model = model;
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VeriAnalizRaporu.pdf");
                _service.ExportToPdf(_dataTable, file);
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

            string col = cmbHistogramColumn.SelectedItem.ToString()!;
            var histogram = _service.CalculateHistogram(_dataTable, col);

            if (histogram == null)
            {
                txtHistogramInfo.Text = "Seçilen sütun sayısal veri içermiyor.";
                return;
            }

            var model = _service.CreateHistogramChart(col, histogram, histogram.Bins.Length);
            histogramChart.Model = model;

            txtHistogramInfo.Text = $"• Veri sayısı: {histogram.DataCount}\n• Min: {histogram.Min:F2}, Max: {histogram.Max:F2}, Ortalama: {histogram.Average:F2}";
        }

        // ======================= İLERİ SEVİYE ANALİZ ==========================
        private void BtnRegression_Click(object sender, RoutedEventArgs e)
        {
            if (cmbXColumn.SelectedItem == null || cmbYColumn.SelectedItem == null)
            {
                MessageBox.Show("X ve Y sütunlarını seçiniz.");
                return;
            }

            string colX = cmbXColumn.SelectedItem.ToString()!;
            string colY = cmbYColumn.SelectedItem.ToString()!;

            var regression = _service.CalculateLinearRegression(_dataTable, colX, colY);

            if (regression == null)
            {
                txtRegressionInfo.Text = "Yeterli veri yok.";
                return;
            }

            txtRegressionInfo.Text = $"Regresyon Denklemi: Y = {regression.Slope:F3}X + {regression.Intercept:F3}\nR² = {regression.RSquared:F3}";

            var model = _service.CreateRegressionChart(regression);
            regressionChart.Model = model;
        }

    }
}
