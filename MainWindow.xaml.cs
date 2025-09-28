using System;
using System.ComponentModel;
using System.Globalization;
using System.Speech.Recognition;
using System.Windows;

namespace SpotifyAndFeel
{
    public partial class MainWindow : Window
    {
        private SpeechRecognitionEngine recognizer;
        private bool isListening = false;

        public MainWindow()
        {
            InitializeComponent();
            PositionBottomRight();      // Önceki örnek: pencereyi sağ altta konumlandır
            InitializeSpeechEngine();   // STT motorunu başlat

            

        }

        private void InitializeSpeechEngine()
        {
            // Türkçe model ile tanıyıcı oluştur
            recognizer = new SpeechRecognitionEngine();

            // Mikrofonu input olarak ayarla
            recognizer.SetInputToDefaultAudioDevice();

            // Genel konuşma (dictation) grameri yükle
            recognizer.LoadGrammar(new DictationGrammar());

            // Event abonelikleri
            recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
            recognizer.AudioLevelUpdated += Recognizer_AudioLevelUpdated;
        }

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!isListening)
            {
                txtResult.Text = string.Empty;
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
                btnToggle.Content = "Kaydı Durdur";
                isListening = true;
            }
            else
            {
                recognizer.RecognizeAsyncStop();
            }
        }

        private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtResult.Text = e.Result.Text + "...";
            });
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtResult.Text = e.Result.Text;
            });
        }

        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {

            Dispatcher.Invoke(() =>
            {
                isListening = false;
                btnToggle.Content = "Kaydı Başlat";

                if (e.Error != null)
                {
                    MessageBox.Show($"Tanıma hatası: {e.Error.Message}",
                                    "Hata",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            });
        }

        private void Recognizer_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // X’e basıldığında pencereyi gizle, uygulama tepside kalsın
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
    }
}