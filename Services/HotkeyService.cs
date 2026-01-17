using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SwissKnifeApp.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_FULLSCREEN = 9001;
        private const int HOTKEY_ID_ACTIVEWINDOW = 9002;
        private const int HOTKEY_ID_REGION = 9003;

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private const uint VK_F = 0x46;
        private const uint VK_W = 0x57;
        private const uint VK_R = 0x52;

        private HwndSource? _source;
        private bool _isRegistered = false;

        public event EventHandler? FullScreenCaptureRequested;
        public event EventHandler? ActiveWindowCaptureRequested;
        public event EventHandler? RegionSelectionRequested;

        public void RegisterHotkeys(System.Windows.Window window)
        {
            if (_isRegistered) return;

            try
            {
                var helper = new WindowInteropHelper(window);
                _source = HwndSource.FromHwnd(helper.Handle);
                
                if (_source != null)
                {
                    _source.AddHook(HwndHook);

                    // Ctrl+Shift+F - Full Screen
                    RegisterHotKey(helper.Handle, HOTKEY_ID_FULLSCREEN, MOD_CONTROL | MOD_SHIFT, VK_F);
                    
                    // Ctrl+Shift+W - Active Window
                    RegisterHotKey(helper.Handle, HOTKEY_ID_ACTIVEWINDOW, MOD_CONTROL | MOD_SHIFT, VK_W);
                    
                    // Ctrl+Shift+R - Region Selection
                    RegisterHotKey(helper.Handle, HOTKEY_ID_REGION, MOD_CONTROL | MOD_SHIFT, VK_R);

                    _isRegistered = true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Hotkey kaydedilemedi!\n\n{ex.Message}\n\nBazı kısayol tuşları zaten kullanımda olabilir.",
                    "Hotkey Hatası",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public void UnregisterHotkeys()
        {
            if (!_isRegistered || _source == null) return;

            try
            {
                var helper = new WindowInteropHelper(System.Windows.Application.Current.MainWindow);
                UnregisterHotKey(helper.Handle, HOTKEY_ID_FULLSCREEN);
                UnregisterHotKey(helper.Handle, HOTKEY_ID_ACTIVEWINDOW);
                UnregisterHotKey(helper.Handle, HOTKEY_ID_REGION);
                
                _source.RemoveHook(HwndHook);
                _isRegistered = false;
            }
            catch
            {
                // Ignore unregister errors
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();

                switch (id)
                {
                    case HOTKEY_ID_FULLSCREEN:
                        FullScreenCaptureRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case HOTKEY_ID_ACTIVEWINDOW:
                        ActiveWindowCaptureRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case HOTKEY_ID_REGION:
                        RegionSelectionRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkeys();
        }
    }
}
