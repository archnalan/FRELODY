namespace FRELODYSHRD.Dtos
{
    public class TikTokVideoDto
    {
        public string VideoId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Uploader { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int DurationSeconds { get; set; }
        public string Url { get; set; } = default!;

        // Native pixel dimensions (when yt-dlp reports them) so the player stage
        // can match the real aspect ratio instead of letterboxing into a fixed box.
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
