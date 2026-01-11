using NAudio.Wave;
using SpotifyAndFeel.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Vosk;
using SpotifyAndFeel.Models;
using System.Windows.Threading;

namespace SpotifyAndFeel
{
    //DONT FORGET TO CHECK APPSETTINGS.JSON
    //DONT FORGET TO CHECK APPSETTINGS.JSON
    //DONT FORGET TO CHECK APPSETTINGS.JSON

    public partial class MainWindow : Window
    {
        private Model _model;
        private VoskRecognizer _recognizer;
        private WaveInEvent _waveIn;
        private bool _isRecording;
        private string _turkishModelPath;
        private string _englishModelPath;
        private readonly AuthService _authService;
        private readonly TokenService _tokenService;
        private SpotifyApiService _spotifyApi;
        private bool _spotifyInitialized;
        private static readonly SemaphoreSlim _toastLock = new(1, 1);
        private static readonly object _uiLock = new();
        private string _currentLanguage = null;
        private bool modifierActive = false;


        
        private bool IsHotkeyEditMode = false;

        
        private Key _initialKeyTurkish = Key.None;
        private ModifierKeys _initialModsTurkish = ModifierKeys.None;

        private Key _initialKeyEnglish = Key.None;
        private ModifierKeys _initialModsEnglish = ModifierKeys.None;

        private Key _initialKeyStart = Key.None;
        private ModifierKeys _initialModsStart = ModifierKeys.None;


        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int key);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr hookID = IntPtr.Zero;
        private LowLevelKeyboardProc proc;

        private bool mainKeyReleased = false;
        private bool modifierReleased = false;


        const int KEY_R = 0x52;   // R
        bool isRecordingHotkeyPressed = false;

        const int VK_CONTROL = 0x11;

        const int HOTKEY_TURKISH = 9000;
        const int HOTKEY_ENGLISH = 9001;

        const uint MOD_CONTROL = 0x0002;
        const uint VK_LEFT = 0x25;
        const uint VK_RIGHT = 0x27;


        public MainWindow(AuthService authService, TokenService tokenService)
        {

            _authService = authService;
            _tokenService = tokenService;

            InitializeComponent();
            PositionBottomRight();
            InitializeVosk();
            btnToggle.IsEnabled = false;
            HotkeyManager.Load();

            _authService.ToastRequested += async (msg, color, dur) =>
            {
                await ShowToastAsync(msg, color, dur);
            };

            SourceInitialized += MainWindow_SourceInitialized;
            popupSettings.Opened += PopupSettings_Opened;
            popupSettings.Closed += PopupSettings_Closed;

        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            
            var helper = new WindowInteropHelper(this);

            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);

            
            RegisterAllHotkeys();

            
            proc = HookCallback;
            hookID = SetWindowsHookEx(13, proc, IntPtr.Zero, 0);
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (IsHotkeyEditMode)
                return CallNextHookEx(hookID, nCode, wParam, lParam);


            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;

            

            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int vkCode = kb.vkCode;

                var record = HotkeyManager.Hotkeys["btnStartRecording"];

                uint vkRecord = (uint)KeyInterop.VirtualKeyFromKey(record.Key);
                uint vkModifier = (record.Modifiers == ModifierKeys.Control) ? (uint)KeyInterop.VirtualKeyFromKey(Key.LeftCtrl) : 0;

                bool modifierMatch = (Keyboard.Modifiers & record.Modifiers) == record.Modifiers;
                bool modifierRequired = record.Modifiers != ModifierKeys.None;


                if (wParam == (IntPtr)WM_KEYDOWN && vkCode == vkRecord && modifierMatch)
                {
                    if (!isRecordingHotkeyPressed)
                    {
                        isRecordingHotkeyPressed = true;

                        mainKeyReleased = false;
                        modifierReleased = false;

                        PressButtonSafe(btnToggle);
                    }
                }

                

                if (wParam == (IntPtr)WM_KEYUP && vkCode == vkRecord)
                {
                    mainKeyReleased = true;
                }

