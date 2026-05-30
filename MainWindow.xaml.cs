using System;
using System.Windows;

namespace SuikaTextExpander
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetupIcon();
        }

        private void SetupIcon()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (exePath.EndsWith(".dll"))
                {
                    exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
                }

                if (System.IO.File.Exists(exePath))
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