using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Services.ChordMini
{
    public interface IChordMiniService
    {
        Task<YouTubeTranscriptionDto> AnalyzeAsync(
            YouTubeAnalyzeRequest request,
            IProgress<AnalysisStage>? progress = null,
            CancellationToken ct = default);

        /// <summary>Analyze any yt-dlp-supported media URL (e.g. TikTok) by URL.</summary>
        Task<YouTubeTranscriptionDto> AnalyzeUrlAsync(
            string url, string idForResult,
            string beatModel, string chordModel, string chordDict,
            IProgress<AnalysisStage>? progress = null,
            CancellationToken ct = default);

        /// <summary>Fetch lightweight metadata (no download) for a media URL.</summary>
        Task<MediaInfo> GetInfoAsync(string url, CancellationToken ct = default);

        /// <summary>
        /// Look up time-synchronized lyrics (LRCLib via the ChordMini backend). Prefers an
        /// artist+title pair; falls back to a free-text search query. Never throws — a miss
        /// or backend failure returns <c>Found == false</c> so callers can degrade gracefully.
        /// </summary>
        Task<LyricsResult> GetLyricsAsync(string? artist, string? title, string? searchQuery, CancellationToken ct = default);
    }

    public sealed record LyricsLine(string Text, double Time);

    public sealed record LyricsResult(
        bool Found,
        bool HasSynchronized,
        double? DurationSeconds,
        IReadOnlyList<LyricsLine> Synchronized,
        string? PlainLyrics);

    public sealed record MediaInfo(
        string Id, string Title, string? Uploader,
        string? Thumbnail, int DurationSeconds, string WebpageUrl,
        int? Width = null, int? Height = null);
}
