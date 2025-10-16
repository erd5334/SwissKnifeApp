using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SwissKnifeApp.Views.Modules
{
    public partial class EyedropperOverlayWindow : Window
    {
        public Color? PickedColor { get; private set; }

        public EyedropperOverlayWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Cover the entire virtual screen (all monitors)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            Activate();
            Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get global screen position
            var p = GetCursorPos();
            // Sample pixel via Win32
            var colorRef = GetScreenPixel(p.x, p.y);
            PickedColor = Color.FromRgb((byte)(colorRef & 0xFF), (byte)((colorRef >> 8) & 0xFF), (byte)((colorRef >> 16) & 0xFF));
            DialogResult = true;
            Close();
        }

        // Win32 interop
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private static POINT GetCursorPos()
        {
            GetCursorPos(out var p);
            return p;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private static int GetScreenPixel(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int colorRef = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            return colorRef;
        }
    }
}