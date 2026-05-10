using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SuikaTextExpander.Models;
using SuikaTextExpander.Services;

namespace SuikaTextExpander.Views
{
    public partial class SnippetPopup : Window
    {
        private readonly KeyboardHookService _hook;
        private readonly Action<string> _onPaste;
        private List<SnippetNode> _currentNodes;
        private Stack<List<SnippetNode>> _history = new Stack<List<SnippetNode>>();

        public SnippetPopup(IEnumerable<SnippetNode> rootNodes, Action<string> onPaste)
        {
            InitializeComponent();
            _hook = new KeyboardHookService();
            _hook.HookKeyDown += OnHookKeyDown;
            _onPaste = onPaste;
            
            _currentNodes = rootNodes.ToList();
            SnippetList.ItemsSource = _currentNodes;
            if (SnippetList.Items.Count > 0) SnippetList.SelectedIndex = 0;

            _hook.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Set WS_EX_NOACTIVATE
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        private void OnHookKeyDown(object sender, KeyEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Key)
                {
                    case Key.Down:
                        {
                            int nextIndex = SnippetList.SelectedIndex;
                            do {
                                nextIndex = (nextIndex + 1) % SnippetList.Items.Count;
                            } while (_currentNodes[nextIndex].IsSeparator && nextIndex != SnippetList.SelectedIndex);
                            
                            SnippetList.SelectedIndex = nextIndex;
                            e.Handled = true;
                        }
                        break;
                    case Key.Up:
                        {
                            int prevIndex = SnippetList.SelectedIndex;
                            do {
                                prevIndex = (prevIndex - 1 + SnippetList.Items.Count) % SnippetList.Items.Count;
                            } while (_currentNodes[prevIndex].IsSeparator && prevIndex != SnippetList.SelectedIndex);
                            
                            SnippetList.SelectedIndex = prevIndex;
                            e.Handled = true;
                        }
                        break;
                    case Key.Enter:
                        ExecuteSelection();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        if (SnippetList.SelectedItem is SnippetNode node && node.IsFolder)
                        {
                            NavigateInto(node);
                            e.Handled = true;
                        }
                        break;
                    case Key.Left:
                    case Key.Back:
                        if (NavigateBack())
                        {
                            e.Handled = true;
                        }
                        break;
                    case Key.Escape:
                        Close();
                        e.Handled = true;
                        break;
                }
            });
        }

        private void NavigateInto(SnippetNode node)
        {
            _history.Push(_currentNodes);
            _currentNodes = node.Children.ToList();
            SnippetList.ItemsSource = _currentNodes;
            SnippetList.SelectedIndex = 0;
            UpdateHeader(node.Title);
        }

        private bool NavigateBack()
        {
            if (_history.Count > 0)
            {
                _currentNodes = _history.Pop();
                SnippetList.ItemsSource = _currentNodes;
                SnippetList.SelectedIndex = 0;
                UpdateHeader(_history.Count == 0 ? "定型文一覧" : "...");
                return true;
            }
            return false;
        }

        private void ExecuteSelection()
        {
            if (SnippetList.SelectedItem is SnippetNode node)
            {
                if (node.IsFolder)
                {
                    NavigateInto(node);
                }
                else if (node.IsSnippet)
                {
                    _onPaste?.Invoke(node.Content);
                    Close();
                }
            }
        }

        private void SnippetList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteSelection();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Close if user clicks outside
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hook.Stop();
            _hook.Dispose();
            base.OnClosed(e);
        }

        private void UpdateHeader(string title)
        {
            HeaderTitle.Text = title;
            BackIcon.Visibility = _history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Native Methods
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        #endregion
    }
}
