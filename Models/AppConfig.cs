namespace SuikaTextExpander.Models
{
    public class AppConfig
    {
        public uint HotkeyModifiers { get; set; } = 0x0001 | 0x0008; // Alt (0x01) + Win (0x08)
        public uint HotkeyKey { get; set; } = 0x53; // 'S' key
        
        // Display properties
        public string HotkeyText => "Win + Alt + " + (char)HotkeyKey;
    }
}
