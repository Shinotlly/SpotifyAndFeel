using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyAndFeel.Utils;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;



namespace SpotifyAndFeel.Services
{
    public class SpotifyCurlService
    {
        private readonly string _clientId;
        private readonly string _redirectUri;

        private string _codeVerifier;
        private string _refreshToken;

        public SpotifyCurlService(string clientId, string redirectUri)
        {
            _clientId = clientId;
            _redirectUri = redirectUri;
        }

        public async Task<string> AuthorizeAsync()
        {
            // 1) PKCE hazırlığı
            _codeVerifier = PkceUtil.CreateCodeVerifier();
            var challenge = PkceUtil.CreateCodeChallenge(_codeVerifier);
            var scope = Uri.EscapeDataString("user-read-playback-state user-modify-playback-state");

            // 2) Spotify izin sayfası URL’i
            var authUrl =
                "https://accounts.spotify.com/authorize" +
                "?response_type=code" +
                $"&client_id={_clientId}" +
                $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                $"&scope={scope}" +
                $"&code_challenge_method=S256" +
                $"&code_challenge={challenge}";

            var tcs = new TaskCompletionSource<string>();

            using var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web =>
                {
                    // A) Artık UseUrls() yok, sadece Kestrel üzerinden bind
                    web.ConfigureKestrel(opts =>
                    {
                        opts.ListenLocalhost(5003, lo => lo.UseHttps());
                    });

                    // B) /callback path’ini yakala
                    web.Configure(app =>
                    {
                        app.Map("/callback", cb =>
                        {
                            cb.Run(async ctx =>
                            {
                                var code = ctx.Request.Query["code"].ToString();
                                await ctx.Response.WriteAsync(
                                    "<html><body>İzin verildi! Pencereyi kapatabilirsiniz.</body></html>"
                                );
                                tcs.SetResult(code);
                            });
                        });
                    });
                })
                .Build();

            await host.StartAsync();

            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            var codeResult = await tcs.Task;
            await host.StopAsync();
            return codeResult;
        }




        public async Task<string> RequestTokensAsync(string code)
        {
            var postFields = $"grant_type=authorization_code" +
                             $"&code={code}" +
                             $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                             $"&client_id={_clientId}" +
                             $"&code_verifier={_codeVerifier}";

            var cmd = $"curl -s -X POST https://accounts.spotify.com/api/token " +
                      $"-H \"Content-Type: application/x-www-form-urlencoded\" " +
                      $"-d \"{postFields}\"";

            var json = await ShellHelper.RunCommandAsync(cmd);
            var doc = JsonDocument.Parse(json);

            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            _refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
            return accessToken;
        }

        public async Task<string> RefreshAccessTokenAsync()
        {
            var postFields = $"grant_type=refresh_token" +
                             $"&refresh_token={_refreshToken}" +
                             $"&client_id={_clientId}";

            var cmd = $"curl -s -X POST https://accounts.spotify.com/api/token " +
                      $"-H \"Content-Type: application/x-www-form-urlencoded\" " +
                      $"-d \"{postFields}\"";

            var json = await ShellHelper.RunCommandAsync(cmd);
            var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();
            return accessToken;
        }


        public async Task<string> SearchFirstTrackUriAsync(string recognizedText, string accessToken)
        {
            var query = Uri.EscapeDataString(recognizedText);
            var url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=1";
            var cmd =
              $"curl -s -X GET \"{url}\" " +
              $"-H \"Authorization: Bearer {accessToken}\"";

            var json = await ShellHelper.RunCommandAsync(cmd);
            var doc = JsonDocument.Parse(json);
            var items = doc.RootElement
                .GetProperty("tracks")
                .GetProperty("items")
                .EnumerateArray();

            if (!items.MoveNext()) return null;
            return items.Current.GetProperty("uri").GetString();
        }

        public async Task PlayUriAsync(string trackUri, string accessToken)
        {
            var body = $"{{\"uris\":[\"{trackUri}\"]}}";
            var cmd =
              $"curl -s -X PUT https://api.spotify.com/v1/me/player/play " +
              $"-H \"Authorization: Bearer {accessToken}\" " +
              $"-H \"Content-Type: application/json\" " +
              $"-d \"{body}\"";

            await ShellHelper.RunCommandAsync(cmd);
        }

    }

}
