using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    /// <summary>
    /// Authoritative server-side gate for the analyzed-song daily quota and 24h
    /// availability window. The metering unit is a distinct (Platform, VideoId)
    /// unlock per user; re-playing within the window is free.
    /// </summary>
    public interface IAnalyzedAccessService
    {
        /// <summary>
        /// Evaluates whether the current user may play an analysis-flow song and,
        /// when a new slot is consumed, records the unlock. Premium users bypass the
        /// quota. Idempotent within the availability window. Honors the enforcement
        /// flag: when enforcement is off, <see cref="AnalyzedAccessResultDto.Allowed"/>
        /// is always true but the result still reports quota usage.
        /// </summary>
        Task<ServiceResult<AnalyzedAccessResultDto>> EvaluateAndRecord(
            AnalyzedPlatform platform, string videoId, string? title = null,
            string? thumbnailUrl = null, string? sourceUrl = null, int? durationSeconds = null);

        /// <summary>
        /// Refunds a slot consumed by <see cref="EvaluateAndRecord"/> when the
        /// downstream analysis fails (e.g. a YouTube bot-wall). Removes today's
        /// unlock for the current user + (platform, videoId) so the song no longer
        /// counts against the daily quota. Safe no-op when nothing was recorded.
        /// </summary>
        Task<ServiceResult<bool>> ReleaseUnlock(AnalyzedPlatform platform, string videoId);

        /// <summary>Read-only quota snapshot for the current user (no slot consumed).</summary>
        Task<ServiceResult<AnalyzedAccessResultDto>> GetQuotaStatus();

        /// <summary>
        /// Records a request blocked at the client duration pre-gate for the superadmin
        /// review, and reports whether the video has been whitelisted (so the client may
        /// analyze it despite its length). Safe for anonymous callers; only known reason
        /// codes are persisted.
        /// </summary>
        Task<ServiceResult<BlockedRequestReportResultDto>> ReportBlockedRequestAsync(
            BlockedRequestReportDto request);

        /// <summary>Public monetization limits (no auth) for client-side pre-gating.</summary>
        AnalyzedLimitsDto GetLimits();

        /// <summary>Analyzed songs the current user unlocked that are still within their availability window.</summary>
        Task<ServiceResult<List<AnalyzedSongDto>>> GetTodaysSongs();

        /// <summary>Most-unlocked analyzed songs across all users, for the Explore discovery strip.</summary>
        Task<ServiceResult<List<PopularAnalyzedSongDto>>> GetPopularSongs(int count = 12);

        /// <summary>
        /// Last 7 days of the user's song history, with accessibility flags, quota info,
        /// practice streak, and a 30-day daily activity map for the calendar heatmap.
        /// </summary>
        Task<ServiceResult<SongHistoryDto>> GetSongHistory();
    }
}
