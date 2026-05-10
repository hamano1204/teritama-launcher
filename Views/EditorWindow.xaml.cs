using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SuikaTextExpander.Models;
using SuikaTextExpander.Services;

namespace SuikaTextExpander.Views
{
    public partial class EditorWindow : Window
    {
        private readonly SnippetManager _manager;
        private SnippetNode _selectedNode;
        private Point _startPoint;
        private SnippetNode _draggedNode;

        public EditorWindow(SnippetManager manager)
        {
            InitializeComponent();
            _manager = manager;
            SnippetTree.ItemsSource = _manager.RootNodes;
            HotkeyCharBox.Text = ((char)_manager.Config.HotkeyKey).ToString();
            AutoStartCheckBox.IsChecked = StartupService.IsStartupEnabled();

            // ホットキー装飾キーの初期化
            ModifierAlt.IsChecked = (_manager.Config.HotkeyModifiers & 0x0001) != 0;
            ModifierCtrl.IsChecked = (_manager.Config.HotkeyModifiers & 0x0002) != 0;
            ModifierShift.IsChecked = (_manager.Config.HotkeyModifiers & 0x0004) != 0;
            ModifierWin.IsChecked = (_manager.Config.HotkeyModifiers & 0x0008) != 0;
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
                if (!RemoveNode(_manager.RootNodes, _selectedNode))
                {
                    // RootNodesから削除できなかった場合、子ノードのいずれかから削除されたはず
                }
                EditorGrid.Visibility = Visibility.Collapsed;
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

        private void HotkeyCharBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HotkeyCharBox.Text.Length > 0)
            {
                char c = char.ToUpper(HotkeyCharBox.Text[0]);
                _manager.Config.HotkeyKey = (uint)c;
            }
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
                    var treeViewItem = FindAnscestor<TreeViewItem>((DependencyObject)e.OriginalSource);

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
            if (!e.Data.GetDataPresent("SnippetNode"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                var targetNode = GetNodeAt(e.GetPosition(SnippetTree));
                if (targetNode != null && (_draggedNode == targetNode || IsChildOf(_draggedNode, targetNode)))
                {
                    e.Effects = DragDropEffects.None;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
            e.Handled = true;
        }

        private void SnippetTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SnippetNode"))
            {
                var droppedNode = e.Data.GetData("SnippetNode") as SnippetNode;
                var targetNode = GetNodeAt(e.GetPosition(SnippetTree));

                if (droppedNode != null && droppedNode != targetNode)
                {
                    // 自分自身を自分の子に移動しようとしていないかチェック
                    if (targetNode != null && IsChildOf(droppedNode, targetNode)) return;

                    // 元の場所から削除
                    RemoveNode(_manager.RootNodes, droppedNode);

                    // 新しい場所に追加
                    if (targetNode == null)
                    {
                        _manager.RootNodes.Add(droppedNode);
                    }
                    else if (targetNode.IsFolder)
                    {
                        targetNode.Children.Add(droppedNode);
                    }
                    else
                    {
                        // フォルダ以外のアイテムの上に落とした場合は、そのアイテムと同じ親のコレクションに追加
                        var parentCollection = FindParentCollection(_manager.RootNodes, targetNode);
                        if (parentCollection != null)
                        {
                            int index = parentCollection.IndexOf(targetNode);
                            parentCollection.Insert(index + 1, droppedNode);
                        }
                    }
                }
            }
        }

        private SnippetNode GetNodeAt(Point pos)
        {
            var hitTestResult = VisualTreeHelper.HitTest(SnippetTree, pos);
            if (hitTestResult != null)
            {
                var treeViewItem = FindAnscestor<TreeViewItem>(hitTestResult.VisualHit);
                if (treeViewItem != null)
                {
                    return treeViewItem.DataContext as SnippetNode;
                }
            }
            return null;
        }

        private static T FindAnscestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T) return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
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

        private ObservableCollection<SnippetNode> FindParentCollection(ObservableCollection<SnippetNode> nodes, SnippetNode target)
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
            // ホットキー装飾キーの取得
            uint modifiers = 0;
            if (ModifierAlt.IsChecked == true) modifiers |= 0x0001;
            if (ModifierCtrl.IsChecked == true) modifiers |= 0x0002;
            if (ModifierShift.IsChecked == true) modifiers |= 0x0004;
            if (ModifierWin.IsChecked == true) modifiers |= 0x0008;

            _manager.Config.HotkeyModifiers = modifiers;

            _manager.Save();
            ((App)Application.Current).UpdateHotkey();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InsertTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                int caretIndex = ContentBox.CaretIndex;
                ContentBox.Text = ContentBox.Text.Insert(caretIndex, tag);
                ContentBox.CaretIndex = caretIndex + tag.Length;
                ContentBox.Focus();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "suika_backup.json",
                Title = "定型文のエクスポート"
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
            var result = MessageBox.Show("インポートを行うと現在のすべての定型文が上書きされます。よろしいですか？", 
                                       "インポートの確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes) return;

            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "定型文のインポート"
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
}
