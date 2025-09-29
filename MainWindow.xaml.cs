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


        public MainWindow()
        {
            InitializeComponent();
            PositionBottomRight(); 
            InitializeVosk();
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

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                var finalJson = _recognizer.FinalResult();
                var text = ExtractText(finalJson);

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
            // Turkish düğmesinin işaretini kaldıralım
            btnTurkish.IsChecked = false;
            SetLanguage("en");
        }

        private void BtnEnglish_Unchecked(object sender, RoutedEventArgs e)
        {
            // ToggleButton ister istemez ikili çalıştığı için 
            // onun tekrar işaretlenmesini zorlayabiliriz
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