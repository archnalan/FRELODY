using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FRELODYAPIs.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FRELODYAPIs.Services.PayPal
{
    /// <summary>Thin client over the PayPal Orders v2 REST API (one-time checkout).</summary>
    public class PayPalClient
    {
        private const string TokenCacheKey = "paypal_access_token";

        private readonly HttpClient _http;
        private readonly PayPalOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PayPalClient> _logger;

        public PayPalClient(HttpClient http, IOptions<PayPalOptions> options, IMemoryCache cache, ILogger<PayPalClient> logger)
        {
            _http = http;
            _options = options.Value;
            _cache = cache;
            _logger = logger;
            _http.BaseAddress ??= new Uri(_options.ApiBaseUrl);
        }

        public sealed record CaptureOutcome(string Status, string? CaptureId, string Raw);

        private async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
                return cached!;

            using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                })
            };
            var basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayPal auth failed ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3000;

            _cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60)));
            return token;
        }

        public async Task<string> CreateOrderAsync(
            decimal amount, string currency, string customId, string description, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync(ct);

            var payload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        custom_id = customId,
                        description = Trim(description, 127),
                        amount = new
                        {
                            currency_code = currency,
                            value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders")
            {
                Content = JsonContent(payload)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayPal create-order failed ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("id").GetString()!;
        }

        public async Task<CaptureOutcome> CaptureOrderAsync(string orderId, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync(ct);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{orderId}/capture")
            {
                // PayPal requires a (possibly empty) JSON body for capture.
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayPal capture failed ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";

            string? captureId = null;
            if (doc.RootElement.TryGetProperty("purchase_units", out var pus) && pus.GetArrayLength() > 0
                && pus[0].TryGetProperty("payments", out var pay)
                && pay.TryGetProperty("captures", out var caps) && caps.GetArrayLength() > 0)
            {
                captureId = caps[0].TryGetProperty("id", out var cid) ? cid.GetString() : null;
            }

            return new CaptureOutcome(status, captureId, body);
        }

        private static StringContent JsonContent(object payload) =>
            new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        private static string Trim(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
    }
}
