using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpotifyAndFeel
{
    public partial class KeyCaptureControl : UserControl
    {
        public ModifierKeys CapturedModifiers { get; private set; }
        public Key CapturedKey { get; private set; }

        private bool isCapturing = false;

        public KeyCaptureControl()
        {
            InitializeComponent();
            txtDisplay.Text = "Click and press a key";
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isCapturing = true;
            txtDisplay.Text = "Press a key...";
            this.Focusable = true;
            this.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!isCapturing)
                return;

            // Modifier yakala
            CapturedModifiers = Keyboard.Modifiers;

            // Ana tuşu yakala
            CapturedKey = e.Key == Key.System ? e.SystemKey : e.Key;

            // Shift/ Ctrl / Alt ana tuş olarak seçilmesin
            if (CapturedKey == Key.LeftCtrl || CapturedKey == Key.RightCtrl ||
                CapturedKey == Key.LeftShift || CapturedKey == Key.RightShift ||
                CapturedKey == Key.LeftAlt || CapturedKey == Key.RightAlt)
                return;

            txtDisplay.Text = FormatKey(CapturedModifiers, CapturedKey);
            isCapturing = false;

            e.Handled = true;
        }

        private string FormatKey(ModifierKeys mod, Key key)
        {
            string m = "";
            if ((mod & ModifierKeys.Control) != 0) m += "Ctrl + ";
            if ((mod & ModifierKeys.Shift) != 0) m += "Shift + ";
            if ((mod & ModifierKeys.Alt) != 0) m += "Alt + ";

            return m + key.ToString();
        }
    }
}

