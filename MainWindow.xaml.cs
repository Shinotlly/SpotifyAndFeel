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

        private SpotifyCurlService _spotify;



        public MainWindow()
        {
            InitializeComponent();
            PositionBottomRight(); 
            InitializeVosk();

            var config = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile("appsettings.development.json", optional: true)
                        .Build();
            var section = config.GetSection("Spotify");
            var clientId = section["ClientId"];
            var redirect = section["RedirectUri"];

            // 2) Servisi hazırla
            _spotify = new SpotifyCurlService(clientId, redirect);

            // 3) Yetkilendirme
            Task.Run(async () =>
            {
                var code = await _spotify.AuthorizeAsync();
                var accessToken = await _spotify.RequestTokensAsync(code);
                Dispatcher.Invoke(() => txtResult.Text = "Spotify hazır");
            });

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

            string text = string.Empty;

            try
            {
                var finalJson = _recognizer.FinalResult();
                text = ExtractText(finalJson);

                Dispatcher.Invoke(() =>
                {
                    txtResult.Text = text;
                    btnToggle.Content = "Start Recording";
                    _isRecording = false;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(ex.Message)
                );
            }
            try
            {
                // 2.1) Access Token (yenileme token’ı varsa yenile, yoksa ilk istek sonrası sakla)
                var accessToken = await _spotify.RefreshAccessTokenAsync();

                // 2.2) Metni Spotify’da ara (ilk eşleşen parçanın URI’si)
                var trackUri = await _spotify.SearchFirstTrackUriAsync(text, accessToken);
                if (string.IsNullOrEmpty(trackUri))
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show("Spotify’da eşleşen şarkı bulunamadı.")
                    );
                    return;
                }

                // 2.3) Parçayı çal
                await _spotify.PlayUriAsync(trackUri, accessToken);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"Spotify isteği başarısız: {ex.Message}")
                );
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