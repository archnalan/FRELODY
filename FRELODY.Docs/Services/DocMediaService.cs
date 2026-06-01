using System.Net.Http.Headers;
using System.Net.Http.Json;
using FRELODY.Docs.Models;

namespace FRELODY.Docs.Services;

/// <summary>
/// Docs-side client for the API's documentation-media endpoints. Reads the published manifest
/// (anonymous) and, for SuperAdmins, uploads images / sets videos. Holds the manifest in memory
/// and exposes a <see cref="Version"/> that bumps on every load/write so cached rendered pages
/// can be invalidated.
/// </summary>
public class DocMediaService
{
    private readonly HttpClient _api;
    private readonly AuthService _auth;
    private readonly string _apiBase;

    private Dictionary<string, DocMediaEntry> _slots = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    /// <summary>Increments on each successful manifest load or write.</summary>
    public int Version { get; private set; }

    public DocMediaService(HttpClient api, AuthService auth, string apiBaseUrl)
    {
        _api = api;
        _auth = auth;
        _apiBase = (apiBaseUrl ?? string.Empty).TrimEnd('/');
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            var manifest = await _api.GetFromJsonAsync<DocMediaManifest>("api/docmedia/getmanifest");
            _slots = new Dictionary<string, DocMediaEntry>(
                manifest?.Slots ?? new(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Best effort: if the API is unreachable, render placeholders rather than fail the page.
            _slots = new(StringComparer.OrdinalIgnoreCase);
        }
        _loaded = true;
        Version++;
    }

    public DocMediaEntry? Get(string key) =>
        key is not null && _slots.TryGetValue(key, out var e) ? e : null;

    public IReadOnlyDictionary<string, DocMediaEntry> Slots => _slots;

    /// <summary>Absolute, cache-busted URL for an entry's image (the API is a different origin).</summary>
    public string? AbsoluteImageUrl(DocMediaEntry? entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.ImageUrl)) return null;
        var bust = entry.UpdatedAt?.ToUnixTimeSeconds().ToString() ?? "1";
        return $"{_apiBase}{entry.ImageUrl}?v={bust}";
    }

    // ── SuperAdmin writes ────────────────────────────────────────────────────

    public async Task<DocMediaEntry?> UploadImageAsync(string slot, Stream content, string fileName, string contentType)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(slot), "slot");
        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/docmedia/uploadimage") { Content = form };
        return await SendAndStoreAsync(req);
    }

    public async Task<DocMediaEntry?> SetTextAsync(string slot, string? videoUrlOrId, string? caption)
    {
        var body = new { videoUrlOrId, caption };
        using var req = new HttpRequestMessage(HttpMethod.Put, $"api/docmedia/settext?slot={Uri.EscapeDataString(slot)}")
        {
            Content = JsonContent.Create(body)
        };
        return await SendAndStoreAsync(req);
    }

    public async Task<DocMediaEntry?> ClearAsync(string slot, string kind)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete,
            $"api/docmedia/clear?slot={Uri.EscapeDataString(slot)}&kind={Uri.EscapeDataString(kind)}");
        return await SendAndStoreAsync(req);
    }

    private async Task<DocMediaEntry?> SendAndStoreAsync(HttpRequestMessage req)
    {
        var token = _auth.Current?.Token;
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _api.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync();
            throw new DocMediaException((int)resp.StatusCode,
                string.IsNullOrWhiteSpace(detail) ? resp.ReasonPhrase ?? "Request failed" : detail);
        }

        var entry = await resp.Content.ReadFromJsonAsync<DocMediaEntry>();
        if (entry is not null)
        {
            _slots[entry.SlotKey] = entry;
            Version++;
        }
        return entry;
    }
}

public sealed class DocMediaException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
