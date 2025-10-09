using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Speech.Recognition;
using System.Text.Json;
using System.Windows;
using Vosk;
using System.IO;
using System.Windows.Media;
using SpotifyAndFeel.Services;
using Microsoft.Extensions.Configuration;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Hosting;
using SpotifyAndFeel.Models;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SpotifyAndFeel
{
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


        public MainWindow(AuthService authService, TokenService tokenService)
        {
            
            _authService = authService;
            _tokenService = tokenService;

            InitializeComponent();
            PositionBottomRight();
            InitializeVosk();
            btnToggle.IsEnabled = false;

            _authService.ToastRequested += async (msg, color, dur) =>
            {
                await ShowToastAsync(msg, color, dur);
            };

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
            SetLanguage("en");
        }

        private void BtnEnglish_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!btnTurkish.IsChecked.GetValueOrDefault())
                btnEnglish.IsChecked = true;
        }

        private void BtnTurkish_Click(object sender, RoutedEventArgs e)
        {
            btnEnglish.IsChecked = false;
            SetLanguage("tr");
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

        public async Task ShowToastAsync(string message, string colorHex = "#1DB954", int durationMs = 2500)
        {
            await _toastLock.WaitAsync();

            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    lock (_uiLock)
                    {
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
