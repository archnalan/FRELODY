using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    /// <summary>Client for one-time PayPal checkout (Orders v2).</summary>
    public interface IPayPalApi
    {
        [Get("/api/pay-pal/config")]
        Task<IApiResponse<PayPalConfigDto>> GetConfig();

        [Post("/api/pay-pal/create-order")]
        Task<IApiResponse<PayPalCreateOrderResult>> CreateOrder([Body] PayPalCreateOrderRequest request);

        [Post("/api/pay-pal/capture-order")]
        Task<IApiResponse<PayPalCaptureResult>> CaptureOrder([Body] PayPalCaptureRequest request);
    }
}
