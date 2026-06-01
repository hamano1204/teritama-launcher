using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TeritamaLauncher.Models;
using TeritamaLauncher.Services;
using static TeritamaLauncher.Services.NativeMethods;

namespace TeritamaLauncher.Views
{
    public partial class EditorWindow : Window
    {
        private readonly SnippetManager _manager;
        private SnippetNode? _selectedNode;
        private Point _startPoint;
        private SnippetNode? _draggedNode;
        private TreeViewItem? _lastHoveredItem;
        private DropPosition _lastDropPosition = DropPosition.None;

        public EditorWindow(SnippetManager manager)
        {
            InitializeComponent();
            _manager = manager;
            SnippetTree.ItemsSource = _manager.RootNodes;
            _recordedKey = _manager.Config.HotkeyKey;
            HotkeyCharBox.Text = GetKeyDisplayName(_recordedKey);
            AutoStartCheckBox.IsChecked = StartupService.IsStartupEnabled();

            // ホットキー装飾キーの初期化
            ModifierAlt.IsChecked   = (_manager.Config.HotkeyModifiers & MOD_ALT)   != 0;
            ModifierCtrl.IsChecked  = (_manager.Config.HotkeyModifiers & MOD_CTRL)  != 0;
            ModifierShift.IsChecked = (_manager.Config.HotkeyModifiers & MOD_SHIFT) != 0;
            ModifierWin.IsChecked   = (_manager.Config.HotkeyModifiers & MOD_WIN)   != 0;

            // バージョン情報の表示
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"v{version?.ToString(3) ?? "1.0.0"}";
        }

        private void SnippetTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedNode = e.NewValue as SnippetNode;
            if (_selectedNode != null)
            {
                EditorGrid.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                TitleBox.Text = _selectedNode.Title;
                ContentBox.Text = _selectedNode.Content;
                
                ContentBox.IsEnabled = _selectedNode.IsSnippet;
                TitleBox.IsEnabled = !_selectedNode.IsSeparator;
            }
            else
            {
                EditorGrid.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (SnippetsSection == null || SettingsSection == null) return;

            if (NavSnippets.IsChecked == true)
            {
                SnippetsSection.Visibility = Visibility.Visible;
                SettingsSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                SnippetsSection.Visibility = Visibility.Collapsed;
                SettingsSection.Visibility = Visibility.Visible;
            }
        }

