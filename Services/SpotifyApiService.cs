using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpotifyAndFeel.Services
{
    public class SpotifyApiService
    {
        private readonly HttpClient _client;
        public event Action<string, string, int> ToastRequested;

        public SpotifyApiService(string accessToken)
        {

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be empty.", nameof(accessToken));

            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            Debug.WriteLine($"[SpotifyApiService] Authorization_Header: {_client.DefaultRequestHeaders.Authorization}");

            RaiseToast("Spotify API initialized successfully.", "#1DB954");
        }

        private void RaiseToast(string message, string colorHex = "#1DB954", int durationMs = 2500)
        {
            ToastRequested?.Invoke(message, colorHex, durationMs);
        }

        public async Task<string> SearchTrackAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search text cannot be empty.", nameof(query));

            query = query.Length <= 100 ? query : query.Substring(0, 100);

            var url = $"search?q={Uri.EscapeDataString(query)}&type=track&limit=5";

            using var res = await _client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                RaiseToast($"Spotify API Error: {res.StatusCode}", "#E53935");
                throw new InvalidOperationException($"Search API error {res.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var items = doc.RootElement
                           .GetProperty("tracks")
                           .GetProperty("items")
                           .EnumerateArray()
                           .ToList();

            if (items.Count == 0)
            {
                Debug.WriteLine("[SpotifyApiService] No results found.");
                RaiseToast("No tracks found for this query.", "#FFB300");
                return null;
            }

            Debug.WriteLine($"[SpotifyApiService] Found {items.Count} results for \"{query}\".");

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

            var first = items.First();
            string firstUri = first.GetProperty("uri").GetString();

            string firstTrack = first.GetProperty("name").GetString();
            string firstArtist = first.GetProperty("artists")[0].GetProperty("name").GetString();

            Debug.WriteLine($"[SpotifyApiService] Selected: {firstTrack}");
            RaiseToast($"Found: {firstTrack} by {firstArtist}", "#1DB954");

            return firstUri;
        }

        public async Task PlayTrackAsync(string trackUri)
        {
            using var devicesRes = await _client.GetAsync("me/player/devices");
            devicesRes.EnsureSuccessStatusCode();

            using var devDoc = JsonDocument.Parse(await devicesRes.Content.ReadAsStringAsync());
            var devices = devDoc.RootElement.GetProperty("devices").EnumerateArray();
            var firstDevice = devices.FirstOrDefault();

            if (firstDevice.ValueKind == JsonValueKind.Undefined)
            {
                RaiseToast("No active Spotify devices found.", "#E53935");
                throw new InvalidOperationException("No active devices found.");
            }

            string deviceId = firstDevice.GetProperty("id").GetString();

            var body = new { uris = new[] { trackUri } };
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var playUrl = $"me/player/play?device_id={deviceId}";
            using var playRes = await _client.PutAsync(playUrl, content);
            playRes.EnsureSuccessStatusCode();

            RaiseToast("Playing on your Spotify device 🎧", "#1DB954");
        }
    }
}
