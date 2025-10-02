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



        public MainWindow(AuthService authService, TokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;

            InitializeComponent();
            PositionBottomRight();
            InitializeVosk();

            // ➋ Kayıt düğmesini başta devre dışı bırak
            btnToggle.IsEnabled = false;
            // Örnek: Uygulama yüklendiğinde Spotify auth akışını başlat
            Loaded += async (_, __) =>
            {

                Debug.WriteLine("[MainWindow] Loaded olayı tetiklendi");

                try
                {
                    await InitializeSpotifyAsync();
                    btnToggle.IsEnabled = true;   // Ses kaydı düğmesini etkinleştirebilirsiniz
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Spotify bağlantısı başarısız:\n{ex.Message}",
                        "Oturum Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            };

            

        }


        public async Task InitializeSpotifyAsync()
        {
            const string scopes =
              "user-read-private user-read-email " +
              "user-read-playback-state user-modify-playback-state user-read-currently-playing";

            // 1. Yetki kodu al
            var (code, redirectUri) = await _authService.GetAuthorizationCodeAsync(scopes);

            // 2. Token al
            var token = await _tokenService.ExchangeCodeForTokenAsync(code, redirectUri);

            Debug.WriteLine($"[MainWindow] access_token uzunluğu: {token.AccessToken.Length}");

            // 3. SpotifyApiService örneğini oluştur
            _spotifyApi = new SpotifyApiService(token.AccessToken);
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
            if (_spotifyApi == null)
            {
                MessageBox.Show(
                    this,
                    "Spotify servisi hazır değil. Lütfen biraz bekleyin.",
                    "Hazır Değil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

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
                MessageBox.Show(
                    this,
                    $"Ses tanıma hatası:\n{ex.Message}",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // 1. Şarkı ara
            string trackUri;
            try
            {
                trackUri = await _spotifyApi.SearchTrackAsync(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Search API hatası:\n{ex.Message}",
                    "Spotify Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // 2. Eğer sonuç yoksa kullanıcıyı bilgilendir
            if (string.IsNullOrEmpty(trackUri))
            {
                MessageBox.Show(
                    this,
                    $"Şarkı bulunamadı: \"{text}\"",
                    "Bulunamadı",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // 3. Bulunan şarkıyı çal
            try
            {
                await _spotifyApi.PlayTrackAsync(trackUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Play API hatası:\n{ex.Message}",
                    "Spotify Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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


    }
}