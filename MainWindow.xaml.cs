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
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (icon != null)
                {
                    MyNotifyIcon.Icon = icon;
                }
                else
                {
                    MyNotifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
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