using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SwissKnifeApp.Models;

public class FileListItem : INotifyPropertyChanged
{
    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set { if (_selected != value) { _selected = value; OnPropertyChanged(); } }
    }

    public string FullPath { get; }
    public string RelativePath { get; }
    public long Size { get; }
    public string SizeHuman { get; }

    public FileListItem(string fullPath, string relativePath, long size, string sizeHuman)
    {
        FullPath = fullPath;
        RelativePath = relativePath;
        Size = size;
        SizeHuman = sizeHuman;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
