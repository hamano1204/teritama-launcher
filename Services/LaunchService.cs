using System;
using System.Diagnostics;
using System.Windows;

namespace TeritamaLauncher.Services
{
    public class LaunchService
    {
        public void Launch(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return;

            try
            {
                // 環境変数を展開 (例: %SystemRoot%\notepad.exe -> C:\Windows\notepad.exe)
                string expandedTarget = Environment.ExpandEnvironmentVariables(target);

                var psi = new ProcessStartInfo
                {
                    FileName = expandedTarget,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"起動できませんでした:\n{target}\n\nエラー: {ex.Message}", 
                    "起動エラー", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }
}
