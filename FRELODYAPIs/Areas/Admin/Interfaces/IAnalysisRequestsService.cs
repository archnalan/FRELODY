using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    /// <summary>
    /// Persistence + read model for blocked analysis requests and the superadmin's
    /// per-video whitelist. Recording is best-effort telemetry (never throws into the
    /// caller's request); the read/whitelist methods back the /admin/requests review.
    /// </summary>
    public interface IAnalysisRequestsService
    {
        /// <summary>
        /// Upserts a blocked request, aggregated per (Platform, VideoId, UserId, UTC day).
        /// Swallows its own errors so it can never fail the user-facing request it logs.
        /// </summary>
        Task RecordAsync(
            AnalyzedPlatform platform, string videoId, string reason,
            string? userId, string? userEmail, bool wasPremium,
            string? title, string? channelTitle, string? thumbnailUrl,
            string? sourceUrl, int? durationSeconds);

        /// <summary>True when the video has an active whitelist override (bypasses duration caps).</summary>
        Task<bool> IsWhitelistedAsync(AnalyzedPlatform platform, string videoId);

        /// <summary>Demand-ranked, reason-classified list of videos blocked at the gate.</summary>
        Task<ServiceResult<List<AnalysisRequestVideoDto>>> GetRequestsAsync();

        /// <summary>Videos a superadmin has approved to bypass the duration caps.</summary>
        Task<ServiceResult<List<WhitelistedVideoDto>>> GetWhitelistAsync();

        /// <summary>Whitelists (or updates) an over-long video so it can be analyzed.</summary>
        Task<ServiceResult<bool>> ApproveVideoAsync(WhitelistVideoRequestDto request);

        /// <summary>Removes a video's whitelist override (re-applies the duration caps).</summary>
        Task<ServiceResult<bool>> RemoveWhitelistAsync(AnalyzedPlatform platform, string videoId);

        /// <summary>Daily success-vs-denied series for the admin outcome chart.</summary>
        Task<ServiceResult<AnalysisOutcomeStatsDto>> GetOutcomeStatsAsync(int days = 30);
    }
}
