namespace FRELODYAPIs.Services.OgCard;

/// <summary>
/// Generates Open Graph snapshot artifacts (hero image + hero HTML) for share links.
/// Invoked at share-link creation time so crawlers can render a preview without
/// hitting authenticated endpoints or executing JavaScript.
/// </summary>
public interface IOgCardService
{
    /// <summary>
    /// Render a 1200×630 PNG card and write it under <c>wwwroot/share-og/{token}.png</c>.
    /// </summary>
    /// <returns>The wwwroot-relative path (e.g. <c>/share-og/abc.png</c>) or <c>null</c> on failure.</returns>
    Task<string?> RenderPngAsync(OgCardContent content, string shareToken, CancellationToken ct = default);

    /// <summary>Build the hero HTML fragment shown in the body of the landing page.</summary>
    string BuildHeroHtml(OgCardContent content);
}

public sealed record OgCardContent(
    OgCardKind Kind,
    string Title,
    string? Subtitle,
    string? Tagline,
    string? Meta); // e.g. "12 songs" for playlists, "#23 · Hymnal" for songs

public enum OgCardKind
{
    Song,
    Playlist
}
