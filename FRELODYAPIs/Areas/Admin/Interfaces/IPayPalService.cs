using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    /// <summary>One-time PayPal checkout (Orders v2): create an order, then capture
    /// it and grant premium on success.</summary>
    public interface IPayPalService
    {
        PayPalConfigDto GetConfig();
        Task<ServiceResult<PayPalCreateOrderResult>> CreateOrderAsync(string productId);
        Task<ServiceResult<PayPalCaptureResult>> CaptureOrderAsync(string orderId, string productId);
    }
}
