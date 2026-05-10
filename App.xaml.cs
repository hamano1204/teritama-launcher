using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Runtime.InteropServices;
using SuikaTextExpander.Models;
using SuikaTextExpander.Services;
using SuikaTextExpander.Views;

namespace SuikaTextExpander
{
    public partial class App : Application
    {
        private SnippetManager _snippetManager;
        private HotkeyService _hotkeyService;
        private PasteService _pasteService;
        private MainWindow _hiddenWindow;
        private SnippetPopup _currentPopup;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _snippetManager = new SnippetManager();
            _snippetManager.Load();

            _pasteService = new PasteService();

            _hiddenWindow = new MainWindow();
            _hiddenWindow.Show();

            _hotkeyService = new HotkeyService();
            if (!_hotkeyService.Register(_hiddenWindow, _snippetManager.Config.HotkeyModifiers, _snippetManager.Config.HotkeyKey))
            {
                MessageBox.Show($"ホットキー({_snippetManager.Config.HotkeyText})の登録に失敗しました。", "ホットキーエラー");
            }
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        private void OnHotkeyPressed(object sender, EventArgs e)
        {
            ShowSnippetMenu();
        }

        private void ShowSnippetMenu()
        {
            if (_currentPopup != null && _currentPopup.IsVisible)
            {
                _currentPopup.Close();
                return;
            }

            var popup = new SnippetPopup(_snippetManager.RootNodes, (content) => 
            {
                _pasteService.PasteText(content);
            });

            // マウス位置に表示
            var mousePos = GetMousePosition();
            popup.Left = mousePos.X;
            popup.Top = mousePos.Y;
            
            popup.Show();
            _currentPopup = popup;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private Point GetMousePosition()
        {
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
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
            _hotkeyService?.Dispose();
            base.OnExit(e);
        }
    }
}
