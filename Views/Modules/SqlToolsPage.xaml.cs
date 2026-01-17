using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace SwissKnifeApp.Views.Modules
{
    public partial class SqlToolsPage : Page
    {
        private string _connectionString = "";

        public SqlToolsPage()
        {
            InitializeComponent();
            SqlEditor.Text = "SELECT * FROM sqlite_master WHERE type='table';";
        }

        private void BtnBrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Veritabanı (*.sqlite;*.db;*.db3;*.s3db)|*.sqlite;*.db;*.db3;*.s3db|Tüm Dosyalar (*.*)|*.*",
                Title = "SQLite Veritabanı Seç"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtDbPath.Text = openFileDialog.FileName;
                BtnConnect_Click(null!, null!);
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string dbPath = TxtDbPath.Text.Trim();
            if (string.IsNullOrEmpty(dbPath)) return;

            _connectionString = $"Data Source={dbPath}";
            RefreshTableList();
        }

        private void RefreshTableList()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                
                var tables = new List<string>();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }

                LstTables.ItemsSource = tables;
                TxtStatus.Text = "Bağlandı. " + tables.Count + " tablo bulundu.";
                ErrorBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Bağlantı hatası: " + ex.Message);
            }
        }

        private void LstTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstTables.SelectedItem is string tableName)
            {
                SqlEditor.Text = $"SELECT * FROM {tableName} LIMIT 100;";
                BtnRunQuery_Click(null!, null!);
            }
        }

        private async void BtnRunQuery_Click(object sender, RoutedEventArgs e)
        {
            string query = SqlEditor.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;
            if (string.IsNullOrEmpty(_connectionString))
            {
                ShowError("Önce bir veritabanına bağlanmalısınız.");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            ErrorBar.Visibility = Visibility.Collapsed;
            TxtStatus.Text = "Çalıştırılıyor...";
            
            try
            {
                var dt = await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = query;

                    using var reader = command.ExecuteReader();
                    var dataTable = new DataTable();
                    dataTable.Load(reader);
                    return dataTable;
                });

                DgResults.ItemsSource = dt.DefaultView;
                TxtStatus.Text = $"{dt.Rows.Count} satır döndü. ({DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                ShowError("Sorgu hatası: " + ex.Message);
                DgResults.ItemsSource = null;
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnFormatSql_Click(object sender, RoutedEventArgs e)
        {
            string sql = SqlEditor.Text;
            if (string.IsNullOrWhiteSpace(sql)) return;

            // Simple SQL Formatter Logic
            string[] keywords = { "SELECT", "FROM", "WHERE", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "ORDER BY", "GROUP BY", "LIMIT", "INSERT INTO", "UPDATE", "DELETE", "SET", "VALUES" };
            
            string formatted = sql;
            // Clean extra spaces
            formatted = Regex.Replace(formatted, @"\s+", " ");
            
            foreach (var kw in keywords)
            {
                formatted = Regex.Replace(formatted, $@"(?i)\b{kw}\b", Environment.NewLine + kw.ToUpper());
            }

            SqlEditor.Text = formatted.Trim();
        }

        private void BtnCopySql_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SqlEditor.Text))
            {
                Clipboard.SetText(SqlEditor.Text);
            }
        }

        private void BtnClearSql_Click(object sender, RoutedEventArgs e)
        {
            SqlEditor.Clear();
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (DgResults.ItemsSource is DataView dv)
            {
                var dt = dv.ToTable();
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Dosyası (*.csv)|*.csv",
                    FileName = "query_results.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        var columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                        sb.AppendLine(string.Join(",", columnNames.Select(name => $"\"{name}\"")));

                        foreach (DataRow row in dt.Rows)
                        {
                            var fields = row.ItemArray.Select(field => $"\"{field?.ToString()?.Replace("\"", "\"\"")}\"");
                            sb.AppendLine(string.Join(",", fields));
                        }

                        File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Sonuçlar başarıyla aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowError("Aktarma hatası: " + ex.Message);
                    }
                }
            }
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            ErrorBar.Visibility = Visibility.Visible;
            TxtStatus.Text = "Hata oluştu.";
        }
    }
}
