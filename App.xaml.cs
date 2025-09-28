using Hardcodet.Wpf.TaskbarNotification;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Windows;

namespace SpotifyAndFeel
{
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();

            // XAML'deki TaskbarIcon örneğini al
            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

            // MainWindow'u oluşturup gizle
            MainWindow = new MainWindow();
            MainWindow.ShowInTaskbar = false;
            MainWindow.Hide();

            foreach (var r in SpeechRecognitionEngine.InstalledRecognizers())
            {
                Console.WriteLine($"{r.Culture.Name} — {r.Description}");
            }

            Console.WriteLine("ssss");

        }

        // Tepsi menüsünden "Aç" tıklandığında
        public void OnOpen(object sender, RoutedEventArgs e)
        {
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        // Tepsi menüsünden "Çıkış" tıklandığında
        public void OnExit(object sender, RoutedEventArgs e)
        {
            _trayIcon.Dispose();   // Kaynakları temizle
            Shutdown();            // Uygulamayı sonlandır
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
    }
}