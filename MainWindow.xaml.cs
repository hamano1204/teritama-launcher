using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TeritamaLauncher
{
    public partial class MainWindow : Window
    {
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW  = 0x00040000;

        public MainWindow()
        {
            InitializeComponent();
            // ウィンドウハンドル確定後にスタイルを変更するためSourceInitializedを使用
            SourceInitialized += OnSourceInitialized;
            SetupIcon();
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            // WS_EX_TOOLWINDOW を付与 & WS_EX_APPWINDOW を除去
            // → このウィンドウを Alt+Tab スイッチャーに表示させない
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = Services.NativeMethods.GetWindowLong(hwnd, Services.NativeMethods.GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            exStyle &= ~WS_EX_APPWINDOW;
            Services.NativeMethods.SetWindowLong(hwnd, Services.NativeMethods.GWL_EXSTYLE, exStyle);
        }

        private void SetupIcon()
        {
            try
            {
                // .NET 6+ では Environment.ProcessPath が確実にEXEパスを返す
                string? exePath = Environment.ProcessPath
                    ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    if (icon != null)
                    {
                        MyNotifyIcon.Icon = icon;
                        return;
                    }
                }

                // Fallback to app.ico in base directory
                string baseIcoPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(baseIcoPath))
                {
                    MyNotifyIcon.Icon = new System.Drawing.Icon(baseIcoPath);
                    return;
                }

                MyNotifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch
            {
                MyNotifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Appクラスのメソッドを呼ぶか、直接開く
            (Application.Current as App)?.ShowEditor();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}