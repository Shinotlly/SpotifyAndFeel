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

        public App()
        {

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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await _host.StartAsync();

                _trayIcon = (TaskbarIcon)Resources["TrayIcon"];

                var window = _host.Services.GetRequiredService<MainWindow>();
                MainWindow = window; // Bu, DI ile oluşturulan instance
                window.ShowInTaskbar = true;
                window.Show();
                window.Activate();

                await window.InitializeSpotifyAsync();
                window.EnableRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Spotify initialization failed: {ex.Message}");
            }
        }


        protected override async void OnExit(ExitEventArgs e)
        {
            _trayIcon.Dispose();
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }

        public void OnOpen(object sender, RoutedEventArgs e)
        {
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
        public void OnTrayExit(object sender, RoutedEventArgs e)
        {

            Shutdown();
        }
    }
}