                if (wParam == (IntPtr)WM_KEYUP)
                {
                    
                    if (!modifierRequired)
                    {
                        modifierReleased = true;
                    }
                    else
                    {
                        
                        if ((Keyboard.Modifiers & record.Modifiers) == ModifierKeys.None)
                        {
                            modifierReleased = true;
                        }
                    }
                }

                if (isRecordingHotkeyPressed && mainKeyReleased && modifierReleased)
                {
                    isRecordingHotkeyPressed = false;

                    PressButtonSequence(btnToggle, btnPlay, 100);
                }
            }

            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }


        private void PressButtonSafe(Button btn)
        {
            if (btn.Dispatcher.CheckAccess())
            {
                
                var peer = new ButtonAutomationPeer(btn);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
            else
            {
                
                btn.Dispatcher.Invoke(() =>
                {
                    var peer = new ButtonAutomationPeer(btn);
                    var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv?.Invoke();
                });
            }
        }

        private void PressButtonSequence(Button first, Button second, int delayMs = 50)
        {
            
            PressButtonSafe(first);

            
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(delayMs);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                PressButtonSafe(second);
            };
            timer.Start();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            if (IsHotkeyEditMode)
                return IntPtr.Zero;

            const int WM_HOTKEY = 0x0312;

             


            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();

                if (id == HotkeyManager.Hotkeys["btnTurkish"].Id)
                    TriggerToggleButtonWithVisual(btnTurkish);

                else if (id == HotkeyManager.Hotkeys["btnEnglish"].Id)
                    TriggerToggleButtonWithVisual(btnEnglish);

                handled = true;
            }

            return IntPtr.Zero;
        }

        private void PopupSettings_Opened(object? sender, EventArgs e)
        {
            IsHotkeyEditMode = true;

            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            foreach (var hk in HotkeyManager.Hotkeys.Values)
            {
                if (hk.UsesRegisterHotkey)
                    UnregisterHotKey(hwnd, hk.Id);
            }

            _initialKeyTurkish = Key.None;
            _initialModsTurkish = ModifierKeys.None;

            _initialKeyEnglish = Key.None;
            _initialModsEnglish = ModifierKeys.None;

            _initialKeyStart = Key.None;
            _initialModsStart = ModifierKeys.None;
        }

        private void PopupSettings_Closed(object? sender, EventArgs e)
        {
            IsHotkeyEditMode = false;

            HotkeyManager.Load();
            RegisterAllHotkeys();
        }


        private void TriggerToggleButtonWithVisual(ToggleButton toggleButton)
        {
            ToggleButtonAutomationPeer peer = new ToggleButtonAutomationPeer(toggleButton);
            IToggleProvider toggleProv = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider;

            toggleProv?.Toggle();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UnregisterAllHotkeys();
        }
        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_spotifyInitialized) return;

            try
            {
                btnToggle.IsEnabled = true;
                _spotifyInitialized = true;
            }
            catch (Exception ex)
            {
                await ShowToastAsync($"Spotify connection failed: {ex.Message}", "#E53935");
            }
        }

        public void EnableRecording()
        {
            btnToggle.IsEnabled = true;
        }

        public async Task InitializeSpotifyAsync()
        {
            const string scopes =
              "user-read-private user-read-email " +
              "user-read-playback-state user-modify-playback-state user-read-currently-playing";

            var (code, redirectUri) = await _authService.GetAuthorizationCodeAsync(scopes);
            var token = await _tokenService.ExchangeCodeForTokenAsync(code, redirectUri);
            Debug.WriteLine($"[MainWindow] access_token length: {token.AccessToken.Length}");
            _spotifyApi = new SpotifyApiService(token.AccessToken);
            _spotifyApi.ToastRequested += async (message, color, duration) =>
            {
                await ShowToastAsync(message, color, duration);
            };

        }

        private void InitializeVosk()
        {
            Vosk.Vosk.SetLogLevel(0);
            _turkishModelPath = "Models\\vosk-model-small-tr-0.3";
            _englishModelPath = "Models\\vosk-model-small-en-us-0.15";
            InitializeAudio();
            SetLanguage("en");
        }

        private void InitializeAudio()
        {
            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1)
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
        }

        private void SetLanguage(string lang)
        {

            if (_currentLanguage == lang)
                return;

            _currentLanguage = lang;

            if (_isRecording)
            {
                _waveIn.StopRecording();
                _isRecording = false;
                btnToggle.Content = "Start Recording";
            }

            _recognizer?.Dispose();
            _model?.Dispose();

            var modelPath = lang == "tr"
                ? _turkishModelPath
                : _englishModelPath;

            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                _recognizer.Reset();
                txtResult.Clear();
                _waveIn.StartRecording();
                btnToggle.Content = "Stop Recording";
                _isRecording = true;
            }
            else
            {
                _waveIn.StopRecording();
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            _recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded);
        }

        private async void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            string text;
            try
            {
                text = ExtractText(_recognizer.FinalResult());
                Dispatcher.Invoke(() =>
                {
                    txtResult.Text = text;
                    btnToggle.Content = "Start Recording";
                    _isRecording = false;
                });
            }
            catch (Exception ex)
            {
                await ShowToastAsync($"Speech recognition error: {ex.Message}", "#E53935");
                return;
            }
        }

        private string ExtractText(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _waveIn?.Dispose();
            _recognizer?.Dispose();
            _model?.Dispose();

            e.Cancel = true;
            ShowInTaskbar = false;
            Hide();
        }

        private void PositionBottomRight()
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 10;
            Top = wa.Bottom - Height - 10;
        }

        private void BtnEnglish_Click(object sender, RoutedEventArgs e)
        {


            btnTurkish.IsChecked = false;
            btnEnglish.IsChecked = true;

            SetLanguage("en");
            Debug.WriteLine("English seçildi");
        }

        private void BtnEnglish_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!btnTurkish.IsChecked.GetValueOrDefault())
                btnEnglish.IsChecked = true;
        }

        private void BtnTurkish_Click(object sender, RoutedEventArgs e)
        {


            btnEnglish.IsChecked = false;
            btnTurkish.IsChecked = true;

            SetLanguage("tr");
            Debug.WriteLine("Turkish seçildi");
        }

        private void BtnTurkish_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!btnEnglish.IsChecked.GetValueOrDefault())
                btnTurkish.IsChecked = true;
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_spotifyApi == null)
            {
                await ShowToastAsync("Spotify service is not ready yet. Please wait a moment.", "#FFB300");
                return;
            }

            string text = txtResult.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                await ShowToastAsync("Please enter or record some text first.", "#FFB300");
                return;
            }

            try
            {
                var trackUri = await _spotifyApi.SearchTrackAsync(text);

                if (string.IsNullOrEmpty(trackUri))
                {
                    await ShowToastAsync($"No track found for: \"{text}\"", "#FFB300");
                    return;
                }

                await _spotifyApi.PlayTrackAsync(trackUri);
                await ShowToastAsync($"Now playing: \"{text}\" 🎵");
            }
            catch (Exception ex)
            {
                await ShowToastAsync($"Spotify error: {ex.Message}", "#E53935");
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            popupSettings.IsOpen = true;

            txtTurkishHotkey.Text = "";
            txtEnglishHotkey.Text = "";
            txtStartHotkey.Text = "";



            if (HotkeyManager.Hotkeys["btnTurkish"].Modifiers != 0)
            {
                txtTurkishHotkey.Text = HotkeyManager.Hotkeys["btnTurkish"].Modifiers.ToString() + " + ";
            }
            
            txtTurkishHotkey.Text += HotkeyManager.Hotkeys["btnTurkish"].Key.ToString();

            if (HotkeyManager.Hotkeys["btnEnglish"].Modifiers != 0)
            {
                txtEnglishHotkey.Text = HotkeyManager.Hotkeys["btnEnglish"].Modifiers.ToString() + " + ";
            }

            txtEnglishHotkey.Text += HotkeyManager.Hotkeys["btnEnglish"].Key.ToString();

            if (HotkeyManager.Hotkeys["btnStartRecording"].Modifiers != 0)
            {
                txtStartHotkey.Text = HotkeyManager.Hotkeys["btnStartRecording"].Modifiers.ToString() + " + ";
            }
            txtStartHotkey.Text += HotkeyManager.Hotkeys["btnStartRecording"].Key.ToString();
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            
            Key realKey = (e.Key == Key.System) ? e.SystemKey : e.Key;

            
            ModifierKeys mods = ModifierKeys.None;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                mods |= ModifierKeys.Control;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                mods |= ModifierKeys.Alt;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                mods |= ModifierKeys.Shift;
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
                mods |= ModifierKeys.Windows;

            
            HotkeyBinding hotkey = null;
            if (tb.Name.Contains("Turkish"))
                hotkey = HotkeyManager.Hotkeys["btnTurkish"];
            else if (tb.Name.Contains("English"))
                hotkey = HotkeyManager.Hotkeys["btnEnglish"];
            else if (tb.Name.Contains("Start"))
                hotkey = HotkeyManager.Hotkeys["btnStartRecording"];

            if (hotkey != null)
            {
                hotkey.Modifiers = mods;
                hotkey.Key = realKey;

                tb.Text = mods != ModifierKeys.None
                          ? mods + " + " + realKey
                          : realKey.ToString();
            }

            e.Handled = true;
        }



        private void BtnApplyHotkeys_Click(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            HotkeySaver.SaveAll();
            HotkeyManager.Load();

            
            foreach (var kvp in HotkeyManager.Hotkeys)
            {
                var hotkey = kvp.Value;

                if (hotkey.UsesRegisterHotkey)
                    UnregisterHotKey(hwnd, hotkey.Id);
            }

            RegisterAllHotkeys();

            IsHotkeyEditMode = false;
            popupSettings.IsOpen = false;
        }



        private void RegisterAllHotkeys()
        {
            var helper = new WindowInteropHelper(this);

            foreach (var hotkey in HotkeyManager.Hotkeys.Values)
            {
                if (!hotkey.UsesRegisterHotkey)
                    continue; 

                uint mod = (uint)hotkey.Modifiers;
                uint vk = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);

                RegisterHotKey(helper.Handle, hotkey.Id, mod, vk);
            }
        }


        private void UnregisterAllHotkeys()
        {
            var helper = new WindowInteropHelper(this);

            foreach (var hotkey in HotkeyManager.Hotkeys.Values)
            {
                if (!hotkey.UsesRegisterHotkey)
                    continue; 

                UnregisterHotKey(helper.Handle, hotkey.Id);
            }
        }

        public async Task ShowToastAsync(string message, string colorHex = "#1DB954", int durationMs = 2500)
        {
            await _toastLock.WaitAsync();

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    lock (_uiLock)
                    {
                        toastNotification.Visibility = Visibility.Visible;
                        toastNotification.IsHitTestVisible = false;

                        toastText.Text = message;
                        toastNotification.Background =
                            (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex);
                    }
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    lock (_uiLock)
                    {
                        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                        {
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };

                        toastNotification.BeginAnimation(OpacityProperty, fadeIn);
                    }
                });

                await Task.Delay(durationMs);

                await Dispatcher.InvokeAsync(() =>
                {
                    lock (_uiLock)
                    {
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                        {
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };

                        fadeOut.Completed += (s, e) =>
                        {
                            toastNotification.Visibility = Visibility.Collapsed;
                            toastNotification.IsHitTestVisible = false;
                            toastNotification.Opacity = 0;
                        };

                        toastNotification.BeginAnimation(OpacityProperty, fadeOut);
                    }
                });

                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Toast Error] {ex.Message}");
            }
            finally
            {
                _toastLock.Release();
            }
        }

    }
}
