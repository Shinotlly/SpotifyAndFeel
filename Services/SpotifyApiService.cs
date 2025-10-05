using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SpotifyAndFeel.Services
{
    public class SpotifyApiService
    {
        private readonly HttpClient _client;

        public SpotifyApiService(string accessToken)
        {

            MessageBox.Show("SpotifyApiService ctor tetiklendi!");

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token boş olamaz.", nameof(accessToken));

            Debug.WriteLine($"[SpotifyApiService] Bearer token başlığı: Bearer {accessToken.Substring(0, 10)}…");

            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };

            // ➊ Authorization header ekle
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            // ➋ Doğru ayarlanıp ayarlanmadığını kontrol etmek için
            Debug.WriteLine(
                $"[SpotifyApiService] Authorization_Header: {_client.DefaultRequestHeaders.Authorization}");
        }


        // 1. Arama ve ilk çıkan track’in Spotify URI’sini döner
        public async Task<string> SearchTrackAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Arama metni boş olamaz.", nameof(query));

            query = query.Length <= 100 ? query : query.Substring(0, 100);

            // 🔹 5 sonuç getir
            var url = $"search?q={Uri.EscapeDataString(query)}&type=track&limit=5";

            using var res = await _client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Search API hata {res.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var items = doc.RootElement
                           .GetProperty("tracks")
                           .GetProperty("items")
                           .EnumerateArray()
                           .ToList();

            if (items.Count == 0)
            {
                Debug.WriteLine("[SpotifyApiService] Aramada hiçbir sonuç bulunamadı.");
                return null;
            }

            Debug.WriteLine($"[SpotifyApiService] \"{query}\" için bulunan ilk {items.Count} sonuç:");

            int index = 1;
            foreach (var item in items)
            {
                string trackName = item.GetProperty("name").GetString();
                string artistName = item.GetProperty("artists")[0].GetProperty("name").GetString();
                int popularity = item.TryGetProperty("popularity", out var popElem) ? popElem.GetInt32() : -1;
                string uri = item.GetProperty("uri").GetString();

                Debug.WriteLine($"  {index}. {artistName} - {trackName} (Popularity: {popularity})");
                Debug.WriteLine($"     URI: {uri}");
                index++;
            }

            // 🔹 Sadece ilk sonucu döndür (Spotify’ın döndürdüğü en alakalı şarkı)
            var first = items.First();
            string firstUri = first.GetProperty("uri").GetString();

            Debug.WriteLine($"[SpotifyApiService] En üstteki sonuç seçildi: {first.GetProperty("name").GetString()}");

            return firstUri;
        }



        // 2. Aktif cihazları listeler ve ilkine URI listesiyle oynatma komutu yollar
        public async Task PlayTrackAsync(string trackUri)
        {
            // 2.1. Cihazları çek
            using var devicesRes = await _client.GetAsync("me/player/devices");
            devicesRes.EnsureSuccessStatusCode();

            using var devDoc = JsonDocument.Parse(
                await devicesRes.Content.ReadAsStringAsync());
            var devices = devDoc.RootElement.GetProperty("devices").EnumerateArray();
            var firstDevice = devices.FirstOrDefault();

            if (firstDevice.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException("Hiçbir aktif cihaz bulunamadı.");

            string deviceId = firstDevice.GetProperty("id").GetString();

            // 2.2. Oynatma isteği gövdesi
            var body = new { uris = new[] { trackUri } };
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 2.3. Play endpoint’ine PUT isteği
            var playUrl = $"me/player/play?device_id={deviceId}";
            using var playRes = await _client.PutAsync(playUrl, content);
            playRes.EnsureSuccessStatusCode();
        }
    }
}