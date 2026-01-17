using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Views.Modules
{
    public partial class RestClientPage : Page
    {
        private readonly HttpClient _httpClient;
        public ObservableCollection<HttpHeader> Headers { get; } = new();
        public ObservableCollection<HttpHeader> FormData { get; } = new();
        public ObservableCollection<RestHistoryItem> History { get; } = new();

        private const string HISTORY_FILE = "rest_history.json";

        public RestClientPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            
            // Set Default ItemsSource
            DgHeaders.ItemsSource = Headers;
            DgFormData.ItemsSource = FormData;
            LstHistory.ItemsSource = History;

            LoadHistory();

            if (Headers.Count == 0)
            {
                Headers.Add(new HttpHeader { Key = "Accept", Value = "*/*" });
                Headers.Add(new HttpHeader { Key = "User-Agent", Value = "SwissKnifeApp/2.0" });
            }

            CmbBodyType.SelectedIndex = 0;
            CmbMethod.SelectedIndex = 0;
            CmbAuthType.SelectedIndex = 0;
        }

        #region Persistence

        private void LoadHistory()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HISTORY_FILE);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var items = JsonSerializer.Deserialize<List<RestHistoryItem>>(json);
                    if (items != null)
                    {
                        History.Clear();
                        foreach (var item in items.OrderByDescending(x => x.Timestamp))
                        {
                            History.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Geçmiş yüklenirken hata: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HISTORY_FILE);
                string json = JsonSerializer.Serialize(History.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Geçmiş kaydedilirken hata: {ex.Message}");
            }
        }

        #endregion

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;

            if (!url.StartsWith("http")) url = "https://" + url;

            LoadingOverlay.Visibility = Visibility.Visible;
            TxtResponseBody.Clear();
            TxtStatus.Text = "Bekleniyor...";
            
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var methodStr = (CmbMethod.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "GET";
                var method = new HttpMethod(methodStr);
                using var request = new HttpRequestMessage(method, url);

                // Add Headers
                foreach (var header in Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key)))
                {
                    try { request.Headers.TryAddWithoutValidation(header.Key, header.Value); } catch { }
                }

                // Auth
                ApplyAuth(request);

                // Body (POST, PUT, PATCH, DELETE etc.)
                if (method != HttpMethod.Get && method != HttpMethod.Head)
                {
                    if (RbRaw.IsChecked == true)
                    {
                        string bodyType = (CmbBodyType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "application/json";
                        request.Content = new StringContent(TxtBody.Text, Encoding.UTF8, bodyType);
                    }
                    else if (RbFormData.IsChecked == true)
                    {
                        var content = new MultipartFormDataContent();
                        foreach (var item in FormData.Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key)))
                        {
                            content.Add(new StringContent(item.Value), item.Key);
                        }
                        request.Content = content;
                    }
                }

                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                // Fill UI
                TxtStatus.Text = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                TxtStatus.Foreground = response.IsSuccessStatusCode ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                TxtTime.Text = $"{stopwatch.ElapsedMilliseconds} ms";

                var contentStr = await response.Content.ReadAsStringAsync();
                TxtSize.Text = FormatSize(Encoding.UTF8.GetByteCount(contentStr));

                // Format JSON
                TxtResponseBody.Text = TryFormat(contentStr, response.Content.Headers.ContentType?.MediaType);

                // Response Headers
                var respHeaders = response.Headers.Select(h => new { Key = h.Key, Value = string.Join(", ", h.Value) })
                                    .Concat(response.Content.Headers.Select(h => new { Key = h.Key, Value = string.Join(", ", h.Value) }))
                                    .ToList();
                DgResponseHeaders.ItemsSource = respHeaders;

                // Update/Add to history
                AddToHistory(methodStr, url);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                TxtStatus.Text = "Hata";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                TxtResponseBody.Text = $"İstek hatası: {ex.Message}";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void AddToHistory(string method, string url)
        {
            // Gather current state
            var item = new RestHistoryItem
            {
                Method = method,
                Url = url,
                Name = new Uri(url).AbsolutePath == "/" ? new Uri(url).Host : new Uri(url).AbsolutePath,
                Headers = Headers.ToList(),
                RequestBody = TxtBody.Text,
                BodyFormat = RbRaw.IsChecked == true ? "raw" : (RbFormData.IsChecked == true ? "formData" : "none"),
                BodyType = (CmbBodyType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "application/json",
                AuthType = CmbAuthType.SelectedIndex,
                AuthToken = TxtBearerToken.Text,
                AuthUser = TxtBasicUsername.Text,
                AuthPass = TxtBasicPassword.Password,
                Timestamp = DateTime.Now
            };

            History.Insert(0, item);
            SaveHistory();
        }

        private void ApplyAuth(HttpRequestMessage request)
        {
            int authType = CmbAuthType.SelectedIndex;
            if (authType == 1) // Bearer
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TxtBearerToken.Text);
            }
            else if (authType == 2) // Basic
            {
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{TxtBasicUsername.Text}:{TxtBasicPassword.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            }
        }

        private void LstHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstHistory.SelectedItem is RestHistoryItem item)
            {
                TxtUrl.Text = item.Url;
                foreach (var mItem in CmbMethod.Items.Cast<ComboBoxItem>())
                {
                    if (mItem.Content.ToString() == item.Method)
                    {
                        CmbMethod.SelectedItem = mItem;
                        break;
                    }
                }

                Headers.Clear();
                foreach (var h in item.Headers) Headers.Add(h);

                TxtBody.Text = item.RequestBody;
                RbRaw.IsChecked = item.BodyFormat == "raw";
                RbFormData.IsChecked = item.BodyFormat == "formData";
                RbNone.IsChecked = item.BodyFormat == "none";

                CmbAuthType.SelectedIndex = item.AuthType;
                TxtBearerToken.Text = item.AuthToken;
                TxtBasicUsername.Text = item.AuthUser;
                TxtBasicPassword.Password = item.AuthPass;
            }
        }

        private void BtnDeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var item = History.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    History.Remove(item);
                    SaveHistory();
                }
            }
        }

        private void TxtSearchResponse_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearchResponse.Text;
            if (string.IsNullOrEmpty(query)) return;

            string content = TxtResponseBody.Text;
            int index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                TxtResponseBody.Focus();
                TxtResponseBody.Select(index, query.Length);
                // Simple scroll to text might be needed if TextBox supported it easily
            }
        }

        private string TryFormat(string content, string? mediaType)
        {
            if (string.IsNullOrWhiteSpace(content)) return "";
            try
            {
                if (mediaType?.Contains("json") == true || content.Trim().StartsWith("{") || content.Trim().StartsWith("["))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                    return JsonSerializer.Serialize(jsonElement, options);
                }
            }
            catch { }
            return content;
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1) { number /= 1024; counter++; }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private void BtnCopyResponse_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtResponseBody.Text)) Clipboard.SetText(TxtResponseBody.Text);
        }
    }
}
