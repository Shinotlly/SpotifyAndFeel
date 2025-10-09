using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SpotifyAndFeel.Models;
using SpotifyAndFeel;

namespace SpotifyAndFeel.Services
{
    public class AuthService
    {
        private readonly SpotifyConfig _config;
        public event Action<string, string, int> ToastRequested;

        public AuthService(SpotifyConfig config)
        {
            _config = config;
        }

        private void RaiseToast(string message, string colorHex = "#1DB954", int durationMs = 2500)
        {
            ToastRequested?.Invoke(message, colorHex, durationMs);
        }

        public async Task<(string Code, string RedirectUri)> GetAuthorizationCodeAsync(string scope)
        {
            const int port = 5000;
            string baseAddress = $"{_config.RedirectUriBase}:{port}";

            string redirectUri = $"{baseAddress}/callback";

            string state = Guid.NewGuid().ToString("N");

            var tcs = new TaskCompletionSource<string>();
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseUrls(baseAddress);
                    web.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/callback", async ctx =>
                            {
                                if (ctx.Request.Query["state"] != state)
                                {
                                    ctx.Response.StatusCode = 400;
                                    await ctx.Response.WriteAsync("Invalid state");
                                    return;
                                }
                                var code = ctx.Request.Query["code"];
                                await ctx.Response.WriteAsync("<h1>Authorization successful ✅</h1>");

                                RaiseToast("Spotify account linked successfully 🎧");
                                tcs.TrySetResult(code);
                            });
                        });
                    });
                })
                .Build();

            await host.StartAsync();

            var authUrl =
              "https://accounts.spotify.com/authorize?" +
              $"client_id={_config.ClientId}&response_type=code" +
              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
              $"&scope={Uri.EscapeDataString(scope)}" +
              $"&state={state}";
            try
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                RaiseToast("Waiting for Spotify authorization...");
            }
            catch (Exception ex)
            {
                RaiseToast($"Failed to open browser: {ex.Message}", "#E53935");
            }

            var codeResult = await tcs.Task;
            await host.StopAsync();
            RaiseToast("Authorization complete ✅");

            return (codeResult, redirectUri);
        }

        private int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
