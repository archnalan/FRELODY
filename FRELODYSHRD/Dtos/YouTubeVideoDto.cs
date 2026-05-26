namespace FRELODYSHRD.Dtos
{
    public class YouTubeVideoDto
    {
        public string VideoId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int DurationSeconds { get; set; }
        public string YouTubeUrl => $"https://www.youtube.com/watch?v={VideoId}";
    }
}
