namespace FRELODY.Docs.Models;

/// <summary>
/// Docs-side mirror of the API's media manifest shapes. Deliberately a local copy (not a
/// FRELODYSHRD reference) — that project pulls heavy PDF/Office libraries unfit for a WASM bundle.
/// Matches the JSON the API serialises (camelCase, case-insensitive on read).
/// </summary>
public sealed class DocMediaManifest
{
    public Dictionary<string, DocMediaEntry> Slots { get; set; } = new();
}

public sealed class DocMediaEntry
{
    public string SlotKey { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? VideoId { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public bool HasVideo => !string.IsNullOrWhiteSpace(VideoId);
}
