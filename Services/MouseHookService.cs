using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TeritamaLauncher.Services
{
    public class MouseHookService : IDisposable
    {
        private NativeMethods.LowLevelMouseProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event EventHandler? MouseClicked;

        public MouseHookService()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                _hookId = SetHook(_proc);
            }
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(NativeMethods.LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                ProcessModule? curModule = curProcess.MainModule;
                string? moduleName = curModule?.ModuleName;
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(moduleName ?? ""), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_LBUTTONDOWN || wParam == (IntPtr)NativeMethods.WM_RBUTTONDOWN || wParam == (IntPtr)NativeMethods.WM_MBUTTONDOWN))
            {
                MouseClicked?.Invoke(this, EventArgs.Empty);
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        ~MouseHookService()
        {
            Stop();
        }
    }
}
