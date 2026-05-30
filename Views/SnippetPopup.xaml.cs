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
        private readonly MouseHookService? _mouseHook;
        private readonly Action<string> _onPaste;
        private List<SnippetNode> _currentNodes;
        private SnippetPopup? _parentPopup;
        private SnippetPopup? _childPopup;
        private bool _isClosingAll = false;
        private bool _isClosing = false;
        private bool _isClosed = false;

        public SnippetPopup(IEnumerable<SnippetNode> rootNodes, Action<string> onPaste)
            : this(rootNodes, onPaste, null)
        {
        }

        private SnippetPopup(IEnumerable<SnippetNode> rootNodes, Action<string> onPaste, SnippetPopup? parent)
        {
            InitializeComponent();
            _parentPopup = parent;
            _hook = new KeyboardHookService();
            _hook.HookKeyDown += OnHookKeyDown;
            _onPaste = onPaste;
            
            _currentNodes = rootNodes.ToList();
            if (_currentNodes.Count == 0)
            {
                _currentNodes.Add(new SnippetNode
                {
                    Title = "（空）",
                    Type = NodeType.Snippet,
                    IsDummy = true
                });
            }
            SnippetList.ItemsSource = _currentNodes;
            if (SnippetList.Items.Count > 0) SnippetList.SelectedIndex = 0;

            SnippetList.SelectionChanged += SnippetList_SelectionChanged;

            if (_parentPopup == null)
            {
                _mouseHook = new MouseHookService();
                _mouseHook.MouseClicked += OnMouseHookClicked;
                _mouseHook.Start();
            }

            _hook.Start();
        }

        private void OnMouseHookClicked(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (!IsMouseOverAnyPopup())
                {
                    CloseAll();
                }
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Set WS_EX_NOACTIVATE
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        private void OnHookKeyDown(object? sender, KeyEventArgs e)
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
                        CloseAll();
                        e.Handled = true;
                        break;
                }
            });
        }

        private void NavigateInto(SnippetNode node)
        {
            // Close any existing child popup first
            if (_childPopup != null)
            {
                var temp = _childPopup;
                _childPopup = null;
                temp.CloseFromThisAndChildren();
            }

            _hook.Stop();

            var helper = new WindowInteropHelper(this);
            IntPtr hMonitor = MonitorFromWindow(helper.Handle, 1); // MONITOR_DEFAULTTONEAREST
            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(mi);
            GetMonitorInfo(hMonitor, ref mi);

            uint dpiX = 96, dpiY = 96;
            try {
                GetDpiForMonitor(hMonitor, 0, out dpiX, out dpiY);
            } catch { }
            double scaleX = dpiX / 96.0;
            double scaleY = dpiY / 96.0;

            double workRight = mi.rcWork.Right / scaleX;
            double workBottom = mi.rcWork.Bottom / scaleY;
            double workLeft = mi.rcWork.Left / scaleX;
            double workTop = mi.rcWork.Top / scaleY;

            double top = this.Top;
            if (SnippetList.SelectedItem != null)
            {
                var container = SnippetList.ItemContainerGenerator.ContainerFromItem(SnippetList.SelectedItem) as FrameworkElement;
                if (container != null)
                {
                    var relativePoint = container.TransformToAncestor(this).Transform(new Point(0, 0));
                    top = this.Top + relativePoint.Y;
                }
            }

            double childWidth = this.ActualWidth > 0 ? this.ActualWidth : 200;
            double left = this.Left + this.ActualWidth - 4; // slight overlap

            if (left + childWidth > workRight)
            {
                left = this.Left - childWidth + 4;
            }
            if (left < workLeft) left = workLeft + 2;

            var child = new SnippetPopup(node.Children, _onPaste, this);
            _childPopup = child;
            child.WindowStartupLocation = WindowStartupLocation.Manual;
            child.Left = left;
            child.Top = top;
            child.HeaderBorder.Visibility = Visibility.Collapsed;

            child.Closed += (s, ev) =>
            {
                _childPopup = null;
                if (!_isClosingAll)
                {
                    this._hook.Start();
                }
            };

            child.Show();

            child.Dispatcher.InvokeAsync(() =>
            {
                if (!child.IsLoaded) return;

                double height = child.ActualHeight;
                double y = child.Top;

                if (y + height > workBottom)
                {
                    y = workBottom - height - 2;
                }
                if (y < workTop) y = workTop + 2;

                child.Top = y;
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private bool NavigateBack()
        {
            if (_parentPopup != null)
            {
                this.Close();
                return true;
            }
            return false;
        }

        private void ExecuteSelection()
        {
            if (SnippetList.SelectedItem is SnippetNode node)
            {
                if (node.IsDummy) return;

                if (node.IsFolder)
                {
                    NavigateInto(node);
                }
                else if (node.IsSnippet)
                {
                    _onPaste?.Invoke(node.Content);
                    CloseAll();
                }
            }
        }

        private void SnippetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_childPopup != null)
            {
                var temp = _childPopup;
                _childPopup = null;
                temp.CloseFromThisAndChildren();
            }
        }

        private void SnippetList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteSelection();
        }

        private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is SnippetNode node)
            {
                if (node.IsSeparator) return;

                if (SnippetList.SelectedItem == node && _childPopup != null) return;

                SnippetList.SelectedItem = node;
                if (node.IsFolder)
                {
                    NavigateInto(node);
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!IsMouseOverAnyPopup())
            {
                CloseAll();
            }
        }

        /// <summary>
        /// 最上位の親ポップアップへ遡り、すべての階層のポップアップウィンドウを閉じます。
        /// </summary>
        public void CloseAll()
        {
            SnippetPopup root = this;
            while (root._parentPopup != null)
            {
                root = root._parentPopup;
            }
            root.CloseFromThisAndChildren();
        }

        /// <summary>
        /// このポップアップおよびすべての子孫ポップアップウィンドウを再帰的に閉じます（親方向へは遡りません）。
        /// </summary>
        private void CloseFromThisAndChildren()
        {
            if (_isClosingAll) return;
            _isClosingAll = true;

            if (_childPopup != null && !_childPopup._isClosing && !_childPopup._isClosed)
            {
                _childPopup.CloseFromThisAndChildren();
            }

            if (!_isClosing && !_isClosed)
            {
                try
                {
                    this.Close();
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        /// <summary>
        /// 現在のマウスカーソルの座標がいずれかのポップアップメニュー領域内に存在するかどうかを判定します。
        /// </summary>
        /// <returns>いずれかのポップアップ上にあれば true、そうでなければ false</returns>
        private bool IsMouseOverAnyPopup()
        {
            POINT mousePt;
            GetCursorPos(out mousePt);

            SnippetPopup? root = this;
            while (root._parentPopup != null)
            {
                root = root._parentPopup;
            }

            SnippetPopup? current = root;
            while (current != null)
            {
                var helper = new WindowInteropHelper(current);
                IntPtr hMonitor = MonitorFromWindow(helper.Handle, 1);
                uint dpiX = 96, dpiY = 96;
                try {
                    GetDpiForMonitor(hMonitor, 0, out dpiX, out dpiY);
                } catch {}
                double scaleX = dpiX / 96.0;
                double scaleY = dpiY / 96.0;

                double left = current.Left * scaleX;
                double top = current.Top * scaleY;
                double width = current.ActualWidth * scaleX;
                double height = current.ActualHeight * scaleY;

                if (mousePt.X >= left && mousePt.X <= left + width &&
                    mousePt.Y >= top && mousePt.Y <= top + height)
                {
                    return true;
                }
                current = current._childPopup;
            }

            return false;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            _hook.HookKeyDown -= OnHookKeyDown;
            _hook.Stop();
            _hook.Dispose();

            if (_mouseHook != null)
            {
                _mouseHook.MouseClicked -= OnMouseHookClicked;
                _mouseHook.Stop();
                _mouseHook.Dispose();
            }

            base.OnClosed(e);
        }

        #region Native Methods
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        #endregion
    }
}
