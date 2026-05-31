namespace TeritamaLauncher.Models
{
    public class AppConfig
    {
        public uint HotkeyModifiers { get; set; } = 0x0002 | 0x0004 | 0x0008; // Ctrl (0x02) + Shift (0x04) + Win (0x08)
        public uint HotkeyKey { get; set; } = 0x52; // 'R' key
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
