using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SwissKnifeApp.Models
{
    public class ClipboardItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Time { get; set; }
        public string Type { get; set; } // "Text", "Image", "File"
        public string Preview { get; set; }
        public object Data { get; set; }
        public string FullText { get; set; } // OCR text or full text
        
        private bool _isPinned;
        public bool IsPinned 
        { 
            get => _isPinned; 
            set { _isPinned = value; OnPropertyChanged(); } 
        }

        private bool _isTemplate;
        public bool IsTemplate 
        { 
            get => _isTemplate; 
            set { _isTemplate = value; OnPropertyChanged(); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
