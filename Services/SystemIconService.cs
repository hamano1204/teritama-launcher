using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TeritamaLauncher.Services
{
    public static class SystemIconService
    {
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001; // 16x16
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x000000080;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // キャッシュ用の辞書 (拡張子やフォルダ、URL、特定のexeなどでキー指定)
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new ConcurrentDictionary<string, ImageSource>();

        public static ImageSource? GetIcon(string? target, bool isFolder)
        {
            if (isFolder)
            {
                return GetCachedIcon("folder_dir_default", () => ExtractFolderIcon());
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                return GetCachedIcon("file_default", () => ExtractDefaultFileIcon());
            }

            // 環境変数の展開
            string expanded = Environment.ExpandEnvironmentVariables(target).Trim();
            expanded = ResolveFullPath(expanded);

            // Web URL (http:// or https://)
            if (expanded.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                expanded.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return GetCachedIcon(".html", () => ExtractFileIconByExtension(".html"));
            }

            try
            {
                // ローカルに実体があるフォルダの場合
                if (Directory.Exists(expanded))
                {
                    return GetCachedIcon("folder_dir_default", () => ExtractFolderIcon());
                }

                // ローカルファイルが存在する場合
                if (File.Exists(expanded))
                {
                    string ext = Path.GetExtension(expanded).ToLower();
                    // .exe や .lnk など、ファイルごとにアイコンが違う場合はフルパスをキーにする
                    if (ext == ".exe" || ext == ".lnk" || ext == ".ico")
                    {
                        return GetCachedIcon(expanded, () => ExtractFileIconFromPath(expanded));
                    }
                    else
                    {
                        // それ以外のファイル（.xlsx, .docx等）は拡張子ごとにキャッシュ
                        return GetCachedIcon(ext, () => ExtractFileIconFromPath(expanded));
                    }
                }

                // ファイルが存在しない場合は、拡張子からデフォルトアイコンを取得
                string fallbackExt = Path.GetExtension(expanded).ToLower();
                if (!string.IsNullOrEmpty(fallbackExt))
                {
                    return GetCachedIcon(fallbackExt, () => ExtractFileIconByExtension(fallbackExt));
                }
            }
            catch
            {
                // 例外発生時は汎用デフォルトファイルアイコン
            }

            return GetCachedIcon("file_default", () => ExtractDefaultFileIcon());
        }

        private static ImageSource GetCachedIcon(string key, Func<ImageSource?> extractor)
        {
            if (_iconCache.TryGetValue(key, out var cachedImg))
            {
                return cachedImg;
            }

            var img = extractor() ?? CreateFallbackEmptySource();
            _iconCache.TryAdd(key, img);
            return img;
        }

        private static ImageSource? ExtractFolderIcon()
        {
            var shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(
                "dummy_folder", 
                FILE_ATTRIBUTE_DIRECTORY, 
                ref shinfo, 
                (uint)Marshal.SizeOf(shinfo), 
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            return ConvertHIconToImageSource(shinfo.hIcon);
        }

        private static ImageSource? ExtractDefaultFileIcon()
        {
            var shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(
                "dummy_file", 
                FILE_ATTRIBUTE_NORMAL, 
                ref shinfo, 
                (uint)Marshal.SizeOf(shinfo), 
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            return ConvertHIconToImageSource(shinfo.hIcon);
        }

        private static ImageSource? ExtractFileIconByExtension(string extension)
        {
            var shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(
                extension, 
                FILE_ATTRIBUTE_NORMAL, 
                ref shinfo, 
                (uint)Marshal.SizeOf(shinfo), 
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);

            return ConvertHIconToImageSource(shinfo.hIcon);
        }

        private static ImageSource? ExtractFileIconFromPath(string path)
        {
            var shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(
                path, 
                0, 
                ref shinfo, 
                (uint)Marshal.SizeOf(shinfo), 
                SHGFI_ICON | SHGFI_SMALLICON);

            if (shinfo.hIcon == IntPtr.Zero)
            {
                // 取得失敗時は拡張子によるデフォルト
                return ExtractFileIconByExtension(Path.GetExtension(path));
            }

            return ConvertHIconToImageSource(shinfo.hIcon);
        }

        private static ImageSource? ConvertHIconToImageSource(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero) return null;

            try
            {
                var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                // Imaging.CreateBitmapSourceFromHIcon copies the icon, so we must destroy the original handle to prevent leaks.
                return imageSource;
            }
            catch
            {
                return null;
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        private static ImageSource CreateFallbackEmptySource()
        {
            // 空のビットマップを返す
            return BitmapSource.Create(
                1, 1, 96, 96, 
                PixelFormats.Pbgra32, 
                null, 
                new byte[] { 0, 0, 0, 0 }, 
                4);
        }

        private static string ResolveFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            // すでにルート付きのフルパスになっている、またはURLの場合はそのまま
            if (Path.IsPathRooted(path) || 
                path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            try
            {
                // 1. System32ディレクトリを確認
                string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string sys32Path = Path.Combine(system32, path);
                if (File.Exists(sys32Path))
                {
                    return sys32Path;
                }

                // 2. Windowsディレクトリを確認
                string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string winPath = Path.Combine(windowsDir, path);
                if (File.Exists(winPath))
                {
                    return winPath;
                }

                // 3. アプリ起動ディレクトリを確認
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string appPath = Path.Combine(appDir, path);
                if (File.Exists(appPath))
                {
                    return appPath;
                }

                // 4. 環境変数 PATH を走査
                string? pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (pathEnv != null)
                {
                    foreach (var dir in pathEnv.Split(';'))
                    {
                        string cleanDir = dir.Trim();
                        if (string.IsNullOrEmpty(cleanDir)) continue;
                        
                        try
                        {
                            string fullPath = Path.Combine(cleanDir, path);
                            if (File.Exists(fullPath))
                            {
                                return fullPath;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                // パスが無効な文字を含む場合の例外等を防ぐ
            }

            return path;
        }
    }
}
