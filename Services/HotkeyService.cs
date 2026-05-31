using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TeritamaLauncher.Services
{
    public class HotkeyService : IDisposable
    {
        private const int HOTKEY_ID = 9000;

        private IntPtr _hWnd;
        private HwndSource? _source;

        public event EventHandler? HotkeyPressed;

        public bool Register(Window window, uint modifiers, uint key)
        {
            Unregister(); // 既存のホットキーがあれば解除

            var helper = new WindowInteropHelper(window);
            IntPtr newHWnd = helper.Handle;

            // ウィンドウが変わった場合は _source を作り直す
            if (_source != null && _hWnd != newHWnd)
            {
                _source.RemoveHook(HwndHook);
                _source.Dispose();
                _source = null;
            }

            _hWnd = newHWnd;

            if (_source == null)
            {
                _source = HwndSource.FromHwnd(_hWnd);
                _source.AddHook(HwndHook);
            }

            if (!NativeMethods.RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, key))
            {
                return false;
            }
            return true;
        }

        public void Unregister()
        {
            if (_hWnd != IntPtr.Zero)
            {
                NativeMethods.UnregisterHotKey(_hWnd, HOTKEY_ID);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
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