        private void TitleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedNode != null)
            {
                _selectedNode.Title = TitleBox.Text;
            }
        }

        private void ContentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedNode != null && _selectedNode.IsSnippet)
            {
                _selectedNode.Content = ContentBox.Text;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newNode = new SnippetNode { Title = "新規定型文", Type = NodeType.Snippet };
            AddNode(newNode);
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var newNode = new SnippetNode { Title = "新規フォルダ", Type = NodeType.Folder };
            AddNode(newNode);
        }

        private void AddSeparatorButton_Click(object sender, RoutedEventArgs e)
        {
            var newNode = new SnippetNode { Title = "----------", Type = NodeType.Separator };
            AddNode(newNode);
        }

        private void AddNode(SnippetNode newNode)
        {
            if (_selectedNode != null && _selectedNode.IsFolder)
            {
                _selectedNode.Children.Add(newNode);
            }
            else
            {
                _manager.RootNodes.Add(newNode);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;

            if (MessageBox.Show("削除してもよろしいですか？", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                RemoveNode(_manager.RootNodes, _selectedNode);
                _selectedNode = null; // 削除後は参照をクリアして幽霊更新を防ぐ
                EditorGrid.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
        }

        private bool RemoveNode(ObservableCollection<SnippetNode> nodes, SnippetNode target)
        {
            if (nodes.Remove(target)) return true;
            foreach (var node in nodes)
            {
                if (RemoveNode(node.Children, target)) return true;
            }
            return false;
        }

        private uint _recordedKey = 0;

        private string GetKeyDisplayName(uint vk)
        {
            if (vk >= 65 && vk <= 90) return ((char)vk).ToString(); // A-Z
            if (vk >= 48 && vk <= 57) return ((char)vk).ToString(); // 0-9
            switch (vk)
            {
                case 112: return "F1";
                case 113: return "F2";
                case 114: return "F3";
                case 115: return "F4";
                case 116: return "F5";
                case 117: return "F6";
                case 118: return "F7";
                case 119: return "F8";
                case 120: return "F9";
                case 121: return "F10";
                case 122: return "F11";
                case 123: return "F12";
                case 32: return "Space";
                default: return ((char)vk).ToString();
            }
        }

        private void HotkeyCharBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            System.Windows.Input.Key key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            
            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
            {
                return;
            }

            int vk = System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
            if (vk > 0)
            {
                _recordedKey = (uint)vk;
                HotkeyCharBox.Text = GetKeyDisplayName(_recordedKey);
                System.Windows.Input.Keyboard.ClearFocus();
            }
        }

        private void HotkeyCharBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HotkeyCharBox.Text = "キーを押してください...";
        }

        private void HotkeyCharBox_LostFocus(object sender, RoutedEventArgs e)
        {
            HotkeyCharBox.Text = GetKeyDisplayName(_recordedKey);
        }

        private void AutoStartCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = AutoStartCheckBox.IsChecked ?? false;
            _manager.Config.AutoStart = isChecked;
            StartupService.SetStartup(isChecked);
        }

        // Drag and Drop Logic
        private void SnippetTree_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void SnippetTree_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var treeView = sender as TreeView;
                    var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (treeViewItem != null)
                    {
                        _draggedNode = treeViewItem.DataContext as SnippetNode;
                        if (_draggedNode != null)
                        {
                            DataObject dragData = new DataObject("SnippetNode", _draggedNode);
                            DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
                        }
                    }
                }
            }
        }

        private void SnippetTree_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (!e.Data.GetDataPresent("SnippetNode"))
            {
                e.Effects = DragDropEffects.None;
                ClearDragIndicators();
            }
            else
            {
                var hitTestResult = VisualTreeHelper.HitTest(SnippetTree, e.GetPosition(SnippetTree));
                var treeViewItem = hitTestResult != null ? FindAncestor<TreeViewItem>(hitTestResult.VisualHit) : null;
                var targetNode = treeViewItem?.DataContext as SnippetNode;

                if (targetNode != null && _draggedNode != null && (_draggedNode == targetNode || IsChildOf(_draggedNode, targetNode)))
                {
                    e.Effects = DragDropEffects.None;
                    ClearDragIndicators();
                }
                else
                {
                    e.Effects = DragDropEffects.Move;

                    if (treeViewItem != null)
                    {
                        Point relativePos = e.GetPosition(treeViewItem);
                        double height = treeViewItem.ActualHeight;
                        DropPosition position = DropPosition.None;

                        if (targetNode != null && targetNode.IsFolder)
                        {
                            if (relativePos.Y < height * 0.3)
                                position = DropPosition.Before;
                            else if (relativePos.Y > height * 0.7)
                                position = DropPosition.After;
                            else
                                position = DropPosition.Inside;
                        }
                        else
                        {
                            if (relativePos.Y < height * 0.5)
                                position = DropPosition.Before;
                            else
                                position = DropPosition.After;
                        }

                        if (_lastHoveredItem != treeViewItem || _lastDropPosition != position)
                        {
                            ClearDragIndicators();
                            _lastHoveredItem = treeViewItem;
                            _lastDropPosition = position;
                            DragDropHelper.SetDropPosition(treeViewItem, position);
                        }
                    }
                    else
                    {
                        ClearDragIndicators();
                    }
                }
            }
            e.Handled = true;
        }

        private void SnippetTree_DragLeave(object sender, DragEventArgs e)
        {
            ClearDragIndicators();
        }

        private void ClearDragIndicators()
        {
            if (_lastHoveredItem != null)
            {
                DragDropHelper.SetDropPosition(_lastHoveredItem, DropPosition.None);
                _lastHoveredItem = null;
                _lastDropPosition = DropPosition.None;
            }
        }

        private void SnippetTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var targetNode = GetNodeAt(e.GetPosition(SnippetTree));

                foreach (var filePath in files)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (string.IsNullOrEmpty(fileName)) fileName = filePath;

                    var newNode = new SnippetNode
                    {
                        Title = fileName,
                        Content = filePath,
                        Type = NodeType.Snippet
                    };

                    if (targetNode == null)
                    {
                        _manager.RootNodes.Add(newNode);
                    }
                    else if (_lastDropPosition == DropPosition.Inside && targetNode.IsFolder)
                    {
                        targetNode.Children.Add(newNode);
                    }
                    else
                    {
                        var parentCollection = FindParentCollection(_manager.RootNodes, targetNode);
                        if (parentCollection != null)
                        {
                            int index = parentCollection.IndexOf(targetNode);
                            if (_lastDropPosition == DropPosition.Before)
                            {
                                parentCollection.Insert(index, newNode);
                            }
                            else
                            {
                                parentCollection.Insert(index + 1, newNode);
                            }
                        }
                    }
                }
                ClearDragIndicators();
                e.Handled = true;
                return;
            }

            if (e.Data.GetDataPresent("SnippetNode"))
            {
                var droppedNode = e.Data.GetData("SnippetNode") as SnippetNode;
                var targetNode = GetNodeAt(e.GetPosition(SnippetTree));

                if (droppedNode != null && droppedNode != targetNode)
                {
                    // 自分自身を自分の子に移動しようとしていないかチェック
                    if (targetNode != null && IsChildOf(droppedNode, targetNode))
                    {
                        ClearDragIndicators();
                        return;
                    }

                    // 元の場所から削除
                    RemoveNode(_manager.RootNodes, droppedNode);

                    // 新しい場所に追加
                    if (targetNode == null)
                    {
                        _manager.RootNodes.Add(droppedNode);
                    }
                    else if (_lastDropPosition == DropPosition.Inside && targetNode.IsFolder)
                    {
                        targetNode.Children.Add(droppedNode);
                    }
                    else
                    {
                        var parentCollection = FindParentCollection(_manager.RootNodes, targetNode);
                        if (parentCollection != null)
                        {
                            int index = parentCollection.IndexOf(targetNode);
                            if (_lastDropPosition == DropPosition.Before)
                            {
                                parentCollection.Insert(index, droppedNode);
                            }
                            else
                            {
                                parentCollection.Insert(index + 1, droppedNode);
                            }
                        }
                    }
                }
            }
            ClearDragIndicators();
        }

        private SnippetNode? GetNodeAt(Point pos)
        {
            var hitTestResult = VisualTreeHelper.HitTest(SnippetTree, pos);
            if (hitTestResult != null)
            {
                var treeViewItem = FindAncestor<TreeViewItem>(hitTestResult.VisualHit);
                if (treeViewItem != null)
                {
                    return treeViewItem.DataContext as SnippetNode;
                }
            }
            return null;
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            DependencyObject? active = current;
            do
            {
                if (active is T target) return target;
                active = VisualTreeHelper.GetParent(active);
            }
            while (active != null);
            return null;
        }

        private bool IsChildOf(SnippetNode parent, SnippetNode potentialChild)
        {
            if (parent == potentialChild) return true;
            foreach (var child in parent.Children)
            {
                if (IsChildOf(child, potentialChild)) return true;
            }
            return false;
        }

        private ObservableCollection<SnippetNode>? FindParentCollection(ObservableCollection<SnippetNode> nodes, SnippetNode target)
        {
            if (nodes.Contains(target)) return nodes;
            foreach (var node in nodes)
            {
                var found = FindParentCollection(node.Children, target);
                if (found != null) return found;
            }
            return null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // ホットキーバリデーション
            if (_recordedKey == 0)
            {
                MessageBox.Show("ホットキーのキーが設定されていません。\nキーボードショートカット設定でキーを選択してください。",
                    "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ホットキー装飾キーの取得
            uint modifiers = 0;
            if (ModifierAlt.IsChecked   == true) modifiers |= MOD_ALT;
            if (ModifierCtrl.IsChecked  == true) modifiers |= MOD_CTRL;
            if (ModifierShift.IsChecked == true) modifiers |= MOD_SHIFT;
            if (ModifierWin.IsChecked   == true) modifiers |= MOD_WIN;

            _manager.Config.HotkeyModifiers = modifiers;
            _manager.Config.HotkeyKey = _recordedKey;

            if (!_manager.Save())
            {
                MessageBox.Show("設定の保存に失敗しました。\nディスクの空き容量を確認してください。",
                    "保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ((App)Application.Current).UpdateHotkey();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Executables (*.exe;*.cmd;*.bat;*.ps1)|*.exe;*.cmd;*.bat;*.ps1",
                Title = "ファイル・ショートカットの選択"
            };

            if (dialog.ShowDialog() == true)
            {
                ContentBox.Text = dialog.FileName;
                if (_selectedNode != null)
                {
                    _selectedNode.Content = dialog.FileName;
                }
            }
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "フォルダの選択"
            };

            if (dialog.ShowDialog() == true)
            {
                ContentBox.Text = dialog.FolderName;
                if (_selectedNode != null)
                {
                    _selectedNode.Content = dialog.FolderName;
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "teritama_backup.json",
                Title = "設定のエクスポート"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _manager.Export(dialog.FileName);
                    MessageBox.Show("エクスポートが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"エクスポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("インポートを行うと現在のすべての設定が上書きされます。よろしいですか？", 
                                       "インポートの確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes) return;

            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "設定のインポート"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _manager.Import(dialog.FileName);
                    // TreeViewの再読み込みを促すためにItemsSourceを再セット（あるいはPropertyChangedが必要）
                    SnippetTree.ItemsSource = null;
                    SnippetTree.ItemsSource = _manager.RootNodes;
                    
                    MessageBox.Show("インポートが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public enum DropPosition
    {
        None,
        Before,
        After,
        Inside
    }

    public static class DragDropHelper
    {
        public static readonly DependencyProperty DropPositionProperty =
            DependencyProperty.RegisterAttached("DropPosition", typeof(DropPosition), typeof(DragDropHelper),
                new FrameworkPropertyMetadata(DropPosition.None, FrameworkPropertyMetadataOptions.AffectsRender));

        public static DropPosition GetDropPosition(DependencyObject obj) => (DropPosition)obj.GetValue(DropPositionProperty);
        public static void SetDropPosition(DependencyObject obj, DropPosition value) => obj.SetValue(DropPositionProperty, value);
    }
}
