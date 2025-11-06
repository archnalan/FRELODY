using Refit;
using FRELODYSHRD.Models.OpenExchange;

namespace FRELODYAPIs.ExternalCalls
{
    public interface IOpenExchangeApi
    {
        [Get("/latest.json")]
        Task<IApiResponse<RateResponseDto>> GetLatestRates([Query] string appId);
    }
}
