using System;
using Microsoft.Win32;
using System.Reflection;

namespace SuikaTextExpander.Services
{
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SuikaTextExpander";

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;

                    if (enable)
                    {
                        string exePath = Assembly.GetExecutingAssembly().Location;
                        // .NET Core/5+ returns .dll for Location sometimes, but usually .exe is next to it.
                        // For WPF apps, we want the .exe.
                        if (exePath.EndsWith(".dll"))
                        {
                            exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
                        }
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update startup registry: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    return key.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
