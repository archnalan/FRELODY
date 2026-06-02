using Refit;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IPaymentsApi
    {
        // Format "o" → ISO-8601 round-trip (2026-05-03T20:54:05.0000000+00:00) so the
        // server's DateTimeOffset model binder parses it unambiguously. Without this
        // Refit emits the culture-default "05/03/2026 20:54:05 +00:00" which is fragile.
        [Get("/api/payments/get-revenue-stats")]
        Task<IApiResponse<RevenueStatsDto>> GetRevenueStats(
            [Query(Format = "o")] DateTimeOffset from,
            [Query(Format = "o")] DateTimeOffset to);
    }
}
