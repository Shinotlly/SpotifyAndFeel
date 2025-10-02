using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyAndFeel.Models;
using SpotifyAndFeel.Services;
using System;
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

            // ➊ DI host’u başlat
            await _host.StartAsync();

            // ➋ Console penceresi aç (isteğe bağlı)
            AllocConsole();

            // ➌ Tray ikonu al
            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

            // ➍ MainWindow örneğini al ve gizle
            var window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.ShowInTaskbar = false;
            window.Hide();

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