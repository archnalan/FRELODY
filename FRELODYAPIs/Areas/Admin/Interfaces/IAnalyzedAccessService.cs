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

        /// <summary>Read-only quota snapshot for the current user (no slot consumed).</summary>
        Task<ServiceResult<AnalyzedAccessResultDto>> GetQuotaStatus();

        /// <summary>Public monetization limits (no auth) for client-side pre-gating.</summary>
        AnalyzedLimitsDto GetLimits();

        /// <summary>Analyzed songs the current user unlocked that are still within their availability window.</summary>
        Task<ServiceResult<List<AnalyzedSongDto>>> GetTodaysSongs();
    }
}
