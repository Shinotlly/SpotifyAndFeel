using Hardcodet.Wpf.TaskbarNotification;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Windows;
using Microsoft.Extensions.Configuration;


namespace SpotifyAndFeel
{
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();


            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];


            MainWindow = new MainWindow();
            MainWindow.ShowInTaskbar = false;
            MainWindow.Hide();

        }

        public void OnOpen(object sender, RoutedEventArgs e)
        {
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }


        public void OnExit(object sender, RoutedEventArgs e)
        {
            _trayIcon.Dispose();  
            Shutdown();          
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
    }
}