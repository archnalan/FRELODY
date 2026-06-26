using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    /// <summary>
    /// Client for the analyzed-song money loop: unlock (consume a daily slot),
    /// read the remaining quota, and list today's still-available unlocks.
    /// </summary>
    public interface IAnalyzedAccessApi
    {
        [Post("/api/analyzed-access/unlock")]
        Task<IApiResponse<AnalyzedAccessResultDto>> Unlock([Body] AnalyzedAccessRequest request);

        [Get("/api/analyzed-access/limits")]
        Task<IApiResponse<AnalyzedLimitsDto>> Limits();

        [Post("/api/analyzed-access/report-blocked")]
        Task<IApiResponse<BlockedRequestReportResultDto>> ReportBlocked([Body] BlockedRequestReportDto request);

        [Get("/api/analyzed-access/quota-status")]
        Task<IApiResponse<AnalyzedAccessResultDto>> QuotaStatus();

        [Get("/api/analyzed-access/todays-songs")]
        Task<IApiResponse<List<AnalyzedSongDto>>> TodaysSongs();

        [Get("/api/analyzed-access/song-history")]
        Task<IApiResponse<SongHistoryDto>> SongHistory();
    }
}
