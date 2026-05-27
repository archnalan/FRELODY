using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Services.ChordMini
{
    public interface IChordMiniService
    {
        Task<YouTubeTranscriptionDto> AnalyzeAsync(YouTubeAnalyzeRequest request, CancellationToken ct = default);

        /// <summary>Analyze any yt-dlp-supported media URL (e.g. TikTok) by URL.</summary>
        Task<YouTubeTranscriptionDto> AnalyzeUrlAsync(
            string url, string idForResult,
            string beatModel, string chordModel, string chordDict,
            CancellationToken ct = default);

        /// <summary>Fetch lightweight metadata (no download) for a media URL.</summary>
        Task<MediaInfo> GetInfoAsync(string url, CancellationToken ct = default);
    }

    public sealed record MediaInfo(
        string Id, string Title, string? Uploader,
        string? Thumbnail, int DurationSeconds, string WebpageUrl,
        int? Width = null, int? Height = null);
}
