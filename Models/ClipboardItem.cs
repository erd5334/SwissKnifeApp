using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwissKnifeApp.Models
{
    public class ClipboardItem
    {
        public DateTime Time { get; set; }
        public string Type { get; set; } // "Text", "Image", "File"
        public string Preview { get; set; }
        public object Data { get; set; }
    }
}
