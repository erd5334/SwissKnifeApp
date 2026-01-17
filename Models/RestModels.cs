using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwissKnifeApp.Models
{
    public class HttpHeader : INotifyPropertyChanged
    {
        private bool _isEnabled = true;
        private string _key = "";
        private string _value = "";
        private string _description = "";

        public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; OnPropertyChanged(); } }
        public string Key { get => _key; set { _key = value; OnPropertyChanged(); } }
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RestHistoryItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public List<HttpHeader> Headers { get; set; } = new();
        public string RequestBody { get; set; } = "";
        public string BodyType { get; set; } = "application/json";
        public string BodyFormat { get; set; } = "none"; // none, raw, formData
        public int AuthType { get; set; } = 0;
        public string AuthToken { get; set; } = "";
        public string AuthUser { get; set; } = "";
        public string AuthPass { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
