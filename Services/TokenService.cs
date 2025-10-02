using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SpotifyAndFeel.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Diagnostics;

namespace SpotifyAndFeel.Services
{
    public class TokenService
    {
        private readonly SpotifyConfig _config;

        public TokenService(SpotifyConfig config)
        {
            _config = config;


        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {



            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://accounts.spotify.com/api/");

            // ➊ Authorization header: Base64(clientId:clientSecret)
            var authHeader = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authHeader);

            // ➋ Form verisi
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };
            using var content = new FormUrlEncodedContent(form);

            // ➌ İsteği yap
            var res = await client.PostAsync("token", content);
            var body = await res.Content.ReadAsStringAsync();

            // ➍ HTTP durum kodunu ve gövdeyi konsola yaz
            Debug.WriteLine($"[TokenService] POST /token");
            Debug.WriteLine($"[TokenService] Authorization: Basic {authHeader}");
            Debug.WriteLine($"[TokenService] Form: code={code}, redirect_uri={redirectUri}");
            Debug.WriteLine($"[TokenService] HTTP {(int)res.StatusCode}: {res.StatusCode}");
            Debug.WriteLine($"[TokenService] Body: {body}");

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Token API hata {(int)res.StatusCode}: {body}");

            // ➎ JSON’dan objeyi çıkar
            var token = JsonSerializer.Deserialize<TokenResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // ➏ Erişimi kontrol et
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new InvalidOperationException(
                    "TokenResponse içinde access_token bulunamadı");

            return token;


        }
    }
}