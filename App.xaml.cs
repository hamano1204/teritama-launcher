using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Threading;
using TeritamaLauncher.Services;
using TeritamaLauncher.Views;

namespace TeritamaLauncher
{
    public partial class App : Application
    {
        private SnippetManager _snippetManager = null!;
        private HotkeyService _hotkeyService = null!;
        private LaunchService _launchService = null!;
        private MainWindow _hiddenWindow = null!;
        private SnippetPopup? _currentPopup;
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 二重起動防止のチェック
            bool createdNew;
            _mutex = new Mutex(true, "TeritamaLauncher-UniqueMutexName-1204", out createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                _mutex = null;
                MessageBox.Show("Teritama Launcher はすでに起動しています。", "二重起動検知", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, ev) => LogException(ev.ExceptionObject as Exception);
            DispatcherUnhandledException += (s, ev) => { LogException(ev.Exception); ev.Handled = false; };

            base.OnStartup(e);

            _snippetManager = new SnippetManager();
            _snippetManager.Load();

            _launchService = new LaunchService();

            _hiddenWindow = new MainWindow();
            _hiddenWindow.Show();

            _hotkeyService = new HotkeyService();
            if (!_hotkeyService.Register(_hiddenWindow, _snippetManager.Config.HotkeyModifiers, _snippetManager.Config.HotkeyKey))
            {
                MessageBox.Show($"ホットキー({_snippetManager.Config.HotkeyText})の登録に失敗しました。", "ホットキーエラー");
            }
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        private void LogException(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                string logText = $"[{DateTime.Now}] Unhandled Exception:\n{ex.ToString()}\n\n";
                System.IO.File.AppendAllText(logPath, logText);
            }
            catch {}
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            ShowSnippetMenu();
        }

        private void ShowSnippetMenu()
        {
            if (_currentPopup != null && _currentPopup.IsVisible)
            {
                _currentPopup.CloseAll();
                return;
            }

            var popup = new SnippetPopup(_snippetManager.RootNodes, (content) => 
            {
                _launchService.Launch(content);
            });

            // 1. マウス物理位置を取得 (Pixels)
            NativeMethods.GetCursorPos(out NativeMethods.POINT mousePt);
            
            // 2. モニタ情報取得 (Pixels)
            IntPtr hMonitor = NativeMethods.MonitorFromPoint(mousePt, 1); // MONITOR_DEFAULTTONEAREST
            NativeMethods.MONITORINFO mi = new NativeMethods.MONITORINFO();
            mi.cbSize = Marshal.SizeOf(mi);
            NativeMethods.GetMonitorInfo(hMonitor, ref mi);

            // 3. モニタのDPI（拡大率）を取得
            uint dpiX = 96, dpiY = 96;
            try {
                NativeMethods.GetDpiForMonitor(hMonitor, 0, out dpiX, out dpiY); // MDT_EFFECTIVE_DPI = 0
            } catch {
            }
            double scaleX = dpiX / 96.0;
            double scaleY = dpiY / 96.0;

            // 4. 初期の論理座標 (DIPs) を計算
            double initialX = mousePt.X / scaleX;
            double initialY = mousePt.Y / scaleY;

            // ちらつきを防ぐため透明にしてからShow
            popup.Opacity = 0;
            popup.WindowStartupLocation = WindowStartupLocation.Manual;
            popup.Left = initialX;
            popup.Top = initialY;
            popup.Show();
            _currentPopup = popup;

            // Show後（レイアウト確定後）に正確なサイズを取得して位置補正
            popup.Dispatcher.InvokeAsync(() =>
            {
                if (popup == null || !popup.IsLoaded) return;

                double width = popup.ActualWidth;
                double height = popup.ActualHeight;

                double workLeft = mi.rcWork.Left / scaleX;
                double workTop = mi.rcWork.Top / scaleY;
                double workRight = mi.rcWork.Right / scaleX;
                double workBottom = mi.rcWork.Bottom / scaleY;

                double x = initialX;
                double y = initialY;

                // 右端
                if (x + width > workRight)
                {
                    x = workRight - width - 2;
                }
                // 下端
                if (y + height > workBottom)
                {
                    y = workBottom - height - 2;
                }

                // 左端・上端
                if (x < workLeft) x = workLeft + 2;
                if (y < workTop) y = workTop + 2;

                popup.Left = x;
                popup.Top = y;

                // 位置確定後に表示
                popup.Opacity = 1;

            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void ShowEditor()
        {
            var editor = new EditorWindow(_snippetManager);
            editor.Show();
        }

        public void UpdateHotkey()
        {
            if (!_hotkeyService.Register(_hiddenWindow, _snippetManager.Config.HotkeyModifiers, _snippetManager.Config.HotkeyKey))
            {
                MessageBox.Show($"新しいホットキー({_snippetManager.Config.HotkeyText})の登録に失敗しました。", "設定エラー");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_hotkeyService != null)
            {
                _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
                _hotkeyService.Dispose();
            }
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch {}
                _mutex.Dispose();
                _mutex = null;
            }
            base.OnExit(e);
        }
    }
}
