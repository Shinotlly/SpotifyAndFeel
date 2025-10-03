using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAndFeel.Models;
using SpotifyAndFeel.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace SpotifyAndFeel
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private TaskbarIcon _trayIcon;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        public App()
        {
            // Generic Host & DI hazırlığı
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cfg =>
                    cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
                .ConfigureServices((ctx, services) =>
                {
                    var spotifyConfig = ctx.Configuration
                        .GetSection("Spotify")
                        .Get<SpotifyConfig>();
                    services.AddSingleton(spotifyConfig);

                    services.AddSingleton<AuthService>();
                    services.AddSingleton<TokenService>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        // Uygulama ayağa kalkarken burası çalışır
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();
            await _host.StartAsync();
            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

            // DI ile MainWindow’u al ve göster
            var window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.ShowInTaskbar = true;
            window.Show();
            window.Activate();

            // Spotify oturumunu doğrudan burada çalıştır
            Debug.WriteLine("[App] Spotify init başlıyor");
            try
            {
                await window.InitializeSpotifyAsync();
                Debug.WriteLine("[App] Spotify init tamamlandı");
                window.EnableRecording();  // btnToggle.IsEnabled = true yapan public method
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Spotify init hata: {ex.Message}");
                MessageBox.Show(
                    $"Spotify bağlantısı başarısız:\n{ex.Message}",
                    "Oturum Hatası",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Uygulama kapanırken burası çalışır
        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIcon.Dispose();
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }

        // Tray menüsünden “Aç” tıklanınca
        public void OnOpen(object sender, RoutedEventArgs e)
        {
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        // Tray menüsünden “Çıkış” tıklanınca
        public void OnTrayExit(object sender, RoutedEventArgs e)
        {
            // Bu çağrı OnExit override’unu tetikler
            Shutdown();
        }
    }
}