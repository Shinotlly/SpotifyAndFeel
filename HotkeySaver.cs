namespace SpotifyAndFeel
{
    public static class HotkeySaver
    {
        public static void SaveAll()
        {
            Properties.Settings.Default.TurkishHotkey =
                HotkeyManager.ToString(HotkeyManager.Hotkeys["btnTurkish"]);

            Properties.Settings.Default.EnglishHotkey =
                HotkeyManager.ToString(HotkeyManager.Hotkeys["btnEnglish"]);

            Properties.Settings.Default.StartHotkey =
                HotkeyManager.ToString(HotkeyManager.Hotkeys["btnStartRecording"]);

            Properties.Settings.Default.Save();
        }
    }
}
