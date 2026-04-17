using System.Net;
using System.Text;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FRELODYAPIs.Controllers;

/// <summary>
/// Serves crawler-friendly HTML for shared songs / playlists. Emits Open Graph
/// + Twitter Card meta tags from the snapshot stored on <see cref="ShareLink"/>
/// so WhatsApp, iMessage, Facebook, Twitter, LinkedIn, Slack, Discord etc.
/// render a rich preview without executing JavaScript. Real browsers are
/// redirected into the Blazor landing routes via a meta-refresh fallback.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ShareLandingController : Controller
{
    private readonly SongDbContext _context;
    private readonly ShareLandingOptions _options;
    private readonly ILogger<ShareLandingController> _logger;

    public ShareLandingController(
        SongDbContext context,
        IOptions<ShareLandingOptions> options,
        ILogger<ShareLandingController> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("/shared/{token}")]
    public Task<IActionResult> Song(string token, CancellationToken ct) =>
        RenderAsync(token, isPlaylist: false, ct);

    [HttpGet("/shared/playlist/{token}")]
    public Task<IActionResult> Playlist(string token, CancellationToken ct) =>
        RenderAsync(token, isPlaylist: true, ct);

    private async Task<IActionResult> RenderAsync(string token, bool isPlaylist, CancellationToken ct)
    {
        var link = await _context.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.ShareToken == token, ct);

        var canonicalUrl = BuildCanonicalUrl(isPlaylist, token);
        var redirectUrl = BuildRedirectUrl(link, isPlaylist);

        var html = BuildHtml(link, isPlaylist, canonicalUrl, redirectUrl);

        Response.Headers["Cache-Control"] = "public, max-age=300";
        return Content(html, "text/html", Encoding.UTF8);
    }

    private string BuildCanonicalUrl(bool isPlaylist, string token)
    {
        var origin = !string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? _options.PublicBaseUrl!.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";
        return isPlaylist ? $"{origin}/shared/playlist/{token}" : $"{origin}/shared/{token}";
    }

    private string BuildRedirectUrl(ShareLink? link, bool isPlaylist)
    {
        // Prefer the configured app origin (Blazor UI). When unset, fall back to
        // the public share origin (single-origin deployments) and finally to the
        // API's own request origin (dev default).
        var appOrigin = (!string.IsNullOrWhiteSpace(_options.AppBaseUrl)
                ? _options.AppBaseUrl
                : !string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
                    ? _options.PublicBaseUrl
                    : $"{Request.Scheme}://{Request.Host}")!
            .TrimEnd('/');

        if (link is null) return appOrigin;

        if (isPlaylist && !string.IsNullOrEmpty(link.PlaylistId))
            return $"{appOrigin}/playlists/landing/{link.PlaylistId}/detail";

        if (!isPlaylist && !string.IsNullOrEmpty(link.SongId))
            return $"{appOrigin}/songs/landing/{link.SongId}";

        return appOrigin;
    }

    private string BuildHtml(ShareLink? link, bool isPlaylist, string canonicalUrl, string redirectUrl)
    {
        var expired = link?.ExpiresAt.HasValue == true && link.ExpiresAt.Value < DateTime.UtcNow;
        var notFound = link is null || expired;

        var title = notFound
            ? (expired ? "This share link has expired" : "Shared content not found")
            : (link!.OgTitle ?? (isPlaylist ? "Frelody Playlist" : "Frelody Song"));

        var description = notFound
            ? "Open Frelody to discover songs, chords and playlists."
            : (link!.OgDescription ?? "Shared via Frelody");

        var imageUrl = BuildImageUrl(link);

        var heroHtml = !notFound && !string.IsNullOrWhiteSpace(link!.OgHtml)
            ? link.OgHtml!
            : BuildFallbackHero(title, description);

        var ogType = isPlaylist ? "music.playlist" : "music.song";
        var siteName = _options.SiteName ?? "Frelody";

        var sb = new StringBuilder(4096);
        sb.Append("<!DOCTYPE html>\n");
        sb.Append("<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">\n");
        sb.Append("<title>").Append(HtmlEncode(title)).Append(" · ").Append(HtmlEncode(siteName)).Append("</title>\n");
        sb.Append("<meta name=\"description\" content=\"").Append(AttrEncode(description)).Append("\">\n");
        sb.Append("<link rel=\"canonical\" href=\"").Append(AttrEncode(canonicalUrl)).Append("\">\n");

        // Open Graph
        AppendMeta(sb, "og:type", ogType, isProperty: true);
        AppendMeta(sb, "og:site_name", siteName, isProperty: true);
        AppendMeta(sb, "og:url", canonicalUrl, isProperty: true);
        AppendMeta(sb, "og:title", title, isProperty: true);
        AppendMeta(sb, "og:description", description, isProperty: true);
        if (!string.IsNullOrEmpty(imageUrl))
        {
            AppendMeta(sb, "og:image", imageUrl, isProperty: true);
            AppendMeta(sb, "og:image:secure_url", imageUrl, isProperty: true);
            AppendMeta(sb, "og:image:type", "image/png", isProperty: true);
            AppendMeta(sb, "og:image:width", "1200", isProperty: true);
            AppendMeta(sb, "og:image:height", "630", isProperty: true);
            AppendMeta(sb, "og:image:alt", title, isProperty: true);
        }

        // Twitter Card
        AppendMeta(sb, "twitter:card", string.IsNullOrEmpty(imageUrl) ? "summary" : "summary_large_image");
        AppendMeta(sb, "twitter:title", title);
        AppendMeta(sb, "twitter:description", description);
        if (!string.IsNullOrEmpty(imageUrl))
            AppendMeta(sb, "twitter:image", imageUrl);

        // Human redirect (crawlers ignore). Kept after all meta so scrapers that
        // stop at the first refresh still have the preview data.
        if (!notFound)
        {
            sb.Append("<meta http-equiv=\"refresh\" content=\"0;url=").Append(AttrEncode(redirectUrl)).Append("\">\n");
        }

        // Base styles for the inline hero so the page looks acceptable if a
        // human lands here and JS/redirect is disabled.
        sb.Append("<style>body{margin:0;background:#0f1020;color:#fff;min-height:100vh;display:flex;align-items:center;justify-content:center;}a{color:#ff80ab;}</style>\n");
        sb.Append("</head>\n<body>\n");
        sb.Append(heroHtml);
        if (!notFound)
        {
            sb.Append("\n<noscript><p style=\"text-align:center\"><a href=\"")
              .Append(AttrEncode(redirectUrl)).Append("\">Open in Frelody →</a></p></noscript>\n");
            sb.Append("<script>window.location.replace(").Append(System.Text.Json.JsonSerializer.Serialize(redirectUrl)).Append(");</script>\n");
        }
        sb.Append("</body>\n</html>\n");

        return sb.ToString();
    }

    private string? BuildImageUrl(ShareLink? link)
    {
        if (link is null || string.IsNullOrWhiteSpace(link.OgImagePath)) return null;

        var path = link.OgImagePath.StartsWith('/') ? link.OgImagePath : "/" + link.OgImagePath;
        // Crawlers require absolute URLs for og:image. Prefer the configured
        // public origin so scrapers (WhatsApp, iMessage, …) can always reach the
        // static PNG even when the API is behind a reverse proxy.
        var origin = !string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? _options.PublicBaseUrl!.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";
        return $"{origin}{path}";
    }

    private static string BuildFallbackHero(string title, string description) =>
        $"""
        <div style="max-width:640px;padding:48px 32px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;text-align:center;">
          <h1 style="font-size:2rem;margin:0 0 12px 0;">{HtmlEncode(title)}</h1>
          <p style="opacity:0.75;">{HtmlEncode(description)}</p>
        </div>
        """;

    private static void AppendMeta(StringBuilder sb, string name, string content, bool isProperty = false)
    {
        sb.Append("<meta ").Append(isProperty ? "property" : "name").Append("=\"")
          .Append(name).Append("\" content=\"").Append(AttrEncode(content)).Append("\">\n");
    }

    private static string HtmlEncode(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
    private static string AttrEncode(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
}

public sealed class ShareLandingOptions
{
    public const string SectionName = "ShareLanding";

    /// <summary>
    /// User-facing origin where share links live (e.g. <c>https://frelody.com</c>).
    /// This is the origin embedded in the URL copied to the clipboard — it MUST
    /// be an origin that reaches this controller (either directly or proxied).
    /// When not set, the API falls back to its own request origin.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Origin of the Blazor UI where human visitors are redirected after the
    /// OG meta has been served (e.g. <c>https://frelody.com</c>). Defaults to
    /// <see cref="PublicBaseUrl"/> when not set (prod / docker — single origin).
    /// Split out only for local dev where UI (7018) and API (7077) differ.
    /// </summary>
    public string? AppBaseUrl { get; set; }

    /// <summary>Site name used for og:site_name.</summary>
    public string? SiteName { get; set; } = "Frelody";
}
