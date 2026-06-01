namespace FRELODYSHRD.Dtos.DocsDtos
{
    /// <summary>
    /// One media slot's published state. A slot maps 1:1 to a placeholder in the docs
    /// markdown (keyed by <c>data-media-slot</c>). Either or both of <see cref="ImageUrl"/>
    /// and <see cref="VideoId"/> may be set; an empty entry renders the original placeholder.
    /// </summary>
    public class DocMediaEntryDto
    {
        /// <summary>Stable slot key, e.g. <c>discover-overview--1</c>. Matches the registry + markdown.</summary>
        public string SlotKey { get; set; } = string.Empty;

        /// <summary>Server-relative URL of the uploaded image, e.g. <c>/docs-media/discover-overview--1.webp</c>.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Bare 11-char YouTube id (extracted server-side from a URL or id).</summary>
        public string? VideoId { get; set; }

        /// <summary>Optional caption shown under the image/video.</summary>
        public string? Caption { get; set; }

        /// <summary>Last write time — also used by the docs site as an image cache-buster.</summary>
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    /// <summary>
    /// The full published media map. Persisted as a single <c>manifest.json</c> on the
    /// <c>frelody_media</c> volume and served anonymously so every docs visitor can render media.
    /// </summary>
    public class DocMediaManifestDto
    {
        public Dictionary<string, DocMediaEntryDto> Slots { get; set; } = new();
    }

    /// <summary>
    /// PUT body for setting a slot's video / caption. <see cref="VideoUrlOrId"/> accepts a full
    /// YouTube link (watch?v=, youtu.be/, /embed/) or a bare id; the server extracts the id.
    /// Send <c>null</c>/empty for a field to leave it unchanged is NOT assumed — both fields are
    /// applied as given (empty string clears).
    /// </summary>
    public class DocMediaTextUpdateDto
    {
        public string? VideoUrlOrId { get; set; }
        public string? Caption { get; set; }
    }
}
