using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private ContextMenu _currentMenu;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _snippetManager = new SnippetManager();
            _snippetManager.Load();

            _pasteService = new PasteService();

            // ホットキー監視・トレイアイコン用のウィンドウ
            _hiddenWindow = new MainWindow();
            _hiddenWindow.Show();

            _hotkeyService = new HotkeyService();
            if (!_hotkeyService.Register(_hiddenWindow, _snippetManager.Config.HotkeyModifiers, _snippetManager.Config.HotkeyKey))
            {
                MessageBox.Show($"ホットキー({_snippetManager.Config.HotkeyText})の登録に失敗しました。既に他のアプリで使用されている可能性があります。\n設定から別のキーに変更してください。", "ホットキーエラー");
            }
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        private void OnHotkeyPressed(object sender, EventArgs e)
        {
            ShowSnippetMenu();
        }

        private void ShowSnippetMenu()
        {
            if (_currentMenu != null && _currentMenu.IsOpen)
            {
                _currentMenu.IsOpen = false;
                return;
            }

            var menu = new ContextMenu();
            BuildMenu(menu.Items, _snippetManager.RootNodes);

            menu.Placement = PlacementMode.MousePoint;
            menu.PlacementTarget = _hiddenWindow;
            menu.IsOpen = true;
            _currentMenu = menu;
        }

        private void BuildMenu(ItemCollection items, System.Collections.Generic.IEnumerable<SnippetNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsFolder)
                {
                    var subMenu = new MenuItem { Header = node.Title };
                    BuildMenu(subMenu.Items, node.Children);
                    items.Add(subMenu);
                }
                else if (node.IsSeparator)
                {
                    items.Add(new Separator());
                }
                else
                {
                    var item = new MenuItem { Header = node.Title };
                    item.Click += (s, e) => _pasteService.PasteText(node.Content);
                    items.Add(item);
                }
            }
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
