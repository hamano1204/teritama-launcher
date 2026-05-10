using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SuikaTextExpander.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        private IntPtr _hWnd;
        private HwndSource _source;

        public event EventHandler HotkeyPressed;

        public bool Register(Window window, uint modifiers, uint key)
        {
            Unregister(); // 既存のホットキーがあれば解除

            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;
            
            if (_source == null)
            {
                _source = HwndSource.FromHwnd(_hWnd);
                _source.AddHook(HwndHook);
            }

            if (!RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, key))
            {
                return false;
            }
            return true;
        }

        public void Unregister()
        {
            if (_hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source.Dispose();
                _source = null;
            }
            _hWnd = IntPtr.Zero;
        }
    }
}
