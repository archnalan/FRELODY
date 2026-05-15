using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Parser;
using FRELODYSHRD.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FRELODYAPIs.Services.WebSong
{
    public interface IWebSongExtractionService
    {
        Task<WebSongFetchResult> FetchAsync(string url, CancellationToken ct = default);
    }

    public sealed class WebSongExtractionOptions
    {
        public const string SectionName = "WebSongExtraction";

        /// <summary>Hosts allowed to be fetched server-side. Sub-domains are matched.</summary>
        public List<string> AllowedHosts { get; set; } = new() { "bradwarden.com" };

        /// <summary>Max response payload accepted from a remote server (bytes).</summary>
        public int MaxResponseBytes { get; set; } = 2 * 1024 * 1024;

        /// <summary>HTTP timeout for a single fetch.</summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>Cache TTL for successful fetches.</summary>
        public int CacheMinutes { get; set; } = 60 * 24;
    }

    public sealed class WebSongExtractionService : IWebSongExtractionService
    {
        public const string HttpClientName = "WebSongFetcher";

        // Real Chrome fingerprint — many lyric/chord sites (e.g. Ultimate Guitar) return 403
        // when the User-Agent looks like a generic bot or omits the Client Hints headers.
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<IWebSongSource> _sources;
        private readonly UrlSafetyValidator _safety;
        private readonly IMemoryCache _cache;
        private readonly WebSongExtractionOptions _options;
        private readonly ILogger<WebSongExtractionService> _logger;

        public WebSongExtractionService(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IWebSongSource> sources,
            UrlSafetyValidator safety,
            IMemoryCache cache,
            IOptions<WebSongExtractionOptions> options,
            ILogger<WebSongExtractionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _sources = sources.OrderBy(s => s.Priority).ToList();
            _safety = safety;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<WebSongFetchResult> FetchAsync(string url, CancellationToken ct = default)
        {
            var safety = await _safety.ValidateAsync(url, _options.AllowedHosts, ct);
            if (!safety.IsAllowed || safety.Uri is null)
            {
                return new WebSongFetchResult
                {
                    SourceUrl = url ?? string.Empty,
                    IsSupported = false,
                    RequiresWebSearchFallback = safety.IsUnsupportedHost,
                    Message = safety.Reason
                };
            }

            var uri = safety.Uri;
            var cacheKey = "websong:" + uri.AbsoluteUri;

            if (_cache.TryGetValue(cacheKey, out WebSongFetchResult? cached) && cached is not null)
                return cached;

            string html;
            try
            {
                html = await DownloadHtmlAsync(uri, ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout fetching {Url}", uri);
                return new WebSongFetchResult
                {
                    SourceUrl = uri.ToString(),
                    SourceHost = uri.Host,
                    IsSupported = false,
                    Message = "The remote server took too long to respond."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fetch failed for {Url}", uri);
                return new WebSongFetchResult
                {
                    SourceUrl = uri.ToString(),
                    SourceHost = uri.Host,
                    IsSupported = false,
                    Message = "Could not fetch the page."
                };
            }

            // Parse with AngleSharp (no scripting, no external resource loading — text only).
            var browsing = BrowsingContext.New(Configuration.Default);
            var parser = browsing.GetService<IHtmlParser>()!;
            var document = await parser.ParseDocumentAsync(html, ct);

            var source = _sources.First(s => s.CanHandle(uri));
            var result = source.Extract(uri, document);

            if (result.IsSupported)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheMinutes));
            }

            return result;
        }

        private async Task<string> DownloadHtmlAsync(Uri uri, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            // Client Hints + Sec-Fetch-* — required by Cloudflare/Akamai bot filters used by
            // sites like tabs.ultimate-guitar.com. TryAddWithoutValidation is needed because
            // these are restricted/non-standard request headers.
            request.Headers.TryAddWithoutValidation(
                "sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"131\", \"Google Chrome\";v=\"131\"");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "none");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            // Use ResponseHeadersRead so we can validate Content-Length and bail out early on huge payloads.
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) &&
                !contentType.StartsWith("application/xhtml", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(contentType))
            {
                throw new InvalidOperationException($"Unsupported content type: {contentType}");
            }

            var declared = response.Content.Headers.ContentLength;
            if (declared.HasValue && declared.Value > _options.MaxResponseBytes)
                throw new InvalidOperationException("Remote response exceeds the maximum allowed size.");

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var ms = new MemoryStream();

            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                if (ms.Length + read > _options.MaxResponseBytes)
                    throw new InvalidOperationException("Remote response exceeds the maximum allowed size.");
                ms.Write(buffer, 0, read);
            }

            // Default to UTF-8; AngleSharp will re-detect from <meta charset> if needed.
            var encoding = System.Text.Encoding.UTF8;
            try
            {
                var charset = response.Content.Headers.ContentType?.CharSet;
                if (!string.IsNullOrWhiteSpace(charset))
                    encoding = System.Text.Encoding.GetEncoding(charset.Trim('"'));
            }
            catch { /* fall back to utf-8 */ }

            return encoding.GetString(ms.ToArray());
        }
    }
}
