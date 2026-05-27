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
    }
}
