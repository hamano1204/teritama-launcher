using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;

namespace SuikaTextExpander.Services
{
    public class PasteService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public void PasteText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // プレースホルダーを置換
            string processedText = ProcessPlaceholders(text);

            // クリップボードのバックアップ
            IDataObject? backup = Clipboard.GetDataObject();

            try
            {
                // 定型文をセット
                Clipboard.SetText(processedText);

                // 少し待機（アプリケーションのフォーカス切り替えなどを考慮）
                Thread.Sleep(50);

                // Ctrl + V を送信
                keybd_event(VK_CONTROL, 0, 0, 0);
                keybd_event(VK_V, 0, 0, 0);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);

                // 貼り付け処理が完了するまで少し待機
                Thread.Sleep(100);
            }
            finally
            {
                // クリップボードを復元（オプション）
                // ただし、バックアップからの復元はシリアライズ不可なデータなどで失敗することがあるため、
                // 今回は単純なテキストの復元を試みるか、そのままにします。
                // ユーザーの利便性を考え、ここでは何もしないか、テキストのみ復元するのが安全です。
            }
        }

        private string ProcessPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 1. エスケープされたタグ \{{ を一時的なマーカーに置き換え
            string escapedMarker = "___ESCAPED_BRACE___";
            string result = text.Replace(@"\{{", escapedMarker);

            // 2. {{now:format}} を置換
            result = Regex.Replace(result, @"{{now:(.*?)}}", m =>
            {
                string format = m.Groups[1].Value;
                try
                {
                    return DateTime.Now.ToString(format);
                }
                catch
                {
                    // 無効な書式の場合はタグをそのまま残す
                    return m.Value;
                }
            });

            // 3. マーカーを {{ に戻す
            result = result.Replace(escapedMarker, "{{");

            return result;
        }
    }
}
