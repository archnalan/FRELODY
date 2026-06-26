using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    /// <summary>
    /// Superadmin client for the analysis-requests review: the demand-ranked,
    /// reason-classified list of videos blocked at the access gate, the success-vs-denied
    /// outcome chart series, and the per-video whitelist that bypasses the duration caps.
    /// </summary>
    public interface IAnalysisRequestsApi
    {
        [Get("/api/analysis-requests/get-requests")]
        Task<IApiResponse<List<AnalysisRequestVideoDto>>> GetRequests();

        [Get("/api/analysis-requests/get-whitelist")]
        Task<IApiResponse<List<WhitelistedVideoDto>>> GetWhitelist();

        [Get("/api/analysis-requests/get-outcome-stats")]
        Task<IApiResponse<AnalysisOutcomeStatsDto>> GetOutcomeStats(int days = 30);

        [Post("/api/analysis-requests/approve-video")]
        Task<IApiResponse<bool>> ApproveVideo([Body] WhitelistVideoRequestDto request);

        [Post("/api/analysis-requests/remove-whitelist")]
        Task<IApiResponse<bool>> RemoveWhitelist([Body] WhitelistVideoRequestDto request);
    }
}
