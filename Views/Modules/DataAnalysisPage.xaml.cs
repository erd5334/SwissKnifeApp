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
        private DataTable _secondDataTable = new DataTable();
        private int _currentPage = 1;
        private int _pageSize = 50;
        private bool _isPaginationEnabled = false;

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
                    cmbCleaningColumn.ItemsSource = columnNames;
                    cmbTimeSeriesColumn.ItemsSource = columnNames;
                    cmbPivotRow.ItemsSource = columnNames;
                    cmbPivotColumn.ItemsSource = columnNames;
                    cmbPivotValue.ItemsSource = columnNames;
                    cmbGroupColumn.ItemsSource = columnNames;
                    cmbGroupValue.ItemsSource = columnNames;
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

        // ======================= YENİ ÖZELLİKLER ==========================
        
        private void BtnAdvancedStats_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColumns.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbColumns.SelectedItem.ToString()!;
            var stats = _service.CalculateAdvancedStatistics(_dataTable, column);

            if (stats == null)
            {
                txtStatsResult.Text = "Seçilen sütun sayısal veri içermiyor.";
                return;
            }

            txtStatsResult.Text = $"📊 Detaylı İstatistik: {stats.ColumnName}\n\n" +
                                  $"• Ortalama: {stats.Average:F2}\n" +
                                  $"• Medyan: {stats.Median:F2}\n" +
                                  $"• Mod: {stats.Mode:F2}\n" +
                                  $"• Min: {stats.Min:F2}\n" +
                                  $"• Max: {stats.Max:F2}\n" +
                                  $"• Q1 (1. Çeyrek): {stats.Q1:F2}\n" +
                                  $"• Q3 (3. Çeyrek): {stats.Q3:F2}\n" +
                                  $"• IQR (Çeyrekler Arası): {stats.IQR:F2}\n" +
                                  $"• Std Sapma: {stats.StandardDeviation:F2}\n" +
                                  $"• Veri Sayısı: {stats.Count}";
        }

        private void BtnBoxPlot_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColumns.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbColumns.SelectedItem.ToString()!;
            var model = _service.CreateBoxPlot(_dataTable, column);
            oxyChart.Model = model;
            
            txtStatsResult.Text = $"📦 Box Plot oluşturuldu: {column}\nGrafik, veri dağılımını, medyan, çeyrekler ve aykırı değerleri gösterir.";
        }

        private void BtnExportChart_Click(object sender, RoutedEventArgs e)
        {
            if (oxyChart.Model == null)
            {
                MessageBox.Show("Önce bir grafik oluşturun.");
                return;
            }

            try
            {
                string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Grafik_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                _service.ExportChartToPng(oxyChart.Model, file);
                MessageBox.Show($"Grafik PNG olarak kaydedildi:\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Grafik kaydedilemedi:\n" + ex.Message);
            }
        }

        // ======================= VERİ TEMİZLEME ==========================
        
        private void BtnAnalyzeQuality_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCleaningColumn.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbCleaningColumn.SelectedItem.ToString()!;
            var result = _service.AnalyzeDataQuality(_dataTable, column);
            
            txtCleaningResult.Text = $"🔍 Veri Kalitesi Analizi: {column}\n\n{result.Report}\n\n" +
                                     $"Toplam Satır: {_dataTable.Rows.Count}\n" +
                                     $"Temiz Veri: {_dataTable.Rows.Count - result.MissingValuesCount - result.OutliersCount}";
        }

        private void BtnFillMissing_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCleaningColumn.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            try
            {
                string column = cmbCleaningColumn.SelectedItem.ToString()!;
                _dataTable = _service.FillMissingValues(_dataTable, column, "mean");
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = _dataTable.DefaultView;
                
                txtCleaningResult.Text = $"✅ Eksik değerler ortalama ile dolduruldu: {column}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem başarısız:\n" + ex.Message);
            }
        }

        private void BtnRemoveOutliers_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCleaningColumn.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            try
            {
                string column = cmbCleaningColumn.SelectedItem.ToString()!;
                int beforeCount = _dataTable.Rows.Count;
                _dataTable = _service.RemoveOutliers(_dataTable, column);
                int afterCount = _dataTable.Rows.Count;
                
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = _dataTable.DefaultView;
                
                txtCleaningResult.Text = $"✅ Aykırı değerler kaldırıldı: {column}\n" +
                                        $"Silinen satır sayısı: {beforeCount - afterCount}\n" +
                                        $"Kalan satır sayısı: {afterCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem başarısız:\n" + ex.Message);
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"VeriAnalizi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                _service.ExportToExcel(_dataTable, file);
                MessageBox.Show($"Excel dosyası oluşturuldu:\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Excel dışa aktarılamadı:\n" + ex.Message);
            }
        }

        private void BtnExportJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"VeriAnalizi_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                _service.ExportToJson(_dataTable, file);
                MessageBox.Show($"JSON dosyası oluşturuldu:\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("JSON dışa aktarılamadı:\n" + ex.Message);
            }
        }

        // ======================= PAGINATION ==========================

        private void ChkPagination_Changed(object sender, RoutedEventArgs e)
        {
            _isPaginationEnabled = chkPagination.IsChecked == true;
            paginationPanel.Visibility = _isPaginationEnabled ? Visibility.Visible : Visibility.Collapsed;
            
            if (_isPaginationEnabled)
            {
                if (int.TryParse(txtPageSize.Text, out int pageSize) && pageSize > 0)
                    _pageSize = pageSize;
                _currentPage = 1;
                LoadPage();
            }
            else
            {
                dataGrid.ItemsSource = _dataTable.DefaultView;
            }
        }

        private void LoadPage()
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0) return;

            var pagedData = _service.GetPagedData(_dataTable, _currentPage, _pageSize);
            dataGrid.ItemsSource = pagedData.DefaultView;

            int totalPages = _service.GetTotalPages(_dataTable, _pageSize);
            txtPageInfo.Text = $"Sayfa {_currentPage} / {totalPages} (Toplam: {_dataTable.Rows.Count} satır)";
        }

        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadPage();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPage();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = _service.GetTotalPages(_dataTable, _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                LoadPage();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _service.GetTotalPages(_dataTable, _pageSize);
            LoadPage();
        }

        // ======================= ZAMAN SERİSİ ANALİZİ ==========================

        private void BtnTimeSeries_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTimeSeriesColumn.SelectedItem == null)
            {
                MessageBox.Show("Bir sütun seçiniz.");
                return;
            }

            string column = cmbTimeSeriesColumn.SelectedItem.ToString()!;
            
            int window = int.TryParse(txtMovingAvgWindow.Text, out int w) && w > 0 ? w : 3;
            int forecast = int.TryParse(txtForecastSteps.Text, out int f) && f > 0 ? f : 5;

            var result = _service.AnalyzeTimeSeries(_dataTable, column, window, forecast);
            
            if (result.Values.Count == 0)
            {
                txtTimeSeriesInfo.Text = "Yeterli veri yok veya sütun sayısal değil.";
                return;
            }

            var model = _service.CreateTimeSeriesChart(result, column);
            timeSeriesChart.Model = model;
            txtTimeSeriesInfo.Text = result.Summary;
        }

        // ======================= ISI HARİTASI ==========================

        private void BtnHeatmap_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Önce veri yükleyin.");
                return;
            }

            var model = _service.CreateCorrelationHeatmap(_dataTable);
            heatmapChart.Model = model;
        }

        // ======================= PIVOT & GRUPLAMA ==========================

        private void BtnPivot_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPivotRow.SelectedItem == null || cmbPivotColumn.SelectedItem == null || cmbPivotValue.SelectedItem == null)
            {
                MessageBox.Show("Tüm alanları doldurun.");
                return;
            }

            string rowCol = cmbPivotRow.SelectedItem.ToString()!;
            string colCol = cmbPivotColumn.SelectedItem.ToString()!;
            string valCol = cmbPivotValue.SelectedItem.ToString()!;
            string agg = (cmbPivotAgg.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "sum";

            try
            {
                var pivotTable = _service.CreatePivotTable(_dataTable, rowCol, colCol, valCol, agg);
                pivotResultGrid.ItemsSource = pivotTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Pivot tablo oluşturulamadı:\n" + ex.Message);
            }
        }

        private void BtnGroupBy_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGroupColumn.SelectedItem == null || cmbGroupValue.SelectedItem == null)
            {
                MessageBox.Show("Grup ve değer sütunlarını seçin.");
                return;
            }

            string groupCol = cmbGroupColumn.SelectedItem.ToString()!;
            string valueCol = cmbGroupValue.SelectedItem.ToString()!;
            string agg = (cmbGroupAgg.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "sum";

            try
            {
                var groupedTable = _service.GroupByAndAggregate(_dataTable, groupCol, valueCol, agg);
                pivotResultGrid.ItemsSource = groupedTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gruplama işlemi başarısız:\n" + ex.Message);
            }
        }

        // ======================= VERİ KARŞILAŞTIRMA ==========================

        private void BtnLoadSecondFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Veri Dosyaları|*.csv;*.xlsx;*.json|Tüm Dosyalar|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _secondDataTable = _service.ReadDataFile(dlg.FileName);
                    txtSecondFileInfo.Text = $"Yüklendi: {_secondDataTable.Rows.Count} satır, {_secondDataTable.Columns.Count} sütun";

                    // Update key column combo
                    var commonColumns = _dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Intersect(_secondDataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))
                        .ToList();
                    cmbKeyColumn.ItemsSource = commonColumns;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya okunamadı:\n" + ex.Message);
                }
            }
        }

        private void BtnCompare_Click(object sender, RoutedEventArgs e)
        {
            if (_secondDataTable == null || _secondDataTable.Rows.Count == 0)
            {
                MessageBox.Show("Önce 2. dosyayı yükleyin.");
                return;
            }

            if (cmbKeyColumn.SelectedItem == null)
            {
                MessageBox.Show("Anahtar sütunu seçin.");
                return;
            }

            string keyColumn = cmbKeyColumn.SelectedItem.ToString()!;
            var result = _service.CompareDataTables(_dataTable, _secondDataTable, keyColumn);
            txtComparisonResult.Text = result.Summary;
        }

        private void BtnMerge_Click(object sender, RoutedEventArgs e)
        {
            if (_secondDataTable == null || _secondDataTable.Rows.Count == 0)
            {
                MessageBox.Show("Önce 2. dosyayı yükleyin.");
                return;
            }

            if (cmbKeyColumn.SelectedItem == null)
            {
                MessageBox.Show("Anahtar sütunu seçin.");
                return;
            }

            string keyColumn = cmbKeyColumn.SelectedItem.ToString()!;
            string mergeType = (cmbMergeType.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "inner";

            try
            {
                var mergedTable = _service.MergeDataTables(_dataTable, _secondDataTable, keyColumn, mergeType);
                dataGrid.ItemsSource = mergedTable.DefaultView;
                txtComparisonResult.Text = $"✅ Birleştirme tamamlandı ({mergeType})\nSonuç: {mergedTable.Rows.Count} satır, {mergedTable.Columns.Count} sütun";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Birleştirme başarısız:\n" + ex.Message);
            }
        }

    }
}
