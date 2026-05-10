namespace SuikaTextExpander.Models
{
    public class AppConfig
    {
        public uint HotkeyModifiers { get; set; } = 0x0001 | 0x0008; // Alt (0x01) + Win (0x08)
        public uint HotkeyKey { get; set; } = 0x53; // 'S' key
        public bool AutoStart { get; set; } = false;

        // 表示用プロパティ
        public string HotkeyText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if ((HotkeyModifiers & 0x0002) != 0) parts.Add("Ctrl");
                if ((HotkeyModifiers & 0x0001) != 0) parts.Add("Alt");
                if ((HotkeyModifiers & 0x0004) != 0) parts.Add("Shift");
                if ((HotkeyModifiers & 0x0008) != 0) parts.Add("Win");
                
                // 仮想キーを文字に変換 (簡易版)
                string keyChar = ((char)HotkeyKey).ToString().ToUpper();
                parts.Add(keyChar);
                
                return string.Join(" + ", parts);
            }
        }
    }
}
