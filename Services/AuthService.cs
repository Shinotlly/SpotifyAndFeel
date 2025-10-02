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


namespace SpotifyAndFeel.Services
{
    public class AuthService
    {
        private readonly SpotifyConfig _config;

        public AuthService(SpotifyConfig config)
        {
            _config = config;
        }

        public async Task<(string Code, string RedirectUri)> GetAuthorizationCodeAsync(string scope)
        {
            // 1. Dinamik port bul
            const int port = 5000;
            // 2. Base address – path içermiyor
            string baseAddress = $"{_config.RedirectUriBase}:{port}";
            // 3. Tarayıcıya vereceğimiz tam URI
            string redirectUri = $"{baseAddress}/callback";
            string state = Guid.NewGuid().ToString("N");

            // 4. Web host’u sadece baseAddress ile ayağa kaldır
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
                                await ctx.Response.WriteAsync(
                                  "<h1>Yetki alındı. Pencereyi kapatabilirsiniz.</h1>");
                                tcs.SetResult(code);
                            });
                        });
                    });
                })
                .Build();

            await host.StartAsync();

            // 5. Kullanıcıyı Spotify izin ekranına yönlendir
            var authUrl =
              "https://accounts.spotify.com/authorize?" +
              $"client_id={_config.ClientId}&response_type=code" +
              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
              $"&scope={Uri.EscapeDataString(scope)}" +
              $"&state={state}";
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // 6. Kod gelene kadar bekle, sonra sunucuyu kapat
            var codeResult = await tcs.Task;
            await host.StopAsync();

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
