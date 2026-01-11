using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace SpotifyAndFeel
{
    public class HotkeyBinding
    {
        public string Name { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }
        public int Id { get; set; }
        public bool UsesRegisterHotkey { get; set; }
    }

    public static class HotkeyManager
    {
        public static Dictionary<string, HotkeyBinding> Hotkeys
            = new Dictionary<string, HotkeyBinding>();

        // ---- MAIN LOAD FUNCTION ----
        public static void Load()
        {
            Hotkeys["btnTurkish"] = Parse("btnTurkish",
                Properties.Settings.Default.TurkishHotkey, 9000, true);

            Hotkeys["btnEnglish"] = Parse("btnEnglish",
                Properties.Settings.Default.EnglishHotkey, 9001, true);

            Hotkeys["btnStartRecording"] = Parse("btnStartRecording",
                Properties.Settings.Default.StartHotkey, 9002, false);
        }

        // ---- PARSE STRING → HOTKEY ----
        private static HotkeyBinding Parse(string name, string raw,
            int id, bool usesRegister)
        {
            ModifierKeys mods = ModifierKeys.None;
            Key key = Key.None;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                string[] parts = raw.Split('+');

                foreach (string part in parts)
                {
                    if (Enum.TryParse(part, out ModifierKeys m))
                        mods |= m;
                    else if (Enum.TryParse(part, out Key k))
                        key = k;
                }
            }

            return new HotkeyBinding
            {
                Name = name,
                Modifiers = mods,
                Key = key,
                Id = id,
                UsesRegisterHotkey = usesRegister
            };
        }

        // ---- HOTKEY → STRING ----
        public static string ToString(HotkeyBinding hk)
        {
            if (hk.Modifiers == ModifierKeys.None)
                return hk.Key.ToString();

            return $"{hk.Modifiers}+{hk.Key}";
        }
    }
}
